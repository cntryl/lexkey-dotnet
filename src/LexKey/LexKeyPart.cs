using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Cntryl.Keys;

/// <summary>
/// Describes one typed component of a composite key without eagerly allocating its encoded form.
/// </summary>
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "This is a transient encoding descriptor; encoded LexKey values own equality semantics.")]
public readonly struct LexKeyPart
{
    readonly PartKind _kind;
    readonly object? _reference;
    readonly ulong _first;
    readonly ulong _second;

    LexKeyPart(PartKind kind, object? reference = null, ulong first = 0, ulong second = 0)
    {
        _kind = kind;
        _reference = reference;
        _first = first;
        _second = second;
    }

    internal int EncodedLength => _kind switch
    {
        PartKind.Empty => 0,
        PartKind.Bytes => ((byte[])_reference!).Length,
        PartKind.String => checked((int)_first),
        PartKind.UInt8 or PartKind.Int8 or PartKind.Boolean => 1,
        PartKind.UInt16 or PartKind.Int16 => 2,
        PartKind.UInt32 or PartKind.Int32 or PartKind.Single => 4,
        PartKind.UInt64 or PartKind.Int64 or PartKind.Double => 8,
        PartKind.Guid => 16,
        PartKind.LexKey => ((LexKey)_reference!).Length,
        PartKind.Custom => checked((int)_first),
        _ => throw new InvalidOperationException("Unknown LexKey part kind."),
    };

    internal int WriteTo(Span<byte> destination)
    {
        var required = EncodedLength;
        if (destination.Length < required)
            throw new ArgumentException("The destination is too short.", nameof(destination));

        switch (_kind)
        {
            case PartKind.Empty:
                break;
            case PartKind.Bytes:
                ((byte[])_reference!).CopyTo(destination);
                break;
            case PartKind.String:
                Encoding.UTF8.GetBytes((string)_reference!, destination);
                break;
            case PartKind.UInt8:
                destination[0] = (byte)_first;
                break;
            case PartKind.UInt16:
                BinaryPrimitives.WriteUInt16BigEndian(destination, (ushort)_first);
                break;
            case PartKind.UInt32:
                BinaryPrimitives.WriteUInt32BigEndian(destination, (uint)_first);
                break;
            case PartKind.UInt64:
                BinaryPrimitives.WriteUInt64BigEndian(destination, _first);
                break;
            case PartKind.Int8:
                destination[0] = (byte)((sbyte)_first ^ sbyte.MinValue);
                break;
            case PartKind.Int16:
                BinaryPrimitives.WriteUInt16BigEndian(destination, (ushort)((short)_first ^ short.MinValue));
                break;
            case PartKind.Int32:
                BinaryPrimitives.WriteUInt32BigEndian(destination, (uint)((int)_first ^ int.MinValue));
                break;
            case PartKind.Int64:
                BinaryPrimitives.WriteUInt64BigEndian(destination, (ulong)((long)_first ^ long.MinValue));
                break;
            case PartKind.Boolean:
                destination[0] = _first == 0 ? (byte)0 : (byte)1;
                break;
            case PartKind.Single:
                BinaryPrimitives.WriteUInt32BigEndian(destination, (uint)_first);
                break;
            case PartKind.Double:
                BinaryPrimitives.WriteUInt64BigEndian(destination, _first);
                break;
            case PartKind.Guid:
                BinaryPrimitives.WriteUInt64BigEndian(destination, _first);
                BinaryPrimitives.WriteUInt64BigEndian(destination[8..], _second);
                break;
            case PartKind.LexKey:
                ((LexKey)_reference!).AsSpan().CopyTo(destination);
                break;
            case PartKind.Custom:
                ((ILexKeyEncodable)_reference!).Encode(destination[..required]);
                break;
            default:
                throw new InvalidOperationException("Unknown LexKey part kind.");
        }

        return required;
    }

    /// <summary>Creates a part from raw bytes, cloning the input.</summary>
    public static LexKeyPart FromBytes(ReadOnlySpan<byte> value) =>
        new(PartKind.Bytes, value.ToArray());

    /// <summary>Creates a part from a UTF-8 string.</summary>
    public static LexKeyPart FromString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(PartKind.String, value, (ulong)Encoding.UTF8.GetByteCount(value));
    }

    /// <summary>Creates a part from a byte array, cloning the input.</summary>
    public static LexKeyPart FromByteArray(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return FromBytes(value);
    }

    /// <summary>Creates a part from an immutable key without copying it.</summary>
    public static LexKeyPart FromLexKey(LexKey value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(PartKind.LexKey, value);
    }

    /// <summary>Creates a part from a UUID.</summary>
    public static LexKeyPart FromGuid(Guid value)
    {
        Span<byte> bytes = stackalloc byte[16];
        if (!value.TryWriteBytes(bytes, bigEndian: true, out var written) || written != bytes.Length)
            throw new InvalidOperationException("The UUID could not be encoded.");
        return new(
            PartKind.Guid,
            first: BinaryPrimitives.ReadUInt64BigEndian(bytes),
            second: BinaryPrimitives.ReadUInt64BigEndian(bytes[8..]));
    }

    /// <summary>Creates a part from a Boolean.</summary>
    public static LexKeyPart FromBoolean(bool value) => new(PartKind.Boolean, first: value ? 1UL : 0UL);

    /// <summary>Creates a part from an unsigned byte.</summary>
    public static LexKeyPart FromByte(byte value) => new(PartKind.UInt8, first: value);

    /// <summary>Creates a part from an unsigned 16-bit integer.</summary>
    public static LexKeyPart FromUInt16(ushort value) => new(PartKind.UInt16, first: value);

    /// <summary>Creates a part from an unsigned 32-bit integer.</summary>
    public static LexKeyPart FromUInt32(uint value) => new(PartKind.UInt32, first: value);

    /// <summary>Creates a part from an unsigned 64-bit integer.</summary>
    public static LexKeyPart FromUInt64(ulong value) => new(PartKind.UInt64, first: value);

    /// <summary>Creates a part from a signed byte.</summary>
    public static LexKeyPart FromSByte(sbyte value) => new(PartKind.Int8, first: unchecked((ulong)value));

    /// <summary>Creates a part from a signed 16-bit integer.</summary>
    public static LexKeyPart FromInt16(short value) => new(PartKind.Int16, first: unchecked((ulong)value));

    /// <summary>Creates a part from a signed 32-bit integer.</summary>
    public static LexKeyPart FromInt32(int value) => new(PartKind.Int32, first: unchecked((ulong)value));

    /// <summary>Creates a part from a signed 64-bit integer.</summary>
    public static LexKeyPart FromInt64(long value) => new(PartKind.Int64, first: unchecked((ulong)value));

    /// <summary>Creates a part from a single-precision float.</summary>
    public static LexKeyPart FromSingle(float value)
    {
        if (float.IsNaN(value))
            throw new ArgumentOutOfRangeException(nameof(value), "NaN is not encodable.");
        var bits = BitConverter.SingleToUInt32Bits(value);
        return new(PartKind.Single, first: (bits & 0x8000_0000U) != 0 ? ~bits : bits ^ 0x8000_0000U);
    }

    /// <summary>Creates a part from a double-precision float.</summary>
    public static LexKeyPart FromDouble(double value)
    {
        if (double.IsNaN(value))
            throw new ArgumentOutOfRangeException(nameof(value), "NaN is not encodable.");
        var bits = BitConverter.DoubleToUInt64Bits(value);
        return new(
            PartKind.Double,
            first: (bits & 0x8000_0000_0000_0000UL) != 0
                ? ~bits
                : bits ^ 0x8000_0000_0000_0000UL);
    }

    /// <summary>Creates a part from a custom allocation-free encoder.</summary>
    public static LexKeyPart From(ILexKeyEncodable value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.EncodedLength < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "EncodedLength cannot be negative.");
        return new(PartKind.Custom, value, (ulong)value.EncodedLength);
    }

    /// <summary>Converts a UTF-8 string to a part.</summary>
    public static implicit operator LexKeyPart(string value) => FromString(value);

    /// <summary>Converts raw bytes to a part.</summary>
    public static implicit operator LexKeyPart(byte[] value) => FromByteArray(value);

    /// <summary>Converts an existing key to a part.</summary>
    public static implicit operator LexKeyPart(LexKey value) => FromLexKey(value);

    /// <summary>Converts a UUID to its RFC-4122 network-order bytes.</summary>
    public static implicit operator LexKeyPart(Guid value) => FromGuid(value);

    /// <summary>Converts a Boolean to its one-byte encoding.</summary>
    public static implicit operator LexKeyPart(bool value) => FromBoolean(value);

    /// <summary>Converts an unsigned byte at its declared width.</summary>
    public static implicit operator LexKeyPart(byte value) => FromByte(value);

    /// <summary>Converts an unsigned 16-bit integer at its declared width.</summary>
    public static implicit operator LexKeyPart(ushort value) => FromUInt16(value);

    /// <summary>Converts an unsigned 32-bit integer at its declared width.</summary>
    public static implicit operator LexKeyPart(uint value) => FromUInt32(value);

    /// <summary>Converts an unsigned 64-bit integer at its declared width.</summary>
    public static implicit operator LexKeyPart(ulong value) => FromUInt64(value);

    /// <summary>Converts a signed byte at its declared width.</summary>
    public static implicit operator LexKeyPart(sbyte value) => FromSByte(value);

    /// <summary>Converts a signed 16-bit integer at its declared width.</summary>
    public static implicit operator LexKeyPart(short value) => FromInt16(value);

    /// <summary>Converts a signed 32-bit integer at its declared width.</summary>
    public static implicit operator LexKeyPart(int value) => FromInt32(value);

    /// <summary>Converts a signed 64-bit integer at its declared width.</summary>
    public static implicit operator LexKeyPart(long value) => FromInt64(value);

    /// <summary>Converts a 32-bit float at its declared width.</summary>
    public static implicit operator LexKeyPart(float value) => FromSingle(value);

    /// <summary>Converts a 64-bit float at its declared width.</summary>
    public static implicit operator LexKeyPart(double value) => FromDouble(value);

    enum PartKind : byte
    {
        Empty,
        Bytes,
        String,
        UInt8,
        UInt16,
        UInt32,
        UInt64,
        Int8,
        Int16,
        Int32,
        Int64,
        Boolean,
        Single,
        Double,
        Guid,
        LexKey,
        Custom,
    }
}
