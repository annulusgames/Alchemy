using System;
#if ALCHEMY_SUPPORT_SERIALIZATION
using System.Collections.Generic;
using Alchemy.Serialization;
#endif
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
#if ALCHEMY_SUPPORT_SERIALIZATION
    [ShowAlchemySerializationData]
    [AlchemySerialize]
#endif
    public partial class InheritedSerializeTestBase<T> : MonoBehaviour
    {
#if ALCHEMY_SUPPORT_SERIALIZATION
        [AlchemySerializeField, NonSerialized] HashSet<T> set;
#endif
    }
}
