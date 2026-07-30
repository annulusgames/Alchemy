# Inspector Extensions with Attributes

Alchemy allows you to extend the Inspector using attributes. To customize the display in the Inspector, you can add attributes to the fields of your class.

```cs
using UnityEngine;
using UnityEngine.UIElements;
using Alchemy.Inspector;  // Import the Alchemy.Inspector namespace

public class AttributesExample : MonoBehaviour
{
    [LabelText("Custom Label")]
    public float foo;

    [HideLabel]
    public Vector3 bar;
    
    [AssetsOnly]
    public GameObject baz;

    [Title("Title")]
    [HelpBox("HelpBox", HelpBoxMessageType.Info)]
    [ReadOnly]
    public string message = "Read Only";
}
```

![img](../../images/img-attributes-example.png)
