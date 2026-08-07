#if ALCHEMY_SUPPORT_SERIALIZATION
using System.Globalization;
using Unity.Serialization.Json;

namespace Alchemy.Serialization.Internal
{
    partial class AlchemyJsonAdapter : IJsonAdapter<decimal>
    {
        public void Serialize(in JsonSerializationContext<decimal> context, decimal value)
        {
            context.Writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
        }

        public decimal Deserialize(in JsonDeserializationContext<decimal> context)
        {
            var view = context.SerializedValue;
            if (view.IsNull()) return default;

            if (view.Type == TokenType.Object)
            {
                using var enumerator = view.AsObjectView().GetEnumerator();
                if (!enumerator.MoveNext())
                {
                    // Legacy value serialized by Unity.Serialization as "{}".
                    return default;
                }
            }

            // Non-empty objects reach AsStringView() and fail instead of
            // being silently converted to zero.
            return decimal.Parse(
                view.AsStringView().ToString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture);
        }
    }
}
#endif
