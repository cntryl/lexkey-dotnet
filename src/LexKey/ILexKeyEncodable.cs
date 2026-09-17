namespace Cntryl.Keys;

/// <summary>Allows an application type to provide its canonical LexKey bytes.</summary>
public interface ILexKeyEncodable
{
    /// <summary>Gets the exact encoded byte count.</summary>
    int EncodedLength { get; }

    /// <summary>Writes the encoding to a destination of at least <see cref="EncodedLength"/> bytes.</summary>
    /// <param name="destination">Destination buffer.</param>
    void Encode(Span<byte> destination);
}
