using BenchmarkDotNet.Attributes;

namespace Cntryl.Keys.Benchmarks;

[MemoryDiagnoser]
public class EncodingBenchmarks
{
    readonly Encoder _encoder = new(64);
    readonly byte[] _prefix = "acme\0kv\0users\0profile\0"u8.ToArray();

    [Benchmark]
    public LexKey AllocateComposite() => LexKey.EncodeComposite("tenant", 42L, true);

    [Benchmark]
    public int ReuseEncoderWithoutMaterializing()
    {
        _encoder.Clear();
        _encoder.EncodeString("tenant");
        _encoder.PushSeparator();
        _encoder.EncodeInt64(42);
        _encoder.PushSeparator();
        _encoder.EncodeBoolean(true);
        return _encoder.Length;
    }

    [Benchmark]
    public byte[] PrefixEnd() => LexKey.PrefixEnd(_prefix);

    [Benchmark]
    public byte[]? PrefixSuccessor() => LexKey.PrefixSuccessor(_prefix);
}
