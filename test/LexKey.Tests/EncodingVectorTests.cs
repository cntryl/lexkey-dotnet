namespace Cntryl.Keys.Tests;

public sealed class EncodingVectorTests
{
    [Fact]
    public void ShouldMatchTextAndUuidVectorsGivenPortableSpecExamples()
    {
        // Arrange
        var uuid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

        // Act
        var text = LexKey.EncodeString("hello");
        var encodedUuid = LexKey.EncodeGuid(uuid);

        // Assert
        Assert.Equal("68656c6c6f", text.ToHexString());
        Assert.Equal("550e8400e29b41d4a716446655440000", encodedUuid.ToHexString());
    }

    [Fact]
    public void ShouldMatchDeclaredWidthIntegerVectorsGivenPortableSpecExamples()
    {
        // Arrange
        const long signed = -123;
        const ulong unsigned = 255;

        // Act
        var signedEncodings = new[]
        {
            LexKey.EncodeInt8((sbyte)signed).ToHexString(),
            LexKey.EncodeInt16((short)signed).ToHexString(),
            LexKey.EncodeInt32((int)signed).ToHexString(),
            LexKey.EncodeInt64(signed).ToHexString(),
        };
        var unsignedEncodings = new[]
        {
            LexKey.EncodeUInt8((byte)unsigned).ToHexString(),
            LexKey.EncodeUInt16((ushort)unsigned).ToHexString(),
            LexKey.EncodeUInt32((uint)unsigned).ToHexString(),
            LexKey.EncodeUInt64(unsigned).ToHexString(),
        };

        // Assert
        Assert.Equal(["05", "7f85", "7fffff85", "7fffffffffffff85"], signedEncodings);
        Assert.Equal(["ff", "00ff", "000000ff", "00000000000000ff"], unsignedEncodings);
    }

    [Fact]
    public void ShouldMatchFloatingPointVectorsGivenPortableSpecExamples()
    {
        // Arrange
        const float single = 3.14F;
        const double @double = 3.14D;

        // Act
        var positiveSingle = LexKey.EncodeSingle(single);
        var negativeSingle = LexKey.EncodeSingle(-single);
        var positiveDouble = LexKey.EncodeDouble(@double);
        var negativeDouble = LexKey.EncodeDouble(-@double);

        // Assert
        Assert.Equal("c048f5c3", positiveSingle.ToHexString());
        Assert.Equal("3fb70a3c", negativeSingle.ToHexString());
        Assert.Equal("c0091eb851eb851f", positiveDouble.ToHexString());
        Assert.Equal("3ff6e147ae147ae0", negativeDouble.ToHexString());
    }

    [Fact]
    public void ShouldPreserveNegativeAndPositiveZeroOrderGivenFloatEncoding()
    {
        // Arrange
        var negativeZero = BitConverter.Int64BitsToDouble(long.MinValue);

        // Act
        var negative = LexKey.EncodeDouble(negativeZero);
        var positive = LexKey.EncodeDouble(0D);

        // Assert
        Assert.True(negative < positive);
        Assert.Equal("7fffffffffffffff", negative.ToHexString());
        Assert.Equal("8000000000000000", positive.ToHexString());
    }

    [Fact]
    public void ShouldRejectNanGivenFloatEncoding()
    {
        // Arrange
        const float single = float.NaN;
        const double @double = double.NaN;

        // Act
        var encodeSingle = () => LexKey.EncodeSingle(single);
        var encodeDouble = () => LexKey.EncodeDouble(@double);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(encodeSingle);
        Assert.Throws<ArgumentOutOfRangeException>(encodeDouble);
    }

    [Fact]
    public void ShouldMatchMarkerAndTimeVectorsGivenPortableSpecExamples()
    {
        // Arrange
        const long unixNanoseconds = 1_700_000_000_000_000_000;

        // Act
        var values = new[]
        {
            LexKey.EncodeBoolean(false).ToHexString(),
            LexKey.EncodeBoolean(true).ToHexString(),
            LexKey.EncodeNil().ToHexString(),
            LexKey.EncodeEndMarker().ToHexString(),
            LexKey.EncodeUnixNanoseconds(0).ToHexString(),
            LexKey.EncodeUnixNanoseconds(unixNanoseconds).ToHexString(),
        };

        // Assert
        Assert.Equal(
            ["00", "01", "00", "ff", "8000000000000000", "97979cfe362a0000"],
            values);
    }

