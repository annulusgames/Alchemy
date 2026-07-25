namespace Alchemy.SourceGenerator.Tests;

/// <summary>
/// Inheritance chaining. When a base class is also [AlchemySerialize], the derived
/// class must hide the base helpers with <c>new</c> and forward to them with
/// <c>base.</c> — otherwise the base class's fields silently stop round-tripping.
/// </summary>
public class InheritanceTests
{
    const string BaseAndDerived = """
        using System;
        using System.Collections.Generic;
        using Alchemy.Serialization;

        namespace Demo
        {
            [AlchemySerialize]
            public partial class BaseType
            {
                [AlchemySerializeField, NonSerialized]
                public Dictionary<string, int> fromBase = new();
            }

            [AlchemySerialize]
            public partial class DerivedType : BaseType
            {
                [AlchemySerializeField, NonSerialized]
                public HashSet<int> fromDerived = new();
            }
        }
        """;

    [Test]
    public async Task Both_types_are_generated()
    {
        var result = GeneratorUtils.Run(BaseAndDerived);
        await Assert.That(result.GeneratedSources.Length).IsEqualTo(2);
        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }

    [Test]
    public async Task Derived_type_forwards_to_the_base_implementation()
    {
        var derived = GeneratorUtils.Run(BaseAndDerived)
            .GeneratedSources.Single(s => s.HintName.Contains("DerivedType")).Text;

        await Assert.That(derived).Contains("base.__AlchemyOnBeforeSerialize();");
        await Assert.That(derived).Contains("base.__AlchemyOnAfterDeserialize();");
    }

    [Test]
    public async Task Derived_type_hides_the_base_helpers_with_new()
    {
        var derived = GeneratorUtils.Run(BaseAndDerived)
            .GeneratedSources.Single(s => s.HintName.Contains("DerivedType")).Text;

        await Assert.That(derived).Contains("protected new void __AlchemyOnBeforeSerialize()");
        await Assert.That(derived).Contains("protected new void __AlchemyOnAfterDeserialize()");
    }

    [Test]
    public async Task Base_type_does_not_forward_to_anything()
    {
        var baseText = GeneratorUtils.Run(BaseAndDerived)
            .GeneratedSources.Single(s => s.HintName.Contains("BaseType")).Text;

        await Assert.That(baseText).DoesNotContain("base.__Alchemy");
        await Assert.That(baseText).DoesNotContain("protected new void");
    }

    [Test]
    public async Task Each_level_keeps_its_own_backing_store()
    {
        // Base and derived must not share one AlchemySerializationData instance,
        // or clearing the reference table in one would corrupt the other.
        var result = GeneratorUtils.Run(BaseAndDerived);

        await Assert.That(result.AllGeneratedText).Contains("__alchemySerializationData_Demo_BaseType");
        await Assert.That(result.AllGeneratedText).Contains("__alchemySerializationData_Demo_DerivedType");
    }

    [Test]
    public async Task A_derived_type_whose_base_is_not_attributed_does_not_forward()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                public partial class PlainBase { }

                [AlchemySerialize]
                public partial class OnlyDerived : PlainBase
                {
                    [AlchemySerializeField, NonSerialized]
                    public Dictionary<string, int> map = new();
                }
            }
            """);

        await Assert.That(result.AllGeneratedText).DoesNotContain("base.__Alchemy");
        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }

    [Test]
    public async Task Attribution_is_detected_through_an_unattributed_intermediate_class()
    {
        // Base -> Middle (not attributed) -> Leaf. The generator walks the whole
        // base chain, so Leaf must still chain to Base's implementation.
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize]
                public partial class Root
                {
                    [AlchemySerializeField, NonSerialized]
                    public Dictionary<string, int> a = new();
                }

                public partial class Middle : Root { }

                [AlchemySerialize]
                public partial class Leaf : Middle
                {
                    [AlchemySerializeField, NonSerialized]
                    public HashSet<int> b = new();
                }
            }
            """);

        var leaf = result.GeneratedSources.Single(s => s.HintName.Contains("Leaf")).Text;
        await Assert.That(leaf).Contains("base.__AlchemyOnBeforeSerialize();");
        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }

    [Test]
    public async Task Three_level_chain_compiles()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize]
                public partial class L1
                {
                    [AlchemySerializeField, NonSerialized] public Dictionary<string, int> a = new();
                }

                [AlchemySerialize]
                public partial class L2 : L1
                {
                    [AlchemySerializeField, NonSerialized] public HashSet<int> b = new();
                }

                [AlchemySerialize]
                public partial class L3 : L2
                {
                    [AlchemySerializeField, NonSerialized] public System.Collections.Generic.List<string> c = new();
                }
            }
            """);

        await Assert.That(result.GeneratedSources.Length).IsEqualTo(3);
        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }
}
