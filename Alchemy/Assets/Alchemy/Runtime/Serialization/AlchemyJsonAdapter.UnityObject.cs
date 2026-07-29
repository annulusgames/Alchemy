#if ALCHEMY_SUPPORT_SERIALIZATION
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Serialization.Json;

namespace Alchemy.Serialization.Internal
{
    partial class AlchemyJsonAdapter : IContravariantJsonAdapter<UnityEngine.Object>, IJsonAdapter<UnityEngine.Object>
    {
        public AlchemyJsonAdapter(IList<UnityEngine.Object> objectReferenceList)
        {
            this.ObjectReferenceList = objectReferenceList;
        }

        IList<UnityEngine.Object> ObjectReferenceList { get; }

        public object Deserialize(IJsonDeserializationContext context)
        {
            return DeserializeInternal(context.SerializedValue);
        }

        public UnityEngine.Object Deserialize(in JsonDeserializationContext<UnityEngine.Object> context)
        {
            return DeserializeInternal(context.SerializedValue);
        }

        public void Serialize(IJsonSerializationContext context, UnityEngine.Object value)
        {
            SerializeInternal(context.Writer, value);
        }

        public void Serialize(in JsonSerializationContext<UnityEngine.Object> context, UnityEngine.Object value)
        {
            SerializeInternal(context.Writer, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SerializeInternal(JsonWriter writer, UnityEngine.Object value)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var index = ObjectReferenceList.IndexOf(value);
            if (index == -1)
            {
                ObjectReferenceList.Add(value);
                index = ObjectReferenceList.Count - 1;
            }
            writer.WriteValue(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        UnityEngine.Object DeserializeInternal(SerializedValueView view)
        {
            if (view.IsNull()) return null;

            var index = view.AsInt32();
            if (index < 0 || index >= ObjectReferenceList.Count) return null;
            return ObjectReferenceList[index];
        }
    }
}
#endif
