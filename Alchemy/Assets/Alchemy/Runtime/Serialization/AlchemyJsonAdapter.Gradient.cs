#if ALCHEMY_SUPPORT_SERIALIZATION
using Unity.Serialization.Json;
using UnityEngine;

namespace Alchemy.Serialization.Internal
{
    partial class AlchemyJsonAdapter : IJsonAdapter<Gradient>, IJsonAdapter<GradientColorKey>, IJsonAdapter<GradientAlphaKey>
    {
        public Gradient Deserialize(in JsonDeserializationContext<Gradient> context)
        {
            if (context.SerializedValue.IsNull()) return null;

            var view = context.SerializedValue.AsObjectView();
            var gradient = new Gradient();
            if (view.TryGetMember("colorKeys", out var colorKeys)) gradient.colorKeys = context.DeserializeValue<GradientColorKey[]>(colorKeys.Value());
            if (view.TryGetMember("alphaKeys", out var alphaKeys)) gradient.alphaKeys = context.DeserializeValue<GradientAlphaKey[]>(alphaKeys.Value());
            if (view.TryGetMember("mode", out var mode)) gradient.mode = context.DeserializeValue<GradientMode>(mode.Value());
#if UNITY_2022_2_OR_NEWER
            if (view.TryGetMember("colorSpace", out var colorSpace)) gradient.colorSpace = context.DeserializeValue<ColorSpace>(colorSpace.Value());
#endif

            return gradient;
        }

        public GradientColorKey Deserialize(in JsonDeserializationContext<GradientColorKey> context)
        {
            var colorKey = default(GradientColorKey);
            var color = context.SerializedValue.AsObjectView().GetMember("color").Value();
            colorKey.color = context.DeserializeValue<Color>(color);
            var time = context.SerializedValue.AsObjectView().GetMember("time").Value();
            colorKey.time = time.AsFloat();
            return colorKey;
        }

        public GradientAlphaKey Deserialize(in JsonDeserializationContext<GradientAlphaKey> context)
        {
            var alphaKey = default(GradientAlphaKey);
            var alpha = context.SerializedValue.AsObjectView().GetMember("alpha").Value();
            alphaKey.alpha = alpha.AsFloat();
            var time = context.SerializedValue.AsObjectView().GetMember("time").Value();
            alphaKey.time = time.AsFloat();
            return alphaKey;
        }

        public void Serialize(in JsonSerializationContext<Gradient> context, Gradient value)
        {
            if (value == null)
            {
                context.Writer.WriteNull();
                return;
            }

            context.Writer.WriteBeginObject();
            context.SerializeValue("colorKeys", value.colorKeys);
            context.SerializeValue("alphaKeys", value.alphaKeys);
            context.SerializeValue("mode", value.mode);
#if UNITY_2022_2_OR_NEWER
            context.SerializeValue("colorSpace", value.colorSpace);
#endif
            context.Writer.WriteEndObject();
        }

        public void Serialize(in JsonSerializationContext<GradientColorKey> context, GradientColorKey value)
        {
            context.Writer.WriteBeginObject();
            context.SerializeValue("color", value.color);
            context.SerializeValue("time", value.time);
            context.Writer.WriteEndObject();
        }

        public void Serialize(in JsonSerializationContext<GradientAlphaKey> context, GradientAlphaKey value)
        {
            context.Writer.WriteBeginObject();
            context.SerializeValue("alpha", value.alpha);
            context.SerializeValue("time", value.time);
            context.Writer.WriteEndObject();
        }
    }
}
#endif
