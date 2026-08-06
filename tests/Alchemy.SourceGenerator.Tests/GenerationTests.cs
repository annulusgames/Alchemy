namespace Alchemy.SourceGenerator.Tests;

/// <summary>
/// Core generation behaviour: what the generator emits for well-formed input,
/// and that the emitted code actually compiles.
/// </summary>
public class GenerationTests
{
    const string Simple = """
        using System;
        using System.Collections.Generic;
        using Alchemy.Serialization;

        namespace Demo
        {
            [AlchemySerialize]
            public partial class Sample
            {
                [AlchemySerializeField, NonSerialized]
                public Dictionary<string, int> map = new();
            }
        }
        """;

    [Test]
    public async Task Emits_exactly_one_source_file_per_attributed_type()
    {
        var result = GeneratorUtils.Run(Simple);
        await Assert.That(result.GeneratedSources.Length).IsEqualTo(1);
    }

    [Test]
    public async Task Generated_code_compiles_cleanly()
    {
        var result = GeneratorUtils.Run(Simple);
        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }

    [Test]
    public async Task Hint_name_is_the_fully_qualified_type_name()
    {
        var result = GeneratorUtils.Run(Simple);
        await Assert.That(result.GeneratedSources[0].HintName)
            .IsEqualTo("Demo.Sample.AlchemySerializeGenerator.g.cs");
    }

    [Test]
    public async Task Implements_ISerializationCallbackReceiver_explicitly()
    {
        var text = GeneratorUtils.Run(Simple).AllGeneratedText;

        await Assert.That(text).Contains("partial class Sample : global::UnityEngine.ISerializationCallbackReceiver");
        await Assert.That(text).Contains("void global::UnityEngine.ISerializationCallbackReceiver.OnBeforeSerialize()");
        await Assert.That(text).Contains("void global::UnityEngine.ISerializationCallbackReceiver.OnAfterDeserialize()");
    }

    [Test]
    public async Task Round_trips_each_field_through_SerializationHelper()
    {
        var text = GeneratorUtils.Run(Simple).AllGeneratedText;

        await Assert.That(text).Contains("SerializationHelper.ToJson(this.map");
        await Assert.That(text).Contains("SerializationHelper.FromJson<");
        await Assert.That(text).Contains(".map.isCreated = true;");
    }

    [Test]
    public async Task Clears_the_UnityObject_reference_table_before_each_serialize()
    {
        // The reference table is index-based; failing to clear it would make indices
        // drift on every re-serialize.
        var text = GeneratorUtils.Run(Simple).AllGeneratedText;
        await Assert.That(text).Contains("UnityObjectReferences.Clear();");
    }

    [Test]
    public async Task Wraps_every_field_in_try_catch_so_one_bad_field_cannot_abort_the_rest()
    {
        var text = GeneratorUtils.Run(Simple).AllGeneratedText;
        await Assert.That(text).Contains("catch (global::System.Exception ex)");
        await Assert.That(text).Contains("global::UnityEngine.Debug.LogException(ex);");
    }

    [Test]
    public async Task Backing_store_is_hidden_from_the_inspector_by_default()
    {
        var text = GeneratorUtils.Run(Simple).AllGeneratedText;
        await Assert.That(text).Contains("[global::UnityEngine.HideInInspector, global::UnityEngine.SerializeField]");
    }

    [Test]
    public async Task ShowAlchemySerializationData_replaces_HideInInspector_with_a_visible_readonly_field()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize, ShowAlchemySerializationData]
                public partial class Shown
                {
                    [AlchemySerializeField, NonSerialized]
                    public Dictionary<string, int> map = new();
                }
            }
            """);

        var text = result.AllGeneratedText;
        await Assert.That(text).Contains("global::Alchemy.Inspector.LabelText(\"Alchemy Serialization Data (Demo.Shown)\")");
        await Assert.That(text).Contains("global::Alchemy.Inspector.ReadOnly");
        await Assert.That(text).DoesNotContain("[global::UnityEngine.HideInInspector, global::UnityEngine.SerializeField] private AlchemySerializationData");
        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }

    [Test]
    public async Task Multiple_fields_each_get_their_own_serialization_slot()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize]
                public partial class Many
                {
                    [AlchemySerializeField, NonSerialized] public Dictionary<string, int> a = new();
                    [AlchemySerializeField, NonSerialized] public HashSet<int> b = new();
                    [AlchemySerializeField, NonSerialized] public (int, int) c;
                }
            }
            """);

        var text = result.AllGeneratedText;
        await Assert.That(text).Contains("public Item a = new();");
        await Assert.That(text).Contains("public Item b = new();");
        await Assert.That(text).Contains("public Item c = new();");
        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }

    [Test]
    public async Task Multiple_declarators_on_one_field_line_are_all_captured()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize]
                public partial class Decl
                {
                    [AlchemySerializeField, NonSerialized]
                    public Dictionary<string, int> x = new(), y = new();
                }
            }
            """);

        var text = result.AllGeneratedText;
        await Assert.That(text).Contains("public Item x = new();");
        await Assert.That(text).Contains("public Item y = new();");
        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }

    [Test]
    public async Task Fields_without_the_attribute_are_ignored()
    {
        var text = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize]
                public partial class Mixed
                {
                    [AlchemySerializeField, NonSerialized] public Dictionary<string, int> tracked = new();
                    public int untracked;
                }
            }
            """).AllGeneratedText;

        await Assert.That(text).Contains("public Item tracked = new();");
        await Assert.That(text).DoesNotContain("untracked");
    }

    [Test]
    public async Task A_type_with_no_attributed_fields_still_produces_valid_code()
    {
        var result = GeneratorUtils.Run("""
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize]
                public partial class Empty { }
            }
            """);

        await Assert.That(result.GeneratedSources.Length).IsEqualTo(1);
        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }

    [Test]
    public async Task Source_without_the_attribute_generates_nothing()
    {
        var result = GeneratorUtils.Run("""
            namespace Demo
            {
                public partial class Untouched
                {
                    public int value;
                }
            }
            """);

        await Assert.That(result.GeneratedSources).IsEmpty();
        await Assert.That(result.GeneratorDiagnostics).IsEmpty();
    }

    [Test]
    public async Task Private_and_protected_fields_are_supported()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize]
                public partial class Access
                {
                    [AlchemySerializeField, NonSerialized] private Dictionary<string, int> priv = new();
                    [AlchemySerializeField, NonSerialized] protected HashSet<int> prot = new();
                }
            }
            """);

        var text = result.AllGeneratedText;
        await Assert.That(text).Contains("public Item priv = new();");
        await Assert.That(text).Contains("public Item prot = new();");
        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }

    [Test]
    public async Task Generation_is_deterministic_across_runs()
    {
        var first = GeneratorUtils.Run(Simple).AllGeneratedText;
        var second = GeneratorUtils.Run(Simple).AllGeneratedText;
        await Assert.That(first).IsEqualTo(second);
    }

}
