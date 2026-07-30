# Creating Custom Attributes

You can create custom attributes for Alchemy by deriving a drawer from `AlchemyAttributeDrawer`. The following example implements `HelpBoxAttribute` and its drawer.

First, define the attribute to be added to fields or properties.

```cs
using System;
using UnityEngine.UIElements;

public sealed class HelpBoxAttribute : Attribute
{
    public HelpBoxAttribute(string message, HelpBoxMessageType messageType = HelpBoxMessageType.Info)
    {
        Message = message;
        MessageType = messageType;
    }

    public string Message { get; }
    public HelpBoxMessageType MessageType { get; }
}
```

Next, create a drawer for the attribute. Place drawer scripts in an `Editor` folder.

```cs
using UnityEngine.UIElements;
using Alchemy.Editor;

[CustomAttributeDrawer(typeof(HelpBoxAttribute))]
public sealed class HelpBoxDrawer : AlchemyAttributeDrawer
{
    HelpBox helpBox;

    public override void OnCreateElement()
    {
        var att = (HelpBoxAttribute)Attribute;
        helpBox = new HelpBox(att.Message, att.MessageType);

        var parent = TargetElement.parent;
        parent.Insert(parent.IndexOf(TargetElement), helpBox);
    }
}
```

Implement `OnCreateElement()` to modify the member's `VisualElement` after it is created. Unlike regular `PropertyDrawer` implementations, which replace the drawing process, an `AlchemyAttributeDrawer` applies post-processing to an existing element. This mechanism allows Alchemy to combine multiple drawers.

Add `[CustomAttributeDrawer]` to the drawer and pass it the custom attribute's type. Alchemy uses this metadata to find the drawers required to render each element.
