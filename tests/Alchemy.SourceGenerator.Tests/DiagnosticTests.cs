using Microsoft.CodeAnalysis;

namespace Alchemy.SourceGenerator.Tests;

/// <summary>
/// The three diagnostics declared in <see cref="DiagnosticDescriptors"/>.
/// </summary>
public class DiagnosticTests
{
    [Test]
    public async Task ALCHEMY001_is_reported_for_a_non_partial_type()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize]
                public class NotPartial
                {
                    [AlchemySerializeField, NonSerialized]
                    public Dictionary<string, int> map = new();
                }
            }
            """);

        await Assert.That(result.HasDiagnostic("ALCHEMY001")).IsTrue();

        var diagnostic = result.GeneratorDiagnostics.Single(d => d.Id == "ALCHEMY001");
        await Assert.That(diagnostic.Severity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(diagnostic.GetMessage()).Contains("NotPartial");
    }

    [Test]
    public async Task ALCHEMY001_suppresses_generation_for_that_type()
    {
        var result = GeneratorUtils.Run("""
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize]
                public class NotPartial { }
            }
            """);

        await Assert.That(result.GeneratedSources).IsEmpty();
    }

    [Test]
    public async Task ALCHEMY002_is_reported_for_a_nested_type()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                public partial class Outer
                {
                    [AlchemySerialize]
                    public partial class Inner
                    {
                        [AlchemySerializeField, NonSerialized]
                        public Dictionary<string, int> map = new();
                    }
                }
            }
            """);

        await Assert.That(result.HasDiagnostic("ALCHEMY002")).IsTrue();
        await Assert.That(result.GeneratedSources).IsEmpty();

        var diagnostic = result.GeneratorDiagnostics.Single(d => d.Id == "ALCHEMY002");
        await Assert.That(diagnostic.Severity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(diagnostic.GetMessage()).Contains("Inner");
    }

    [Test]
    public async Task ALCHEMY003_warns_when_the_field_is_not_marked_NonSerialized()
    {
        // Without [NonSerialized] Unity serializes the field itself as well as the
        // JSON payload, so the value is stored twice and can diverge.
        var result = GeneratorUtils.Run("""
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize]
                public partial class Warned
                {
                    [AlchemySerializeField]
                    public Dictionary<string, int> map = new();
                }
            }
            """);

        await Assert.That(result.HasDiagnostic("ALCHEMY003")).IsTrue();

        var diagnostic = result.GeneratorDiagnostics.Single(d => d.Id == "ALCHEMY003");
        await Assert.That(diagnostic.Severity).IsEqualTo(DiagnosticSeverity.Warning);
        await Assert.That(diagnostic.GetMessage()).Contains("map");
    }

    [Test]
    public async Task ALCHEMY003_still_generates_the_code()
    {
        // It is a warning, not an error — generation must continue.
        var result = GeneratorUtils.Run("""
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize]
                public partial class Warned
                {
                    [AlchemySerializeField]
                    public Dictionary<string, int> map = new();
                }
            }
            """);

        await Assert.That(result.GeneratedSources.Length).IsEqualTo(1);
        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }

    [Test]
    public async Task No_diagnostic_when_NonSerialized_is_present()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize]
                public partial class Clean
                {
                    [AlchemySerializeField, NonSerialized]
                    public Dictionary<string, int> map = new();
                }
            }
            """);

        await Assert.That(result.GeneratorDiagnostics).IsEmpty();
    }

    [Test]
    public async Task Namespace_qualified_NonSerialized_is_recognised()
    {
        // The bare spelling is covered by No_diagnostic_when_NonSerialized_is_present.
        var result = GeneratorUtils.Run("""
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize]
                public partial class Spelled
                {
                    [AlchemySerializeField, System.NonSerialized]
                    public Dictionary<string, int> map = new();
                }
            }
            """);

        await Assert.That(result.HasDiagnostic("ALCHEMY003")).IsFalse();
    }

    [Test]
    public async Task The_generator_never_crashes_on_well_formed_input()
    {
        // A generator crash surfaces as the catch-all "AlchemySerializeGeneratorError".
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize]
                public partial class Fine
                {
                    [AlchemySerializeField, NonSerialized]
                    public Dictionary<string, int> map = new();
                }
            }
            """);

        await Assert.That(result.HasDiagnostic("AlchemySerializeGeneratorError")).IsFalse();
    }
}
