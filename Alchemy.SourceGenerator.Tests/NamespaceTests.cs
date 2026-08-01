namespace Alchemy.SourceGenerator.Tests;

/// <summary>
/// Namespace emission — the generator reconstructs the containing namespace by hand,
/// so every namespace shape needs covering.
/// </summary>
public class NamespaceTests
{
    [Test]
    public async Task Block_scoped_namespace_is_reproduced()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Outer.Inner.Deep
            {
                [AlchemySerialize]
                public partial class Deeply
                {
                    [AlchemySerializeField, NonSerialized]
                    public Dictionary<string, int> map = new();
                }
            }
            """);

        await Assert.That(result.AllGeneratedText).Contains("namespace Outer.Inner.Deep {");
        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }

    [Test]
    public async Task File_scoped_namespace_is_supported()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace FileScoped;

            [AlchemySerialize]
            public partial class Sample
            {
                [AlchemySerializeField, NonSerialized]
                public Dictionary<string, int> map = new();
            }
            """);

        await Assert.That(result.AllGeneratedText).Contains("namespace FileScoped {");
        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }

    [Test]
    public async Task Global_namespace_emits_no_namespace_block()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            [AlchemySerialize]
            public partial class NoNamespace
            {
                [AlchemySerializeField, NonSerialized]
                public Dictionary<string, int> map = new();
            }
            """);

        await Assert.That(result.AllGeneratedText).DoesNotContain("namespace ");
        await Assert.That(result.GeneratedSources[0].HintName)
            .IsEqualTo("NoNamespace.AlchemySerializeGenerator.g.cs");
        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }

    [Test]
    public async Task Two_same_named_types_in_different_namespaces_do_not_collide()
    {
        // Both produce a type called "Sample"; the hint name must disambiguate them
        // or AddSource throws on the duplicate.
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace First
            {
                [AlchemySerialize]
                public partial class Sample
                {
                    [AlchemySerializeField, NonSerialized]
                    public Dictionary<string, int> map = new();
                }
            }

            namespace Second
            {
                [AlchemySerialize]
                public partial class Sample
                {
                    [AlchemySerializeField, NonSerialized]
                    public HashSet<int> set = new();
                }
            }
            """);

        await Assert.That(result.GeneratedSources.Length).IsEqualTo(2);
        await Assert.That(result.HasDiagnostic("AlchemySerializeGeneratorError")).IsFalse();

        var hintNames = result.GeneratedSources.Select(s => s.HintName).OrderBy(x => x).ToArray();
        await Assert.That(hintNames[0]).IsEqualTo("First.Sample.AlchemySerializeGenerator.g.cs");
        await Assert.That(hintNames[1]).IsEqualTo("Second.Sample.AlchemySerializeGenerator.g.cs");
        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }

    [Test]
    public async Task Backing_field_name_is_namespace_qualified_so_it_cannot_clash()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace My.Game
            {
                [AlchemySerialize]
                public partial class Player
                {
                    [AlchemySerializeField, NonSerialized]
                    public Dictionary<string, int> map = new();
                }
            }
            """);

        await Assert.That(result.AllGeneratedText).Contains("__alchemySerializationData_My_Game_Player");
    }

    [Test]
    public async Task Dictionary_types_are_globally_qualified_when_root_namespace_is_shadowed()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;
            using Artillery.Entities;
            using Artillery.Entities.Units;

            namespace Artillery.Artillery
            {
                public sealed class NamespaceCollision { }
            }

            namespace Artillery.Entities
            {
                public enum UnitTeam
                {
                    Ally,
                    Enemy
                }
            }

            namespace Artillery.Entities.Units
            {
                public class BasicUnit : UnityEngine.MonoBehaviour { }
            }

            namespace Artillery.Entities.Units.Configuration
            {
                [AlchemySerialize]
                public partial class UnitConfiguration
                {
                    [AlchemySerializeField, NonSerialized]
                    public Dictionary<UnitTeam, UnityEngine.GameObject> teamObjects = new();

                    [AlchemySerializeField, NonSerialized]
                    public Dictionary<int, BasicUnit> units = new();

                    [AlchemySerializeField, NonSerialized]
                    public Dictionary<UnitTeam, BasicUnit> teamUnits = new();
                }
            }
            """);

        await Assert.That(result.AllGeneratedText).Contains(
            "FromJson<global::System.Collections.Generic.Dictionary<global::Artillery.Entities.UnitTeam, global::UnityEngine.GameObject>>");
        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }
}
