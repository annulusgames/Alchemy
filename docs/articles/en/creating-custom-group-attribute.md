# Creating Custom Group Attributes

You can create custom field-grouping attributes by deriving a drawer from `AlchemyGroupDrawer`. The following example implements `FoldoutGroupAttribute` and its drawer. Some implementation details are omitted for clarity.

First, define the attribute that identifies the groups. It must inherit from `PropertyGroupAttribute`.

```cs
using Alchemy.Inspector;

public sealed class FoldoutGroupAttribute : PropertyGroupAttribute
{
    public FoldoutGroupAttribute() : base() { }
    public FoldoutGroupAttribute(string groupPath) : base(groupPath) { }
}
```

Next, create a drawer for the attribute. Place drawer scripts in an `Editor` folder.

```cs
using UnityEngine.UIElements;
using Alchemy.Editor;

[CustomGroupDrawer(typeof(FoldoutGroupAttribute))]
public sealed class FoldoutGroupDrawer : AlchemyGroupDrawer
{
    public override VisualElement CreateRootElement(string label)
    {
        var foldout = new Foldout()
        {
            style = {
                width = Length.Percent(100f)
            },
            text = label
        };

        return foldout;
    }
}
```

Implement `CreateRootElement(string label)` to create the root `VisualElement` for each group. Add `[CustomGroupDrawer]` to the drawer and pass it the custom attribute's type. Alchemy uses this metadata to find the drawers required to render each group.
