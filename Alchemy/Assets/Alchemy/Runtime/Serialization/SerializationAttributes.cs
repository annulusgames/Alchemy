#if ALCHEMY_SUPPORT_SERIALIZATION
using System;

namespace Alchemy.Serialization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class AlchemySerializeAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class ShowAlchemySerializationDataAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class AlchemySerializeFieldAttribute : Attribute { }
}
#endif
