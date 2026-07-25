# Alchemy.SourceGenerator

Source generator for Alchemy.

# How to update the shipped DLL

Unity consumes the compiled generator at
`Alchemy/Assets/Alchemy/Generator/Alchemy.SourceGenerator.dll`. After changing the generator, rebuild it with:

```bash
dotnet run scripts/generator.cs
```
