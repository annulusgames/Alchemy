#if ALCHEMY_SUPPORT_SERIALIZATION
using Unity.Serialization.Json;
using UnityEngine;

namespace Alchemy.Serialization.Internal
{
    partial class AlchemyJsonAdapter : IJsonAdapter<AnimationCurve>, IJsonAdapter<Keyframe>
    {
        public AnimationCurve Deserialize(in JsonDeserializationContext<AnimationCurve> context)
        {
            if (context.SerializedValue.IsNull())
            {
                return null;
            }

            var view = context.SerializedValue.AsObjectView();
            var animationCurve = new AnimationCurve();

            if (view.TryGetMember("keys", out var keys)) animationCurve.keys = context.DeserializeValue<Keyframe[]>(keys.Value());
            if (view.TryGetMember("postWrapMode", out var postWrapMode)) animationCurve.postWrapMode = context.DeserializeValue<WrapMode>(postWrapMode.Value());
            if (view.TryGetMember("preWrapMode", out var preWrapMode)) animationCurve.preWrapMode = context.DeserializeValue<WrapMode>(preWrapMode.Value());

            return animationCurve;
        }

        public Keyframe Deserialize(in JsonDeserializationContext<Keyframe> context)
        {
            var view = context.SerializedValue.AsObjectView();
            var keyframe = default(Keyframe);

            if (view.TryGetMember("time", out var time)) keyframe.time = time.Value().AsFloat();
            if (view.TryGetMember("value", out var value)) keyframe.value = value.Value().AsFloat();
            if (view.TryGetMember("inTangent", out var inTangent)) keyframe.inTangent = inTangent.Value().AsFloat();
            if (view.TryGetMember("outTangent", out var outTangent)) keyframe.outTangent = outTangent.Value().AsFloat();
            if (view.TryGetMember("inWeight", out var inWeight)) keyframe.inWeight = inWeight.Value().AsFloat();
            if (view.TryGetMember("outWeight", out var outWeight)) keyframe.outWeight = outWeight.Value().AsFloat();
            if (view.TryGetMember("weightedMode", out var weightedMode)) keyframe.weightedMode = context.DeserializeValue<WeightedMode>(weightedMode.Value());
#pragma warning disable 618
            if (view.TryGetMember("tangentMode", out var tangentMode)) keyframe.tangentMode = context.DeserializeValue<int>(tangentMode.Value());
#pragma warning restore 618

            return keyframe;
        }

        public void Serialize(in JsonSerializationContext<AnimationCurve> context, AnimationCurve value)
        {
            if (value == null)
            {
                context.Writer.WriteNull();
                return;
            }

            context.Writer.WriteBeginObject();
            context.SerializeValue("keys", value.keys);
            context.SerializeValue("postWrapMode", value.postWrapMode);
            context.SerializeValue("preWrapMode", value.preWrapMode);
            context.Writer.WriteEndObject();
        }

        public void Serialize(in JsonSerializationContext<Keyframe> context, Keyframe value)
        {
            context.Writer.WriteBeginObject();
            context.SerializeValue("time", value.time);
            context.SerializeValue("value", value.value);
            context.SerializeValue("inTangent", value.inTangent);
            context.SerializeValue("outTangent", value.outTangent);
            context.SerializeValue("inWeight", value.inWeight);
            context.SerializeValue("outWeight", value.outWeight);
            context.SerializeValue("weightedMode", value.weightedMode);
#pragma warning disable 618
            context.SerializeValue("tangentMode", value.tangentMode);
#pragma warning restore 618
            context.Writer.WriteEndObject();
        }
    }
}
#endif
