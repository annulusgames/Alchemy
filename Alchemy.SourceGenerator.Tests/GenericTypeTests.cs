namespace Alchemy.SourceGenerator.Tests;

/// <summary>
/// Generic targets, and the naming contract that ties the generator to the editor.
/// </summary>
public class GenericTypeTests
{
    /// <summary>
    /// Reproduces the backing-field name the editor computes at runtime in
    /// <c>InspectorHelper.CreateMemberElement</c>:
    /// <code>
    /// var declaredType = fieldInfo.DeclaringType;
    /// if (declaredType.IsConstructedGenericType) declaredType = declaredType.GetGenericTypeDefinition();
    /// var dataName = "__alchemySerializationData_" + declaredType.FullName.Replace("`", "").Replace(".", "_");
    /// </code>
    /// The generator builds the same name from Roslyn symbols by a completely different
    /// route, so the two must be pinned together or the inspector silently fails to find
    /// the serialized data.
    /// </summary>
    static string ExpectedNameFromEditorSide(string reflectionFullName) =>
        "__alchemySerializationData_" + reflectionFullName.Replace("`", "").Replace(".", "_");

    [Test]
    public async Task Single_type_parameter_generates_and_compiles()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo.Deep
            {
                [AlchemySerialize]
                public partial class Gen<T>
                {
                    [AlchemySerializeField, NonSerialized]
                    public List<T> items = new();
                }
            }
            """);

        await Assert.That(result.AllGeneratedText).Contains("partial class Gen<T>");
        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }

    [Test]
    public async Task Backing_field_name_matches_what_the_editor_looks_up_for_one_type_parameter()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo.Deep
            {
                [AlchemySerialize]
                public partial class Gen<T>
                {
                    [AlchemySerializeField, NonSerialized]
                    public List<T> items = new();
                }
            }
            """);

        // typeof(Gen<>).FullName == "Demo.Deep.Gen`1"
        var expected = ExpectedNameFromEditorSide("Demo.Deep.Gen`1");
        await Assert.That(expected).IsEqualTo("__alchemySerializationData_Demo_Deep_Gen1");
        await Assert.That(result.AllGeneratedText).Contains(expected);
    }

    [Test]
    public async Task Backing_field_name_matches_what_the_editor_looks_up_for_two_type_parameters()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize]
                public partial class Pair<TKey, TValue>
                {
                    [AlchemySerializeField, NonSerialized]
                    public Dictionary<TKey, TValue> map = new();
                }
            }
            """);

        // typeof(Pair<,>).FullName == "Demo.Pair`2"
        var expected = ExpectedNameFromEditorSide("Demo.Pair`2");
        await Assert.That(expected).IsEqualTo("__alchemySerializationData_Demo_Pair2");
        await Assert.That(result.AllGeneratedText).Contains(expected);
    }

    [Test]
    public async Task Backing_field_name_matches_for_a_non_generic_type()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize]
                public partial class Plain
                {
                    [AlchemySerializeField, NonSerialized]
                    public Dictionary<string, int> map = new();
                }
            }
            """);

        var expected = ExpectedNameFromEditorSide("Demo.Plain");
        await Assert.That(expected).IsEqualTo("__alchemySerializationData_Demo_Plain");
        await Assert.That(result.AllGeneratedText).Contains(expected);
    }

    [Test]
    public async Task Type_parameters_are_carried_onto_the_partial_declaration()
    {
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize]
                public partial class Pair<TKey, TValue>
                {
                    [AlchemySerializeField, NonSerialized]
                    public Dictionary<TKey, TValue> map = new();
                }
            }
            """);

        await Assert.That(result.AllGeneratedText).Contains("partial class Pair<TKey, TValue>");
        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }

    [Test]
    public async Task Generic_type_with_a_constraint_compiles()
    {
        // The generated partial deliberately omits the where-clause, which is legal C#
        // as long as at least one declaration states it.
        var result = GeneratorUtils.Run("""
            using System;
            using System.Collections.Generic;
            using Alchemy.Serialization;

            namespace Demo
            {
                [AlchemySerialize]
                public partial class Constrained<T> where T : class, new()
                {
                    [AlchemySerializeField, NonSerialized]
                    public List<T> items = new();
                }
            }
            """);

        await Assert.That(result.DescribeCompilationErrors()).IsEqualTo("<none>");
    }
}
