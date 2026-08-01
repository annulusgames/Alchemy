using System;
using System.Collections.Generic;
using UnityEngine;
#if ALCHEMY_SUPPORT_SERIALIZATION
using Alchemy.Serialization;
#endif

namespace Alchemy.Tests.EditorUI
{
#if ALCHEMY_SUPPORT_SERIALIZATION
    [AlchemySerialize]
    [ShowAlchemySerializationData]
#endif
    [DocumentationSample]
    public partial class ShowAlchemySerializationDataTest : MonoBehaviour
    {
#if ALCHEMY_SUPPORT_SERIALIZATION
        [AlchemySerializeField, NonSerialized]
        public Dictionary<string, GameObject> dictionary = new();
#endif
    }
}
