namespace Cntryl.Keys.Tests;

public sealed class RangeAndValueTests
{
    [Fact]
    public void ShouldMatchStructuredRangeVectorsGivenPartitionAndRows()
    {
        // Arrange
        var partition = "part"u8;

        // Act
        var lower = LexKey.EncodeRangeLower(partition, "start"u8.ToArray());
        var upper = LexKey.EncodeRangeUpper(partition, "end"u8.ToArray());
        var bounds = LexKey.EncodeRangeBounds(partition);

        // Assert
        Assert.Equal("70617274007374617274", lower.ToHexString());
        Assert.Equal("7061727400656e64ff", upper.ToHexString());
        Assert.Equal("7061727400", bounds.Lower.ToHexString());
        Assert.Equal("70617274ff", bounds.Upper.ToHexString());
    }

    [Fact]
    public void ShouldDistinguishStructuredEndFromRawSuccessorGivenPrefixBytes()
    {
        // Arrange
        var prefix = "acme\0kv\0"u8;

        // Act
        var structured = LexKey.PrefixEnd(prefix);
        var successor = LexKey.PrefixSuccessor(prefix);
        var scanBounds = LexKey.PrefixScanBounds(prefix);
        var rangeBounds = LexKey.PrefixRangeBounds(prefix);

        // Assert
        Assert.Equal("61636d65006b7600ff", Convert.ToHexStringLower(structured));
        Assert.Equal("61636d65006b7601", Convert.ToHexStringLower(successor!));
        Assert.Equal(prefix.ToArray(), scanBounds.Lower);
        Assert.Equal(successor, scanBounds.Upper);
        Assert.Equal("61636d65006b760000", Convert.ToHexStringLower(rangeBounds.Lower));
        Assert.Equal(structured, rangeBounds.Upper);
    }

    [Theory]
    [InlineData("616263", "616264")]
    [InlineData("6162ff", "6163")]
    [InlineData("ff", null)]
    [InlineData("ffff", null)]
    [InlineData("", null)]
    public void ShouldMatchRawPrefixSuccessorVectorsGivenArbitraryBytes(string input, string? expected)
    {
        // Arrange
        var prefix = Convert.FromHexString(input);

        // Act
        var successor = LexKey.PrefixSuccessor(prefix);

        // Assert
        Assert.Equal(expected, successor is null ? null : Convert.ToHexStringLower(successor));
    }

    [Fact]
    public void ShouldCloneInputsAndOutputsGivenImmutableKey()
    {
        // Arrange
        var source = new byte[] { 1, 2, 3 };
        var key = LexKey.FromBytes(source);

        // Act
        source[0] = 9;
        var copy = key.ToArray();
        copy[1] = 9;

        // Assert
        Assert.Equal(new byte[] { 1, 2, 3 }, key.ToArray());
    }

    [Fact]
    public void ShouldExposeReadOnlyMemoryWithoutCopyGivenImmutableKey()
    {
        // Arrange
        var key = LexKey.FromBytes([1, 2, 3]);

        // Act
        var first = key.AsMemory();
        var second = key.AsMemory();

        // Assert
        Assert.True(first.Equals(second));
        Assert.Equal(new byte[] { 1, 2, 3 }, first.ToArray());
    }

    [Fact]
    public void ShouldCloneCallerOwnedBytesGivenCompositePart()
    {
        // Arrange
        var source = new byte[] { 1, 2, 3 };
        var part = LexKeyPart.FromByteArray(source);

        // Act
        source[0] = 9;
        var key = LexKey.EncodeComposite(part);

        // Assert
        Assert.Equal(new byte[] { 1, 2, 3 }, key.ToArray());
    }

    [Fact]
    public void ShouldEncodeFirstAndLastDirectlyGivenCompositeParts()
    {
        // Arrange
        LexKeyPart[] parts = ["tenant", 42L];

        // Act
        var first = LexKey.EncodeFirst(parts);
        var last = LexKey.EncodeLast(parts);

        // Assert
        Assert.Equal("74656e616e7400800000000000002a00", first.ToHexString());
        Assert.Equal("74656e616e7400800000000000002aff", last.ToHexString());
    }

    [Fact]
    public void ShouldUseByteEqualityOrderingAndHashingGivenEquivalentKeys()
    {
        // Arrange
        var first = LexKey.FromBytes([1, 2]);
        var equivalent = LexKey.FromBytes([1, 2]);
        var later = LexKey.FromBytes([1, 2, 0]);

        // Act
        var set = new HashSet<LexKey> { first, equivalent, later };

        // Assert
        Assert.Equal(first, equivalent);
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
        Assert.True(first < later);
        Assert.Equal(2, set.Count);
    }
}
