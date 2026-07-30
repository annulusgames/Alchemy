using System;
#if ALCHEMY_SUPPORT_SERIALIZATION
using Alchemy.Serialization;
#endif

namespace Alchemy.Tests.Inspector
{
#if ALCHEMY_SUPPORT_SERIALIZATION
    [ShowAlchemySerializationData]
    [AlchemySerialize]
#endif
    public partial class InheritedSerializeTest : InheritedSerializeTestBase<string>
    {
#if ALCHEMY_SUPPORT_SERIALIZATION
        [AlchemySerializeField, NonSerialized] int? nullableInt;
#endif
    }
}
