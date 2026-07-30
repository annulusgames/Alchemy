# Alchemy

<img src="https://github.com/annulusgames/Alchemy/blob/main/docs/images/header.png" width="800">

[![license](https://img.shields.io/badge/LICENSE-MIT-green.svg)](LICENSE)

[日本語版READMEはこちら](README_JA.md)

## Overview

Alchemy is a library that provides attribute-based Inspector extensions.

In addition to providing easy and powerful attribute-based editor extensions, Alchemy can serialize types that Unity does not normally support, such as dictionaries, hash sets, nullable value types, and tuples, so they can be edited in the Inspector. Add the appropriate attributes to a target type and mark it as `partial`; a source generator creates the necessary code. Unlike Odin, there is no need to inherit from dedicated base classes.

<img src="https://github.com/annulusgames/Alchemy/blob/main/docs/images/img-v2.0.png" width="800">

v2.0 also adds EditorWindow and Hierarchy extensions. These make it easy to build tools that streamline your editor workflow.

## Features

* Add over 30 attributes to extend the Inspector
* Support SerializeReference, allowing selection of types from a dropdown
* Serialize additional types, including dictionaries, hash sets, nullable value types, and tuples, and edit them in the Inspector
* Create EditorWindows using attributes
* Improve Hierarchy usability
* Create custom attributes that work with Alchemy

## Setup

### Requirements

* Unity 2021.2 or later (Unity 2022.1 or later is recommended for serialization extensions)
* Unity.Serialization 2.0 or later (for serialization extensions)

### Installation

1. Open the Package Manager from Window > Package Manager
2. Click the "+" button > Add package from git URL
3. Enter the following URL:

```
https://github.com/annulusgames/Alchemy.git?path=/Alchemy/Assets/Alchemy
```

Alternatively, open `Packages/manifest.json` and add the following entry to the `dependencies` block:

```json
{
    "dependencies": {
        "com.annulusgames.alchemy": "https://github.com/annulusgames/Alchemy.git?path=/Alchemy/Assets/Alchemy"
    }
}
```

## Documentation

The full documentation can be found [here](https://annulusgames.github.io/Alchemy/).

## Basic Usage

To customize the display in the Inspector, add attributes to the class fields.

```cs
using UnityEngine;
using UnityEngine.UIElements;
using Alchemy.Inspector;

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

<img src="https://github.com/annulusgames/Alchemy/blob/main/docs/images/img-attributes-example.png" width="600">

Attributes for grouping fields are also available. Groups can be nested by separating group names with a slash `/`.

```cs
using UnityEngine;
using Alchemy.Inspector;

public class GroupAttributesExample : MonoBehaviour
{
    [FoldoutGroup("Foldout")]
    public int a;

    [FoldoutGroup("Foldout")]
    public int b;

    [FoldoutGroup("Foldout")]
    public int c;

    [TabGroup("Tab", "Tab1")]
    public int x;

    [TabGroup("Tab", "Tab2")]
    public string y;

    [TabGroup("Tab", "Tab3")]
    public Vector3 z;
}
```

<img src="https://github.com/annulusgames/Alchemy/blob/main/docs/images/img-group-1.png" width="600">

By adding the `[Button]` attribute to a method, you can execute the method from the Inspector.

```cs
using System;
using System.Text;
using UnityEngine;
using Alchemy.Inspector;

[Serializable]
public sealed class Example
{
    public float foo;
    public Vector3 bar;
    public GameObject baz;
}

public class ButtonExample : MonoBehaviour
{
    [Button]
    public void Foo()
    {
        Debug.Log("Foo");
    }

    [Button]
    public void Foo(int parameter)
    {
        Debug.Log("Foo: " + parameter);
    }

    [Button]
    public void Foo(Example parameter)
    {
        var builder = new StringBuilder();
        builder.AppendLine();
        builder.Append("foo = ").AppendLine(parameter.foo.ToString());
        builder.Append("bar = ").AppendLine(parameter.bar.ToString());
        builder.Append("baz = ").Append(parameter.baz == null ? "Null" : parameter.baz.ToString());
        Debug.Log("Foo: " + builder.ToString());
    }
}
```

<img src="https://github.com/annulusgames/Alchemy/blob/main/docs/images/img-button.png" width="600">

Alchemy provides many other attributes. The list of available attributes can be found in the [documentation](https://annulusgames.github.io/Alchemy/articles/en/inspector-extension-with-attributes.html).

## Editing Interfaces and Abstract Classes

Alchemy supports Unity's SerializeReference. By adding the `[SerializeReference]` attribute, interfaces and abstract classes can be edited in the Inspector.

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
    [SerializeReference] public IExample Example;
    [SerializeReference] public IExample[] ExampleArray;
}
```

<img src="https://github.com/annulusgames/Alchemy/blob/main/docs/images/img-serialize-reference.png" width="600">

Interfaces and abstract classes are displayed as shown above, and you can select concrete types from the dropdown to instantiate them.

For more details, refer to [SerializeReference](https://annulusgames.github.io/Alchemy/articles/en/serialize-reference.html).

## Hierarchy

Alchemy provides several features that extend the Hierarchy.

<img src="https://github.com/annulusgames/Alchemy/blob/main/docs/images/img-hierarchy.png" width="600">

### Toggles and Icons

<img src="https://github.com/annulusgames/Alchemy/blob/main/docs/images/gif-hierarchy-toggle.gif" width="600">

You can display active-state toggles and component icons for each object in the Hierarchy. These features can be configured in Project Settings.

<img src="https://github.com/annulusgames/Alchemy/blob/main/docs/images/img-project-settings.png" width="600">

### Decoration

From the Create menu, you can create objects that decorate the Hierarchy.

<img src="https://github.com/annulusgames/Alchemy/blob/main/docs/images/img-create-hierarchy-object.png" width="600">

These objects are automatically excluded from builds. If they have children, the children are unparented before the decorative objects are deleted.
For more details, refer to [Decorating the Hierarchy](https://annulusgames.github.io/Alchemy/articles/en/decorating-hierarchy.html).

## AlchemyEditorWindow

By inheriting from the `AlchemyEditorWindow` class instead of `EditorWindow`, you can create editor windows using Alchemy attributes.

```cs
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Alchemy.Editor;
using Alchemy.Inspector;

public class EditorWindowExample : AlchemyEditorWindow
{
    [MenuItem("Window/Example")]
    static void Open()
    {
        var window = GetWindow<EditorWindowExample>("Example");
        window.Show();
    }
    
    [Serializable]
    [HorizontalGroup]
    public class DatabaseItem
    {
        [LabelWidth(30f)]
        public float foo;

        [LabelWidth(30f)]
        public Vector3 bar;
        
        [LabelWidth(30f)]
        public GameObject baz;
    }

    [ListViewSettings(ShowAlternatingRowBackgrounds = AlternatingRowBackground.All, ShowFoldoutHeader = false)]
    public List<DatabaseItem> items;

    [Button, HorizontalGroup]
    public void Button1() { }

    [Button, HorizontalGroup]
    public void Button2() { }

    [Button, HorizontalGroup]
    public void Button3() { }
}
```

<img src="https://github.com/annulusgames/Alchemy/blob/main/docs/images/img-editor-window.png" width="600">

Data for windows that inherit from `AlchemyEditorWindow` is saved as JSON in the project's `ProjectSettings` folder. For more details, refer to [Saving Editor Window Data](https://annulusgames.github.io/Alchemy/articles/en/saving-editor-window-data.html).

## Using Serialization Extensions

To edit types that Unity does not normally serialize, such as dictionaries, use the `[AlchemySerialize]` attribute.

Serialization extensions require the [Unity.Serialization](https://docs.unity3d.com/Packages/com.unity.serialization@3.1/manual/index.html) package. Additionally, reflection-based serialization using Unity.Serialization may not work in AOT environments prior to Unity 2022.1. Check the package manual for details.

The following example uses Alchemy's serialization extension to make various types serializable and editable in the Inspector.

```cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Alchemy.Serialization;

// By adding the [AlchemySerialize] attribute, Alchemy's serialization extension is enabled.
// It can be used regardless of the target type's base class, but the target type must be partial for the source generator to generate code.
[AlchemySerialize]
public partial class AlchemySerializationExample : MonoBehaviour
{
    // Add [AlchemySerializeField] and [NonSerialized] attributes to the target fields.
    [AlchemySerializeField, NonSerialized]
    public HashSet<GameObject> hashset = new();

    [AlchemySerializeField, NonSerialized]
    public Dictionary<string, GameObject> dictionary = new();

    [AlchemySerializeField, NonSerialized]
    public (int, int) tuple;

    [AlchemySerializeField, NonSerialized]
    public Vector3? nullable = null;
}
```

<img src="https://github.com/annulusgames/Alchemy/blob/main/docs/images/img-serialization-sample.png" width="600">

For technical details on the serialization process, refer to [Alchemy Serialization Process](https://annulusgames.github.io/Alchemy/articles/en/alchemy-serialization-process.html) in the documentation.

## Help

Unity forum: https://forum.unity.com/threads/released-alchemy-inspector-serialization-extensions.1523665/

## License

[MIT License](LICENSE)
