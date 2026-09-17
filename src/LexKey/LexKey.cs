using System.Buffers.Binary;
using System.Text;

namespace Cntryl.Keys;

/// <summary>An immutable, lexicographically sortable binary key.</summary>
public sealed class LexKey : IComparable<LexKey>, IEquatable<LexKey>, ILexKeyEncodable
{
    readonly byte[] _bytes;

    LexKey(byte[] bytes) => _bytes = bytes;

    /// <summary>The separator placed between composite parts.</summary>
    public const byte Separator = 0x00;

    /// <summary>The marker used as the structured range end.</summary>
    public const byte EndMarker = 0xFF;

    /// <summary>Gets the empty key.</summary>
    public static LexKey Empty { get; } = new([]);

    /// <summary>Gets the encoded byte count.</summary>
    public int Length => _bytes.Length;

    /// <summary>Gets whether the key contains no bytes.</summary>
    public bool IsEmpty => _bytes.Length == 0;

    int ILexKeyEncodable.EncodedLength => _bytes.Length;

    /// <summary>Creates an immutable key by cloning raw bytes.</summary>
    public static LexKey FromBytes(ReadOnlySpan<byte> bytes) => bytes.IsEmpty ? Empty : new(bytes.ToArray());

    /// <summary>Returns a read-only view of the encoded bytes.</summary>
    public ReadOnlySpan<byte> AsSpan() => _bytes;

    /// <summary>Returns an owned copy of the encoded bytes.</summary>
    public byte[] ToArray() => _bytes.ToArray();

    /// <summary>Returns lowercase hexadecimal without a prefix.</summary>
    public string ToHexString() => Convert.ToHexStringLower(_bytes);

