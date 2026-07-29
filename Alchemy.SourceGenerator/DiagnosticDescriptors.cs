using Microsoft.CodeAnalysis;

namespace Alchemy.SourceGenerator
{
    public static class DiagnosticDescriptors
    {
        const string Category = "GenerateAlchemySerializationData";

        public static readonly DiagnosticDescriptor MustBePartial = new(
            id: "ALCHEMY001",
            title: "AlchemySerialize class must be partial.",
            messageFormat: "AlchemySerialize class '{0}' must be partial.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor NestedNotAllow = new(
            id: "ALCHEMY002",
            title: "AlchemySerialize class must not be nested type.",
            messageFormat: "The AlchemySerialize class '{0}' must be not nested type.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor ShouldBeNonSerialized = new(
            id: "ALCHEMY003",
            title: "AlchemySerializeField should be NonSerialized to avoid duplicate serialization.",
            messageFormat: "AlchemySerializeField '{0}' should be NonSerialized to avoid duplicate serialization.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );
    }
}
