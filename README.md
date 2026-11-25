# LexKey .NET

LexKey turns typed values into lexicographically sortable byte keys for ordered
storage and range scans on .NET 11. It implements the same encoding as
[LexKey for Rust](https://github.com/cntryl/lexkey-rs), so the two implementations
produce the same bytes for the same typed values.

## Install

The package is available from the Cntryl GitHub Packages feed. Configure
[GitHub Packages authentication](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry)
with a classic personal access token that has `read:packages`.

```sh
dotnet nuget add source https://nuget.pkg.github.com/cntryl/index.json --name cntryl
dotnet add package Cntryl.LexKey
```

## Quick start

Encode a partition, an individual key, and the bounds for scanning that partition.

```csharp
using Cntryl.Keys;

var partition = LexKey.EncodeComposite("tenant-1", "user");
var key = LexKey.EncodeComposite("tenant-1", "user", 42L);
var (lower, upper) = LexKey.EncodeRangeBounds(partition.AsSpan());
```

Numeric widths are part of the encoding. Pick a consistent width for each field
that needs a predictable sort order. Pass `key.AsMemory()` to a storage API when
it accepts read-only memory, or use `ToArray()` when the caller needs an owned
mutable copy. For repeated writes, reuse an `Encoder` and call `Clear()` between
keys.

## Range helpers

| Task | API |
| --- | --- |
| Scan an arbitrary byte prefix | `PrefixScanBounds` returns the prefix and its finite successor if one exists. |
| Scan a structured partition | `EncodeRangeBounds` returns `LexKey` bounds. `PrefixRangeBounds` returns structured byte bounds. |
| Encode either side of a bounded row range | `EncodeRangeLower` and `EncodeRangeUpper` distinguish an absent bound from an explicitly empty row. |

See the [encoding specification](docs/SPEC.md) for byte layout, ordering, and
the distinction between structured ranges and raw prefixes.

## Development

Use the SDK specified in [global.json](global.json).

```sh
dotnet restore LexKey.slnx --locked-mode
dotnet test LexKey.slnx -c Release --no-restore
```

See [Contributing](CONTRIBUTING.md) for compatibility checks and the
[changelog](CHANGELOG.md) for release history.

LexKey .NET is licensed under [Apache 2.0](LICENSE).
