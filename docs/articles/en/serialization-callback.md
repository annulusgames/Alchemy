# Serialization Callbacks

When you add `[AlchemySerialize]`, Alchemy's source generator automatically implements `ISerializationCallbackReceiver`. Therefore, you cannot implement that interface yourself to add callbacks.

Instead, implement Alchemy's `IAlchemySerializationCallbackReceiver` interface when using `[AlchemySerialize]`.

```cs
[AlchemySerialize]
public partial class AlchemySerializationSample : MonoBehaviour, IAlchemySerializationCallbackReceiver
{
    public void OnAfterDeserialize()
    {
        Debug.Log("OnAfterDeserialize");
    }

    public void OnBeforeSerialize()
    {
        Debug.Log("OnBeforeSerialize");
    }
}
```
