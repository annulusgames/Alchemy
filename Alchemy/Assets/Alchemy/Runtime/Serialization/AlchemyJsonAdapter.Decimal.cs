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

            // Unity.Serialization treats System.Decimal as an empty object ("{}") by default.
            if (view.Type == TokenType.Object) return default;

            return decimal.Parse(
                view.AsStringView().ToString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture);
        }
    }
}
#endif
