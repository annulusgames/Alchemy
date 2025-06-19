# SerializeReference

Alchemy supports Unity's `[SerializeReference]`. By adding the `[SerializeReference]` attribute, you can edit interfaces or abstract classes in the Inspector.

```cs
using System;
using UnityEngine;

public interface IExample { }

[Serializable]
public sealed class ExampleA : IExample
{
    public float alpha;
}

[Serializable]
public sealed class ExampleB : IExample
{
    public Vector3 beta;
}

[Serializable]
public sealed class ExampleC : IExample
{
    public GameObject gamma;
}

public class SerializeReferenceExample : MonoBehaviour
{
    [SerializeReference] public IExample example;
    [SerializeReference] public IExample[] exampleArray;
}
```

![img](../../images/img-serialize-reference.png)

Interfaces and abstract classes are displayed as shown above, and you can select and create subclasses from the dropdown.

For more information about SerializeReference serialization, please refer to [Unity's official documentation](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/SerializeReference.html).
