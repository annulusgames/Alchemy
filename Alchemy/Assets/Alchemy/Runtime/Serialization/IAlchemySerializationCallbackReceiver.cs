#if ALCHEMY_SUPPORT_SERIALIZATION
namespace Alchemy.Serialization
{
    public interface IAlchemySerializationCallbackReceiver
    {
        void OnBeforeSerialize();
        void OnAfterDeserialize();
    }
}
#endif
