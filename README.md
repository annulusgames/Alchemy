# Alchemy

<img src="https://github.com/yn01-dev/Alchemy/blob/main/docs/images/header.png" width="800">

[![license](https://img.shields.io/badge/LICENSE-MIT-green.svg)](LICENSE)

[日本語版READMEはこちら](README_JA.md)

## Overview

Alchemy is a library that provides inspector extensions using attributes.

In addition to adding easy and powerful editor extensions based on attributes, it allows serialization of any type (Dictionary, Hashset, Nullable, Tuple, etc...) through its own serialization process, making it possible to edit them in the inspector. By using Source Generator to dynamically generate the necessary code, it works simply by adding attributes to the target type marked as partial. There is no need to inherit from dedicated classes as with Odin.

<img src="https://github.com/yn01-dev/Alchemy/blob/main/docs/images/img-v2.0.png" width="800">

Additionally, with the new features of v2.0, EditorWindow extensions and Hierarchy extensions have been added. These make it easy to create tools that streamline the development workflow in the editor.

## Features

* Adds over 30 attributes to extend the Inspector
* Supports SerializeReference, allowing selection of types from a dropdown
* Serialize any type (Dictionary, Hashset, Nullable, Tuple, etc...) / Editable in Inspector
* Creation of EditorWindow using attributes
* Provides features to improve usability of the Hierarchy
* Creation of custom attributes that work with Alchemy

## Setup

### Requirements

* Unity 2021.2 or higher (Recommended: 2022.1 or higher for serialization extensions)
* Serialization 2.0 or higher (for serialization extensions)

### Installation

1. Open Package Manager from Window > Package Manager
2. Click on the "+" button > Add package from git URL
3. Enter the following URL:

```
https://github.com/yn01-dev/Alchemy.git?path=/Alchemy/Assets/Alchemy
```

Or open Packages/manifest.json and add the following to the dependencies block:

```json
{
    "dependencies": {
        "com.annulusgames.alchemy": "https://github.com/yn01-dev/Alchemy.git?path=/Alchemy/Assets/Alchemy"
    }
}
```

## Documentation

The full version of the documentation can be found [here](https://docs.yn01.dev/Alchemy/).

## Basic Usage

To customize the display in the Inspector, add attributes to the fields of the class.

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

<img src="https://github.com/yn01-dev/Alchemy/blob/main/docs/images/img-attributes-example.png" width="600">

Various attributes for grouping each field are also provided. Each group can be nested by separating with a slash `/`.

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

<img src="https://github.com/yn01-dev/Alchemy/blob/main/docs/images/img-group-1.png" width="600">

By adding the `[Button]` attribute to a method, the method can be executed from the Inspector.

```cs
using System.Text;
using UnityEngine;
using Alchemy.Inspector;

[Serializable]
public sealed class Example : IExample
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

<img src="https://github.com/yn01-dev/Alchemy/blob/main/docs/images/img-button.png" width="600">

Alchemy provides many other attributes. The list of available attributes can be found in the [documentation](https://docs.yn01.dev/Alchemy/articles/en/inspector-extension-with-attributes.html).

## Editing Interfaces/Abstract Classes

Alchemy supports Unity's SerializeReference. By adding the `[SerializeReference]` attribute, interfaces and abstract classes can be edited in the Inspector.

```cs
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

<img src="https://github.com/yn01-dev/Alchemy/blob/main/docs/images/img-serialize-reference.png" width="600">

Interfaces/abstract classes are displayed as shown above, and you can select child classes from the dropdown to instantiate them.

For more details, refer to [SerializeReference](https://docs.yn01.dev/Alchemy/articles/en/serialize-reference.html).

## Hierarchy

By introducing Alchemy, several features are added to extend the Hierarchy.

<img src="https://github.com/yn01-dev/Alchemy/blob/main/docs/images/img-hierarchy.png" width="600">

### Toggles and Icons

<img src="https://github.com/yn01-dev/Alchemy/blob/main/docs/images/gif-hierarchy-toggle.gif" width="600">

Toggles to switch between active/inactive states of objects and icons to display the components of objects can be added to the Hierarchy. These can be configured from ProjectSettings.

<img src="https://github.com/yn01-dev/Alchemy/blob/main/docs/images/img-project-settings.png" width="600">

### Decoration

Additionally, objects to decorate the Hierarchy can be created from the Create menu.

<img src="https://github.com/yn01-dev/Alchemy/blob/main/docs/images/img-create-hierarchy-object.png" width="600">

These objects are automatically excluded in build. (If they have child objects, all child objects are detached before deletion.)
For more details, refer to [Decorating Hierarchy](https://docs.yn01.dev/Alchemy/articles/en/decorating-hierarchy.html).

## AlchemyEditorWindow

By inheriting from the `AlchemyEditorWindow` class instead of the usual `Editor` class, you can create editor windows using Alchemy attributes.

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

<img src="https://github.com/yn01-dev/Alchemy/blob/main/docs/images/img-editor-window.png" width="600">

The data of windows created by inheriting from `AlchemyEditorWindow` is saved in json format in the ProjectSettings folder of the project. For more details, refer to [Saving Editor Window Data](https://docs.yn01.dev/Alchemy/articles/en/saving-editor-window-data.html).

## Using Serialization Extensions

If you want to edit types that Unity cannot serialize, such as Dictionary, you can use the `[AlchemySerialize]` attribute to perform serialization.

If you want to use serialization extensions, you will need the [Unity.Serialization](https://docs.unity3d.com/Packages/com.unity.serialization@3.1/manual/index.html) package. Additionally, reflection-based serialization using Unity.Serialization may not work in AOT environments prior to Unity 2022.1. Check the package manual for details.

Below is a sample using Alchemy's serialization extension to make various types serializable/editable in the Inspector.

```cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Alchemy.Serialization;

// By adding the [AlchemySerialize] attribute, Alchemy's serialization extension is enabled.
// It can be used with any type that has an optional base class, but the target type must be partial for the Source Generator to generate code.
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

<img src="https://github.com/yn01-dev/Alchemy/blob/main/docs/images/img-serialization-sample.png" width="600">

For technical details of the serialization process, refer to [Alchemy Serialization Process](https://docs.yn01.dev/Alchemy/articles/en/alchemy-serialization-process.html) in the documentation.

## Help

Unity forum: https://forum.unity.com/threads/released-alchemy-inspector-serialization-extensions.1523665/

## License

[MIT License](LICENSE)