    [Fact]
    public void ShouldEncodeMixedCompositeGivenTypedParts()
    {
        // Arrange
        LexKeyPart[] parts = ["foo", 42L, true];

        // Act
        var key = LexKey.EncodeComposite(parts);

        // Assert
        Assert.Equal("666f6f00800000000000002a0001", key.ToHexString());
    }

    [Fact]
    public void ShouldPreserveEmptyPartsWithoutTrailingSeparatorGivenComposite()
    {
        // Arrange
        LexKeyPart[] parts = [Array.Empty<byte>(), "middle", Array.Empty<byte>()];

        // Act
        var key = LexKey.EncodeComposite(parts);

        // Assert
        Assert.Equal("006d6964646c6500", key.ToHexString());
    }

    [Fact]
    public void ShouldOrderSignedValuesNumericallyGivenSameDeclaredWidth()
    {
        // Arrange
        long[] values = [long.MinValue, -100, -1, 0, 1, 100, long.MaxValue];

        // Act
        var keys = values.Select(LexKey.EncodeInt64).ToArray();

        // Assert
        Assert.Equal(keys, keys.Order());
    }

    [Fact]
    public void ShouldOrderFiniteFloatsNumericallyGivenSameDeclaredWidth()
    {
        // Arrange
        double[] values = [double.NegativeInfinity, -10, -0D, 0D, 10, double.PositiveInfinity];

        // Act
        var keys = values.Select(LexKey.EncodeDouble).ToArray();

        // Assert
        Assert.Equal(keys, keys.Order());
    }

    [Fact]
    public void ShouldOrderEveryIntegerWidthGivenBoundaryValues()
    {
        // Arrange
        sbyte[] int8 = [sbyte.MinValue, -1, 0, 1, sbyte.MaxValue];
        short[] int16 = [short.MinValue, -1, 0, 1, short.MaxValue];
        int[] int32 = [int.MinValue, -1, 0, 1, int.MaxValue];
        long[] int64 = [long.MinValue, -1, 0, 1, long.MaxValue];
        byte[] uint8 = [byte.MinValue, 1, byte.MaxValue];
        ushort[] uint16 = [ushort.MinValue, 1, ushort.MaxValue];
        uint[] uint32 = [uint.MinValue, 1, uint.MaxValue];
        ulong[] uint64 = [ulong.MinValue, 1, ulong.MaxValue];

        // Act
        var orderedEncodings = new[]
        {
            int8.Select(LexKey.EncodeInt8),
            int16.Select(LexKey.EncodeInt16),
            int32.Select(LexKey.EncodeInt32),
            int64.Select(LexKey.EncodeInt64),
            uint8.Select(LexKey.EncodeUInt8),
            uint16.Select(LexKey.EncodeUInt16),
            uint32.Select(LexKey.EncodeUInt32),
            uint64.Select(LexKey.EncodeUInt64),
        };

        // Assert
        Assert.All(orderedEncodings, AssertOrdered);
    }

    [Fact]
    public void ShouldMatchStandaloneEncodingsGivenEveryCompositePartType()
    {
        // Arrange
        var uuid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        LexKeyPart[] parts =
        [
            "hello",
            new byte[] { 0xAA, 0xBB },
            LexKey.EncodeUInt16(42),
            uuid,
            true,
            (byte)1,
            (ushort)2,
            3U,
            4UL,
            (sbyte)-1,
            (short)-2,
            -3,
            -4L,
            1.5F,
            2.5D,
        ];
        LexKey[] expected =
        [
            LexKey.EncodeString("hello"),
            LexKey.EncodeBytes([0xAA, 0xBB]),
            LexKey.EncodeUInt16(42),
            LexKey.EncodeGuid(uuid),
            LexKey.EncodeBoolean(true),
            LexKey.EncodeUInt8(1),
            LexKey.EncodeUInt16(2),
            LexKey.EncodeUInt32(3),
            LexKey.EncodeUInt64(4),
            LexKey.EncodeInt8(-1),
            LexKey.EncodeInt16(-2),
            LexKey.EncodeInt32(-3),
            LexKey.EncodeInt64(-4),
            LexKey.EncodeSingle(1.5F),
            LexKey.EncodeDouble(2.5D),
        ];

        // Act
        var actual = LexKey.EncodeComposite(parts).ToArray();
        var expectedBytes = expected
            .SelectMany(static (part, index) => index == 0
                ? part.ToArray()
                : new byte[] { LexKey.Separator }.Concat(part.ToArray()))
            .ToArray();

        // Assert
        Assert.Equal(expectedBytes, actual);
    }

    static void AssertOrdered(IEnumerable<LexKey> values)
    {
        var keys = values.ToArray();
        Assert.Equal(keys, keys.Order());
    }
}
