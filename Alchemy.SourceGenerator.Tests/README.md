# Alchemy.SourceGenerator.Tests

Tests for `AlchemySerializeGenerator`, using [TUnit](https://tunit.dev/).

```bash
cd Alchemy.SourceGenerator.Tests
dotnet run
```

TUnit runs on `Microsoft.Testing.Platform`, so this project is an executable. Use `dotnet run`,
not `dotnet test`.

```bash
dotnet run --treenode-filter "/*/*/GenerationTests/*"   # a single class
dotnet run --coverage
dotnet run --report-trx
```
