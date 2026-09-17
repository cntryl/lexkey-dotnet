using Cntryl.Keys;

var key = LexKey.EncodeComposite("tenant", 42L, true);
if (key.ToHexString() != "74656e616e7400800000000000002a0001")
    throw new InvalidOperationException("Packed composite encoding did not match the contract.");
if (!key.AsMemory().Span.SequenceEqual(key.AsSpan()))
    throw new InvalidOperationException("The allocation-free memory view did not match the encoded key.");

var uuid = LexKey.EncodeGuid(Guid.Parse("550e8400-e29b-41d4-a716-446655440000"));
if (uuid.ToHexString() != "550e8400e29b41d4a716446655440000")
    throw new InvalidOperationException("Packed UUID encoding did not use RFC-4122 network order.");

if (typeof(LexKey).Assembly.GetName().Version != new Version(1, 0, 0, 0))
    throw new InvalidOperationException("Cntryl.LexKey assembly version must remain 1.0.0.0.");

Console.WriteLine(typeof(LexKey).FullName);
