# Alchemy Serialization Process

Adding `[AlchemySerialize]` to a target type causes Alchemy's source generator to implement `ISerializationCallbackReceiver`. It collects all fields marked with `[AlchemySerializeField]` and uses the Unity.Serialization package to serialize their data to JSON. Because references to `UnityEngine.Object` instances cannot be represented directly in JSON, Alchemy stores them in a separate list and writes their indices to the JSON data.

For example, consider the following class:

```cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Alchemy.Serialization;

[AlchemySerialize]
public partial class AlchemySerializationExample : MonoBehaviour
{
    [AlchemySerializeField, NonSerialized]
    public Dictionary<string, GameObject> dictionary = new();
}
```

Alchemy generates code similar to the following:

```cs
partial class AlchemySerializationExample : global::UnityEngine.ISerializationCallbackReceiver
{
    [global::System.Serializable]
    sealed class AlchemySerializationData
    {
        [global::System.Serializable]
        public sealed class Item
        {
            [global::UnityEngine.HideInInspector] public bool isCreated;
            [global::UnityEngine.TextArea] public string data;
        }

        public Item dictionary = new();

        [global::UnityEngine.SerializeField] private global::System.Collections.Generic.List<UnityEngine.Object> unityObjectReferences = new();

        public global::System.Collections.Generic.IList<UnityEngine.Object> UnityObjectReferences => unityObjectReferences;
    }

    [global::UnityEngine.HideInInspector, global::UnityEngine.SerializeField] private AlchemySerializationData alchemySerializationData =  new();

    void global::UnityEngine.ISerializationCallbackReceiver.OnBeforeSerialize()
    {
        if (this is global::Alchemy.Serialization.IAlchemySerializationCallbackReceiver receiver) receiver.OnBeforeSerialize();
        alchemySerializationData.UnityObjectReferences.Clear();
        
        try
        {
            alchemySerializationData.dictionary.data = global::Alchemy.Serialization.Internal.SerializationHelper.ToJson(this.dictionary , alchemySerializationData.UnityObjectReferences);
            alchemySerializationData.dictionary.isCreated = true;
        }
        catch (global::System.Exception ex)
        {
            global::UnityEngine.Debug.LogException(ex);
        }
    }

    void global::UnityEngine.ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        try 
        {
            if (alchemySerializationData.dictionary.isCreated)
            {
                this.dictionary = global::Alchemy.Serialization.Internal.SerializationHelper.FromJson<System.Collections.Generic.Dictionary<string, UnityEngine.GameObject>>(alchemySerializationData.dictionary.data, alchemySerializationData.UnityObjectReferences);
            }
        }
        catch (global::System.Exception ex)
        {
            global::UnityEngine.Debug.LogException(ex);
        }

        if (this is global::Alchemy.Serialization.IAlchemySerializationCallbackReceiver receiver) receiver.OnAfterDeserialize();
    }
}
```

Using `[AlchemySerializeField]` adds serialization and deserialization overhead. Use it only for fields that Unity cannot serialize normally.
