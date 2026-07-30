# Debugging Serialized Data

By adding `[ShowAlchemySerializationData]` alongside `[AlchemySerialize]`, you can inspect the serialized data in the Inspector.

```cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Alchemy.Serialization;

[AlchemySerialize]
[ShowAlchemySerializationData]
public partial class AlchemySerializationExample : MonoBehaviour
{
    [AlchemySerializeField, NonSerialized]
    public Dictionary<string, GameObject> dictionary = new();
}
```

![img](../../images/img-show-serialization-data.png)
