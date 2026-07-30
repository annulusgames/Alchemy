# Serialization Extension

To edit types that Unity does not normally serialize, such as dictionaries, use the `[AlchemySerialize]` attribute.

To use the serialization extension, install the [Unity.Serialization](https://docs.unity3d.com/Packages/com.unity.serialization@3.1/manual/index.html) package. Reflection-based serialization with Unity.Serialization may not work in AOT environments before Unity 2022.1. Refer to the package manual for details.

The following example uses Alchemy's serialization extension to serialize several types and make them editable in the Inspector:

```cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Alchemy.Serialization; // Import the Alchemy.Serialization namespace

[AlchemySerialize]
public partial class AlchemySerializationExample : MonoBehaviour
{
    // Add the [AlchemySerializeField] and [NonSerialized] attributes to the target field.
    [AlchemySerializeField, NonSerialized]
    public HashSet<GameObject> hashSet = new();

    [AlchemySerializeField, NonSerialized]
    public Dictionary<string, GameObject> dictionary = new();

    [AlchemySerializeField, NonSerialized]
    public (int, int) tuple;

    [AlchemySerializeField, NonSerialized]
    public Vector3? nullable = null;
}
```

![img](../../images/img-serialization-sample.png)

Currently, the following types can be edited in the Inspector:

- Primitive types
- UnityEngine.Object
- AnimationCurve
- Gradient
- Array
- List<>
- HashSet<>
- Dictionary<,>
- ValueTuple<>
- Nullable<>
- Classes and structs composed of the types above

For technical details on serialization, please refer to [Alchemy's Serialization Process](alchemy-serialization-process.md).
