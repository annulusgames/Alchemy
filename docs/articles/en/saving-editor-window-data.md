# Saving EditorWindow Data

Data for an editor window derived from `AlchemyEditorWindow` is automatically saved as JSON in the project's `ProjectSettings` folder.

You can customize the save/load behavior and destination path by overriding the `SaveWindowData()`, `LoadWindowData()`, and `GetWindowDataPath()` methods.

```cs
using UnityEditor;
using UnityEngine;
using Alchemy.Editor;

public class EditorWindowExample : AlchemyEditorWindow
{
    [MenuItem("Window/Example")]
    static void Open()
    {
        var window = GetWindow<EditorWindowExample>("Example");
        window.Show();
    }

    protected override string GetWindowDataPath()
    {
        // Return the path where the data will be saved
        return ...
    }

    protected override void LoadWindowData(string dataPath)
    {
        // Implement the loading process here
        ...
    }

    protected override void SaveWindowData(string dataPath)
    {
        // Implement the saving process here
        ...
    }
}
```
