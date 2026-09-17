# lexkey-dotnet

`lexkey-dotnet` is the .NET implementation of the language-independent LexKey encoding
specification used by [`lexkey-rs`](https://github.com/cntryl/lexkey-rs). It produces
byte-for-byte compatible, lexicographically sortable keys for typed numbers, strings,
UUIDs, time values, composites, and storage ranges.

## Install

The package is published to Cntryl's GitHub Packages feed:

```xml
<PackageReference Include="Cntryl.LexKey" Version="0.1.0" />
```

The package namespace is `Cntryl.Keys` so the primary type remains the unambiguous
`LexKey` rather than colliding with its namespace.

## Quick start

```csharp
using Cntryl.Keys;

var userKey = LexKey.EncodeComposite("tenant", "user", userId);
var numeric = LexKey.EncodeInt64(42);
var (lower, upper) = LexKey.EncodeRangeBounds(userKey.AsSpan());
```

Typed numeric widths are preserved: `byte`, `ushort`, `uint`, and `ulong` encode to
1, 2, 4, and 8 bytes respectively, and signed/floating-point types behave likewise.
Normalize values to a common explicit width when a schema needs cross-width ordering.

For hot paths, reuse an `Encoder`:

```csharp
var encoder = new Encoder(64);
encoder.EncodeString("tenant");
encoder.PushSeparator();
encoder.EncodeInt64(42);
var key = encoder.ToLexKey();
encoder.Clear();
```

The composite convenience API uses typed, stack-friendly descriptors and allocates only
the immutable result for strings, numbers, UUIDs, existing keys, and custom encoders.
The reusable encoder can write without managed allocations after its buffer reaches capacity;
materializing a `LexKey` or array intentionally creates an owned copy.

## Range helpers

- `PrefixEnd(P)` returns structured `P || 0xFF`.
- `PrefixSuccessor(P)` returns the finite upper bound for arbitrary raw prefixes by
  incrementing the final non-`0xFF` byte and truncating.
- `PrefixScanBounds(P)` returns `(P, PrefixSuccessor(P))`.
- `PrefixRangeBounds(P)` returns structured `(P || 0x00, P || 0xFF)`.
- `EncodeRangeLower` and `EncodeRangeUpper` preserve the Rust distinction between a
  missing row bound and an explicitly empty row bound.

These operations are intentionally separate. `P || 0xFF` is not a safe upper bound for
arbitrary raw prefixes whose children may begin with `0xFF`.

## Compatibility and quality gates

- exact portable byte vectors and ordering tests
- immutable input/output ownership
- warnings-as-errors and full .NET analyzers
- XML documentation for every shipped public member
- locked restores and deterministic SourceLink packages
- external consumer restored only from freshly packed artifacts
- whole-assembly trimming and NativeAOT validation

`AssemblyVersion` remains `1.0.0.0`; package versions advance independently. The project
is licensed under Apache-2.0.
