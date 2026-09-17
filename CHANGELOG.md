# Changelog

All notable changes are recorded here. This project follows Semantic Versioning.
`AssemblyVersion` remains pinned at `1.0.0.0`; releases advance `PackageVersion`.

## [Unreleased]

## [0.1.1] - 2026-09-17

### Fixed

- Use the canonical Apache 2.0 repository license text so hosting providers identify it correctly.
- No API, encoding, wire-format, or assembly-version changes.

## [0.1.0] - 2026-09-16

### Added

- Byte-compatible encoders for strings, bytes, UUIDs, booleans, signed and unsigned
  declared-width integers, IEEE-754 floats, Unix nanoseconds, nil, and end markers.
- Typed and raw composite keys, structured range bounds, raw-prefix successors, immutable
  `LexKey` values, custom `ILexKeyEncodable` parts, and a reusable buffer `Encoder`.
- Pedantic build, test, package-consumer, benchmark, NativeAOT, and GitHub Packages workflows.
