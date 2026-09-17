namespace Cntryl.Keys.Tests;

public sealed class EncoderTests
{
    [Fact]
    public void ShouldMatchAllocatingApiGivenEveryTypedEncoding()
    {
        // Arrange
        var encoder = new Encoder(128);
        var uuid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

        // Act
        encoder.EncodeUInt8(7);
        encoder.EncodeUInt16(8);
        encoder.EncodeUInt32(9);
        encoder.EncodeUInt64(10);
        encoder.EncodeInt8(-7);
        encoder.EncodeInt16(-8);
        encoder.EncodeInt32(-9);
        encoder.EncodeInt64(-10);
        encoder.EncodeSingle(3.14F);
        encoder.EncodeDouble(3.14D);
        encoder.EncodeBoolean(true);
        encoder.EncodeGuid(uuid);
        encoder.EncodeUnixNanoseconds(42);
        var expectedParts = new[]
        {
            LexKey.EncodeUInt8(7).ToArray(),
            LexKey.EncodeUInt16(8).ToArray(),
            LexKey.EncodeUInt32(9).ToArray(),
            LexKey.EncodeUInt64(10).ToArray(),
            LexKey.EncodeInt8(-7).ToArray(),
            LexKey.EncodeInt16(-8).ToArray(),
            LexKey.EncodeInt32(-9).ToArray(),
            LexKey.EncodeInt64(-10).ToArray(),
            LexKey.EncodeSingle(3.14F).ToArray(),
            LexKey.EncodeDouble(3.14D).ToArray(),
            LexKey.EncodeBoolean(true).ToArray(),
            LexKey.EncodeGuid(uuid).ToArray(),
            LexKey.EncodeUnixNanoseconds(42).ToArray(),
        };

        // Assert
        Assert.Equal(expectedParts.SelectMany(static part => part), encoder.ToArray());
    }

    [Fact]
    public void ShouldReuseCapacityAndEncodeCompositeGivenClearedEncoder()
    {
        // Arrange
        var encoder = new Encoder(64);
        encoder.EncodeString("discard");
        encoder.Clear();
        LexKeyPart[] parts = ["tenant", 42L, true];

        // Act
        var written = encoder.EncodeComposite(parts);
        var key = encoder.ToLexKey();

        // Assert
        Assert.Equal(17, written);
        Assert.Equal("74656e616e7400800000000000002a0001", key.ToHexString());
        Assert.Equal(written, encoder.Length);
        Assert.False(encoder.IsEmpty);
    }

    [Fact]
    public void ShouldRejectNanWithoutMutatingBufferGivenEncoder()
    {
        // Arrange
        var encoder = new Encoder();
        encoder.PushByte(42);

        // Act
        Action encode = () => _ = encoder.EncodeDouble(double.NaN);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(encode);
        Assert.Equal(new byte[] { 42 }, encoder.ToArray());
    }

    [Fact]
    public void ShouldEncodeCustomPartGivenApplicationEncoder()
    {
        // Arrange
        var custom = LexKeyPart.From(new TwoBytePart());

        // Act
        var key = LexKey.EncodeComposite("prefix", custom);

        // Assert
        Assert.Equal("70726566697800aabb", key.ToHexString());
    }

    sealed class TwoBytePart : ILexKeyEncodable
    {
        public int EncodedLength => 2;

        public void Encode(Span<byte> destination)
        {
            destination[0] = 0xAA;
            destination[1] = 0xBB;
        }
    }
}
