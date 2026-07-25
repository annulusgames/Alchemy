#!/usr/bin/env dotnet
//
// Rebuilds Alchemy.SourceGenerator and refreshes the DLL shipped inside the Unity package.
//
//   dotnet run scripts/generator.cs
//
// Unity loads the compiled generator from Alchemy/Assets/Alchemy/Generator/, so that copy has to
// be rebuilt and committed whenever the generator source changes.

using System.Diagnostics;
using System.Runtime.CompilerServices;

if (args.Length > 0)
{
    Console.Error.WriteLine($"error: unexpected argument '{args[0]}'");
    Console.Error.WriteLine("usage: dotnet run scripts/generator.cs");
    return 2;
}

var repoRoot = FindRepositoryRoot();
var projectPath = Path.Combine(repoRoot, "Alchemy.SourceGenerator", "Alchemy.SourceGenerator.csproj");
var shippedDll = Path.Combine(repoRoot, "Alchemy", "Assets", "Alchemy", "Generator", "Alchemy.SourceGenerator.dll");

if (!File.Exists(shippedDll))
{
    Console.Error.WriteLine($"error: shipped DLL not found at {Relative(shippedDll)}");
    return 1;
}

var buildDir = Directory.CreateTempSubdirectory("alchemy-generator-").FullName;

try
{
    Console.WriteLine("Building Alchemy.SourceGenerator...");

    var exitCode = Run("dotnet", [
        "build", projectPath,
        "--configuration", "Release",
        "--output", buildDir,
        "--nologo",
        "--verbosity", "quiet"
    ]);

    if (exitCode != 0)
    {
        Console.Error.WriteLine("error: build failed");
        return 1;
    }

    var builtDll = Path.Combine(buildDir, "Alchemy.SourceGenerator.dll");

    if (ContentEquals(builtDll, shippedDll))
    {
        Console.WriteLine($"Unchanged: {Relative(shippedDll)} is already current.");
        return 0;
    }

    File.Copy(builtDll, shippedDll, overwrite: true);
    Console.WriteLine($"Updated: {Relative(shippedDll)}");
    return 0;
}
finally
{
    try { Directory.Delete(buildDir, recursive: true); } catch { /* best effort */ }
}

static bool ContentEquals(string left, string right) =>
    File.ReadAllBytes(left).AsSpan().SequenceEqual(File.ReadAllBytes(right));

static int Run(string fileName, string[] arguments)
{
    var startInfo = new ProcessStartInfo(fileName) { UseShellExecute = false };
    foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

    using var process = Process.Start(startInfo)
        ?? throw new InvalidOperationException($"could not start {fileName}");

    process.WaitForExit();
    return process.ExitCode;
}

string Relative(string path) => Path.GetRelativePath(repoRoot, path).Replace('\\', '/');

// The script is compiled in place, so the source path locates the repository without
// depending on the working directory. Falls back to walking up from the caller's directory.
static string FindRepositoryRoot([CallerFilePath] string scriptPath = "")
{
    foreach (var start in new[] { Path.GetDirectoryName(scriptPath), Environment.CurrentDirectory })
    {
        if (string.IsNullOrEmpty(start)) continue;

        for (var directory = new DirectoryInfo(start); directory != null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Alchemy.SourceGenerator")) &&
                Directory.Exists(Path.Combine(directory.FullName, "Alchemy")))
            {
                return directory.FullName;
            }
        }
    }

    throw new InvalidOperationException("could not locate the repository root");
}
