using UnityEngine;
#if ALCHEMY_SUPPORT_SERIALIZATION
using Alchemy.Serialization;
#endif

namespace Alchemy.Tests.EditorUI
{
#if ALCHEMY_SUPPORT_SERIALIZATION
    [AlchemySerialize]
#endif
    [DocumentationSample]
    public partial class SerializationCallbackTest : MonoBehaviour
#if ALCHEMY_SUPPORT_SERIALIZATION
        , IAlchemySerializationCallbackReceiver
#endif
    {
        public int beforeSerializeCount;
        public int afterDeserializeCount;

        public void OnBeforeSerialize()
        {
            beforeSerializeCount++;
        }

        public void OnAfterDeserialize()
        {
            afterDeserializeCount++;
        }
    }
}