    /// <summary>Encodes a UTF-8 string as raw bytes.</summary>
    public static LexKey EncodeString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(Encoding.UTF8.GetBytes(value));
    }

    /// <summary>Encodes raw bytes without transformation.</summary>
    public static LexKey EncodeBytes(ReadOnlySpan<byte> value) => FromBytes(value);

    /// <summary>Encodes an unsigned byte at its declared width.</summary>
    public static LexKey EncodeUInt8(byte value) => new([value]);

    /// <summary>Encodes an unsigned 16-bit integer in big-endian order.</summary>
    public static LexKey EncodeUInt16(ushort value)
    {
        var bytes = new byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        return new(bytes);
    }

    /// <summary>Encodes an unsigned 32-bit integer in big-endian order.</summary>
    public static LexKey EncodeUInt32(uint value)
    {
        var bytes = new byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        return new(bytes);
    }

    /// <summary>Encodes an unsigned 64-bit integer in big-endian order.</summary>
    public static LexKey EncodeUInt64(ulong value)
    {
        var bytes = new byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        return new(bytes);
    }

    /// <summary>Encodes a signed byte by flipping its sign bit.</summary>
    public static LexKey EncodeInt8(sbyte value) => new([(byte)(value ^ sbyte.MinValue)]);

    /// <summary>Encodes a signed 16-bit integer by flipping its sign bit and writing big-endian.</summary>
    public static LexKey EncodeInt16(short value)
    {
        var bytes = new byte[sizeof(short)];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, (ushort)(value ^ short.MinValue));
        return new(bytes);
    }

    /// <summary>Encodes a signed 32-bit integer by flipping its sign bit and writing big-endian.</summary>
    public static LexKey EncodeInt32(int value)
    {
        var bytes = new byte[sizeof(int)];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, (uint)(value ^ int.MinValue));
        return new(bytes);
    }

    /// <summary>Encodes a signed 64-bit integer by flipping its sign bit and writing big-endian.</summary>
    public static LexKey EncodeInt64(long value)
    {
        var bytes = new byte[sizeof(long)];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, (ulong)(value ^ long.MinValue));
        return new(bytes);
    }

    /// <summary>Encodes a Boolean as <c>0x00</c> or <c>0x01</c>.</summary>
    public static LexKey EncodeBoolean(bool value) => new([value ? (byte)1 : (byte)0]);

    /// <summary>Encodes a sortable IEEE-754 single-precision value.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is NaN.</exception>
    public static LexKey EncodeSingle(float value)
    {
        if (float.IsNaN(value))
            throw new ArgumentOutOfRangeException(nameof(value), "NaN is not encodable.");
        var bits = BitConverter.SingleToUInt32Bits(value);
        var transformed = (bits & 0x8000_0000U) != 0 ? ~bits : bits ^ 0x8000_0000U;
        return EncodeUInt32(transformed);
    }

    /// <summary>Encodes a sortable IEEE-754 double-precision value.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is NaN.</exception>
    public static LexKey EncodeDouble(double value)
    {
        if (double.IsNaN(value))
            throw new ArgumentOutOfRangeException(nameof(value), "NaN is not encodable.");
        var bits = BitConverter.DoubleToUInt64Bits(value);
        var transformed = (bits & 0x8000_0000_0000_0000UL) != 0
            ? ~bits
            : bits ^ 0x8000_0000_0000_0000UL;
        return EncodeUInt64(transformed);
    }

    /// <summary>Encodes a UUID as its 16 RFC-4122 network-order bytes.</summary>
    public static LexKey EncodeGuid(Guid value)
    {
        var bytes = new byte[16];
        if (!value.TryWriteBytes(bytes, bigEndian: true, out var written) || written != bytes.Length)
            throw new InvalidOperationException("The UUID could not be encoded.");
        return new(bytes);
    }

    /// <summary>Encodes Unix nanoseconds with the signed 64-bit transform.</summary>
    public static LexKey EncodeUnixNanoseconds(long nanoseconds) => EncodeInt64(nanoseconds);

    /// <summary>Encodes the end sentinel as one <c>0xFF</c> byte.</summary>
    public static LexKey EncodeEndMarker() => new([EndMarker]);

    /// <summary>Encodes the nil marker as one <c>0x00</c> byte.</summary>
    public static LexKey EncodeNil() => new([Separator]);

    /// <summary>Encodes typed parts separated by one <c>0x00</c> byte.</summary>
    public static LexKey EncodeComposite(params ReadOnlySpan<LexKeyPart> parts)
    {
        if (parts.IsEmpty)
            return Empty;
        var length = parts.Length - 1;
        foreach (var part in parts)
            length = checked(length + part.EncodedLength);
        var bytes = new byte[length];
        var offset = 0;
        for (var index = 0; index < parts.Length; index++)
        {
            var part = parts[index];
            offset += part.WriteTo(bytes.AsSpan(offset));
            if (index + 1 < parts.Length)
                bytes[offset++] = Separator;
        }
        return new(bytes);
    }

    /// <summary>Encodes already encoded byte parts separated by one <c>0x00</c> byte.</summary>
    public static LexKey EncodeCompositeBytes(params ReadOnlySpan<ReadOnlyMemory<byte>> parts)
    {
        if (parts.IsEmpty)
            return Empty;
        var length = parts.Length - 1;
        foreach (var part in parts)
            length = checked(length + part.Length);
        var bytes = new byte[length];
        var offset = 0;
        for (var index = 0; index < parts.Length; index++)
        {
            parts[index].Span.CopyTo(bytes.AsSpan(offset));
            offset += parts[index].Length;
            if (index + 1 < parts.Length)
                bytes[offset++] = Separator;
        }
        return new(bytes);
    }

    /// <summary>Builds a structured lower bound by appending <c>0x00</c> to a composite.</summary>
    public static LexKey EncodeFirst(params ReadOnlySpan<LexKeyPart> parts) =>
        EncodeCompositeWithSuffix(parts, Separator);

    /// <summary>Builds a structured upper bound by appending <c>0xFF</c> to a composite.</summary>
    public static LexKey EncodeLast(params ReadOnlySpan<LexKeyPart> parts) =>
        EncodeCompositeWithSuffix(parts, EndMarker);

    /// <summary>Builds <c>partition || 0x00 || rowLower</c>, omitting the row when null.</summary>
    public static LexKey EncodeRangeLower(
        ReadOnlySpan<byte> partition,
        ReadOnlyMemory<byte>? rowLower = null)
    {
        var row = rowLower.GetValueOrDefault().Span;
        var length = partition.Length + 1 + row.Length;
        var bytes = new byte[length];
        partition.CopyTo(bytes);
        bytes[partition.Length] = Separator;
        row.CopyTo(bytes.AsSpan(partition.Length + 1));
        return new(bytes);
    }

    /// <summary>
    /// Builds <c>partition || 0x00 || rowUpper || 0xFF</c>, or
    /// <c>partition || 0xFF</c> when no row upper bound is supplied.
    /// </summary>
    public static LexKey EncodeRangeUpper(
        ReadOnlySpan<byte> partition,
        ReadOnlyMemory<byte>? rowUpper = null)
    {
        if (!rowUpper.HasValue)
            return FromBytes(PrefixEnd(partition));
        var row = rowUpper.Value.Span;
        var bytes = new byte[partition.Length + row.Length + 2];
        partition.CopyTo(bytes);
        bytes[partition.Length] = Separator;
        row.CopyTo(bytes.AsSpan(partition.Length + 1));
        bytes[^1] = EndMarker;
        return new(bytes);
    }

    /// <summary>Returns the structured partition bounds <c>(P || 0x00, P || 0xFF)</c>.</summary>
    public static (LexKey Lower, LexKey Upper) EncodeRangeBounds(ReadOnlySpan<byte> partition) =>
        (EncodeRangeLower(partition), FromBytes(PrefixEnd(partition)));

    /// <summary>Returns <c>prefix || 0xFF</c> for a structured partition range.</summary>
    public static byte[] PrefixEnd(ReadOnlySpan<byte> prefix) => Append(prefix, EndMarker);

    /// <summary>Alias for <see cref="PrefixEnd"/>.</summary>
    public static byte[] RangeUpper(ReadOnlySpan<byte> prefix) => PrefixEnd(prefix);

    /// <summary>Returns the finite exclusive upper bound for an arbitrary raw-byte prefix.</summary>
    public static byte[]? PrefixSuccessor(ReadOnlySpan<byte> prefix)
    {
        for (var index = prefix.Length - 1; index >= 0; index--)
        {
            if (prefix[index] == byte.MaxValue)
                continue;
            var upper = prefix[..(index + 1)].ToArray();
            upper[index]++;
            return upper;
        }
        return null;
    }

    /// <summary>Returns cloned inclusive/exclusive bounds for an arbitrary raw-byte prefix.</summary>
    public static (byte[] Lower, byte[]? Upper) PrefixScanBounds(ReadOnlySpan<byte> prefix) =>
        (prefix.ToArray(), PrefixSuccessor(prefix));

    /// <summary>Returns structured bounds <c>(prefix || 0x00, prefix || 0xFF)</c>.</summary>
    public static (byte[] Lower, byte[] Upper) PrefixRangeBounds(ReadOnlySpan<byte> prefix) =>
        (Append(prefix, Separator), Append(prefix, EndMarker));

    /// <summary>Alias for <see cref="PrefixRangeBounds"/>.</summary>
    public static (byte[] Lower, byte[] Upper) RangeBounds(ReadOnlySpan<byte> prefix) =>
        PrefixRangeBounds(prefix);

    /// <inheritdoc />
    public int CompareTo(LexKey? other) => other is null ? 1 : AsSpan().SequenceCompareTo(other.AsSpan());

    /// <inheritdoc />
    public bool Equals(LexKey? other) => other is not null && AsSpan().SequenceEqual(other.AsSpan());

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is LexKey other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.AddBytes(_bytes);
        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString() => ToHexString();

    /// <summary>Tests byte equality.</summary>
    public static bool operator ==(LexKey? left, LexKey? right) => Equals(left, right);

    /// <summary>Tests byte inequality.</summary>
    public static bool operator !=(LexKey? left, LexKey? right) => !Equals(left, right);

    /// <summary>Tests byte-lexicographic ordering.</summary>
    public static bool operator <(LexKey? left, LexKey? right) =>
        Comparer<LexKey>.Default.Compare(left, right) < 0;

    /// <summary>Tests byte-lexicographic ordering.</summary>
    public static bool operator >(LexKey? left, LexKey? right) =>
        Comparer<LexKey>.Default.Compare(left, right) > 0;

    /// <summary>Tests byte-lexicographic ordering.</summary>
    public static bool operator <=(LexKey? left, LexKey? right) =>
        Comparer<LexKey>.Default.Compare(left, right) <= 0;

    /// <summary>Tests byte-lexicographic ordering.</summary>
    public static bool operator >=(LexKey? left, LexKey? right) =>
        Comparer<LexKey>.Default.Compare(left, right) >= 0;

    void ILexKeyEncodable.Encode(Span<byte> destination)
    {
        if (destination.Length < _bytes.Length)
            throw new ArgumentException("The destination is too short.", nameof(destination));
        _bytes.CopyTo(destination);
    }

    static byte[] Append(ReadOnlySpan<byte> value, byte suffix)
    {
        var result = new byte[value.Length + 1];
        value.CopyTo(result);
        result[^1] = suffix;
        return result;
    }

    static LexKey EncodeCompositeWithSuffix(ReadOnlySpan<LexKeyPart> parts, byte suffix)
    {
        var length = 1;
        if (!parts.IsEmpty)
            length = checked(length + parts.Length - 1);
        foreach (var part in parts)
            length = checked(length + part.EncodedLength);

        var bytes = new byte[length];
        var offset = 0;
        for (var index = 0; index < parts.Length; index++)
        {
            var part = parts[index];
            offset += part.WriteTo(bytes.AsSpan(offset));
            if (index + 1 < parts.Length)
                bytes[offset++] = Separator;
        }
        bytes[offset] = suffix;
        return new(bytes);
    }
}
