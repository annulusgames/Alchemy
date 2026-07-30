# Extending AlchemyEditor

If a MonoBehaviour or ScriptableObject has its own custom editor class, Alchemy attributes won't work by default.
To combine a custom editor with Alchemy, inherit from the `AlchemyEditor` class instead of the standard `Editor` class.

```cs
using UnityEditor;
using UnityEngine.UIElements;
using Alchemy.Editor;

[CustomEditor(typeof(Example))]
public class EditorExample : AlchemyEditor
{
    public override VisualElement CreateInspectorGUI()
    {
        // Always call the base CreateInspectorGUI
        var root = base.CreateInspectorGUI();

        // Add your custom logic here

        return root;
    }
}
```
