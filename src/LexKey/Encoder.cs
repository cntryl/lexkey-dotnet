using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace Cntryl.Keys;

/// <summary>A reusable buffer-oriented LexKey encoder for allocation-sensitive paths.</summary>
public sealed class Encoder
{
    readonly ArrayBufferWriter<byte> _buffer;

    /// <summary>Initializes an encoder with an optional capacity hint.</summary>
    /// <param name="capacity">Initial buffer capacity.</param>
    public Encoder(int capacity = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        _buffer = capacity == 0 ? new ArrayBufferWriter<byte>() : new ArrayBufferWriter<byte>(capacity);
    }

    /// <summary>Gets the number of accumulated bytes.</summary>
    public int Length => _buffer.WrittenCount;

    /// <summary>Gets whether the encoder is empty.</summary>
    public bool IsEmpty => _buffer.WrittenCount == 0;

    /// <summary>Returns a read-only view valid until the encoder is modified.</summary>
    public ReadOnlySpan<byte> AsSpan() => _buffer.WrittenSpan;

    /// <summary>Clears the accumulated bytes while retaining capacity.</summary>
    public void Clear() => _buffer.Clear();

    /// <summary>Returns an immutable key containing a copy of the accumulated bytes.</summary>
    public LexKey ToLexKey() => LexKey.FromBytes(_buffer.WrittenSpan);

    /// <summary>Returns an owned copy of the accumulated bytes.</summary>
    public byte[] ToArray() => _buffer.WrittenSpan.ToArray();

    /// <summary>Appends one byte.</summary>
    public void PushByte(byte value)
    {
        _buffer.GetSpan(1)[0] = value;
        _buffer.Advance(1);
    }

    /// <summary>Appends the composite separator.</summary>
    public void PushSeparator() => PushByte(LexKey.Separator);

    /// <summary>Appends the structured end marker.</summary>
    public void PushEndMarker() => PushByte(LexKey.EndMarker);

    /// <summary>Appends UTF-8 bytes and returns the number written.</summary>
    public int EncodeString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var length = Encoding.UTF8.GetByteCount(value);
        var written = Encoding.UTF8.GetBytes(value, _buffer.GetSpan(length));
        _buffer.Advance(written);
        return written;
    }

    /// <summary>Appends raw bytes and returns the number written.</summary>
    public int EncodeBytes(ReadOnlySpan<byte> value)
    {
        value.CopyTo(_buffer.GetSpan(value.Length));
        _buffer.Advance(value.Length);
        return value.Length;
    }

    /// <summary>Appends an unsigned byte.</summary>
    public int EncodeUInt8(byte value)
    {
        PushByte(value);
        return 1;
    }

    /// <summary>Appends an unsigned 16-bit big-endian integer.</summary>
    public int EncodeUInt16(ushort value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(_buffer.GetSpan(sizeof(ushort)), value);
        _buffer.Advance(sizeof(ushort));
        return sizeof(ushort);
    }

    /// <summary>Appends an unsigned 32-bit big-endian integer.</summary>
    public int EncodeUInt32(uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(_buffer.GetSpan(sizeof(uint)), value);
        _buffer.Advance(sizeof(uint));
        return sizeof(uint);
    }

    /// <summary>Appends an unsigned 64-bit big-endian integer.</summary>
    public int EncodeUInt64(ulong value)
    {
        BinaryPrimitives.WriteUInt64BigEndian(_buffer.GetSpan(sizeof(ulong)), value);
        _buffer.Advance(sizeof(ulong));
        return sizeof(ulong);
    }

    /// <summary>Appends a sortable signed byte.</summary>
    public int EncodeInt8(sbyte value) => EncodeUInt8((byte)(value ^ sbyte.MinValue));

    /// <summary>Appends a sortable signed 16-bit integer.</summary>
    public int EncodeInt16(short value) => EncodeUInt16((ushort)(value ^ short.MinValue));

    /// <summary>Appends a sortable signed 32-bit integer.</summary>
    public int EncodeInt32(int value) => EncodeUInt32((uint)(value ^ int.MinValue));

    /// <summary>Appends a sortable signed 64-bit integer.</summary>
    public int EncodeInt64(long value) => EncodeUInt64((ulong)(value ^ long.MinValue));

    /// <summary>Appends a Boolean marker.</summary>
    public int EncodeBoolean(bool value) => EncodeUInt8(value ? (byte)1 : (byte)0);

    /// <summary>Appends a sortable single-precision float.</summary>
    public int EncodeSingle(float value)
    {
        if (float.IsNaN(value))
            throw new ArgumentOutOfRangeException(nameof(value), "NaN is not encodable.");
        var bits = BitConverter.SingleToUInt32Bits(value);
        return EncodeUInt32((bits & 0x8000_0000U) != 0 ? ~bits : bits ^ 0x8000_0000U);
    }

    /// <summary>Appends a sortable double-precision float.</summary>
    public int EncodeDouble(double value)
    {
        if (double.IsNaN(value))
            throw new ArgumentOutOfRangeException(nameof(value), "NaN is not encodable.");
        var bits = BitConverter.DoubleToUInt64Bits(value);
        return EncodeUInt64((bits & 0x8000_0000_0000_0000UL) != 0
            ? ~bits
            : bits ^ 0x8000_0000_0000_0000UL);
    }

    /// <summary>Appends a UUID in RFC-4122 network order.</summary>
    public int EncodeGuid(Guid value)
    {
        var destination = _buffer.GetSpan(16);
        if (!value.TryWriteBytes(destination, bigEndian: true, out var written) || written != 16)
            throw new InvalidOperationException("The UUID could not be encoded.");
        _buffer.Advance(written);
        return written;
    }

    /// <summary>Appends Unix nanoseconds with the signed 64-bit transform.</summary>
    public int EncodeUnixNanoseconds(long nanoseconds) => EncodeInt64(nanoseconds);

    /// <summary>Appends typed parts separated by one <c>0x00</c> byte.</summary>
    public int EncodeComposite(params ReadOnlySpan<LexKeyPart> parts)
    {
        var start = Length;
        for (var index = 0; index < parts.Length; index++)
        {
            var part = parts[index];
            var length = part.EncodedLength;
            _buffer.Advance(part.WriteTo(_buffer.GetSpan(length)));
            if (index + 1 < parts.Length)
                PushSeparator();
        }
        return Length - start;
    }
}
