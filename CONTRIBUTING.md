# Contributing

Encoding compatibility is the primary contract. Any semantic change must update byte-level
vectors and ordering tests and be checked against `cntryl/lexkey-rs` and `docs/SPEC.md`.

Before opening a pull request, run:

```bash
dotnet restore LexKey.slnx --locked-mode
dotnet format LexKey.slnx --verify-no-changes --no-restore
dotnet build LexKey.slnx -c Release --no-restore
dotnet test LexKey.slnx -c Release --no-build
```

Pack and run the external consumer for public-surface changes. Keep `AssemblyVersion` at
`1.0.0.0`; advance `PackageVersion` only. Breaking encoding changes require a major version.
