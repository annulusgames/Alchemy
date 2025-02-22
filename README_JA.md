# Alchemy

<img src="https://github.com/yn01-dev/Alchemy/blob/main/docs/images/header.png" width="800">

[![license](https://img.shields.io/badge/LICENSE-MIT-green.svg)](LICENSE)

[English README is here](README.md)

## 概要

Alchemyは属性を使用したInspector拡張を提供するライブラリです。

属性ベースの簡単かつ強力なエディタ拡張を追加するほか、独自のシリアル化プロセスを介してあらゆる型(Dictionary, Hashset, Nullable, Tuple, etc...)をシリアル化し、Inspector上で編集することが可能になります。Source Generatorを用いて必要なコードを動的に生成するため、partialにした対象型に属性を付加するだけで機能します。Odinのように専用のクラスを継承する必要はありません。

<img src="https://github.com/yn01-dev/Alchemy/blob/main/docs/images/img-v2.0.png" width="800">

また、v2.0の新機能としてEditorWindow拡張とHierarchy拡張が追加されました。これらを用いることで、エディタでの開発フローを効率化するツールを簡単に作成できるようになります。

## 特徴

* Inspectorを拡張する30以上の属性を追加
* SerializeReferenceをサポートし、ドロップダウンから型を選択可能に
* あらゆる型(Dictionary, Hashset, Nullable, Tuple, etc...)をシリアル化/Inspectorで編集可能
* 属性を用いたEditorWindowの作成
* Hierarchyの使い勝手を向上させる機能の提供
* Alchemy上で動作するカスタム属性の作成

## セットアップ

### 要件

* Unity 2021.2 以上 (シリアル化拡張を使用する場合、2022.1以上を推奨)
* Serialization 2.0 以上 (シリアル化拡張を使用する場合)

### インストール

1. Window > Package ManagerからPackage Managerを開く
2. 「+」ボタン > Add package from git URL
3. 以下のURLを入力する

```
https://github.com/yn01-dev/Alchemy.git?path=/Alchemy/Assets/Alchemy
```

あるいはPackages/manifest.jsonを開き、dependenciesブロックに以下を追記

```json
{
    "dependencies": {
        "com.annulusgames.alchemy": "https://github.com/yn01-dev/Alchemy.git?path=/Alchemy/Assets/Alchemy"
    }
}
```

## ドキュメント

ドキュメントのフルバージョンは[こちら](https://docs.yn01.dev/Alchemy/)から確認できます。

## 基本的な使い方

Inspectorでの表示をカスタマイズしたい場合には、クラスが持つフィールドに属性を付加します。

```cs
using UnityEngine;
using UnityEngine.UIElements;
using Alchemy.Inspector;  // Alchemy.Inspector名前空間をusingに追加

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

各フィールドをグループ化する属性も用意されています。各グループはスラッシュ`/`で区切ることでネストできます。

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

メソッドに`[Button]`属性を付加することで、メソッドをInspectorから実行することが可能になります。

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

Alchemyでは他にも数多くの属性が提供されています。利用可能な属性の一覧は[ドキュメント](https://docs.yn01.dev/Alchemy/articles/ja/inspector-extension-with-attributes.html)から確認できます。

## インターフェース/抽象クラスを編集する

AlchemyはUnityのSerializeReferenceに対応しています。`[SerializeReference]`属性を付加することでインターフェースや抽象クラスをInspector上で編集できるようになります。

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

インターフェース・抽象クラスは上のように表示され、ドロップダウンから子クラスを選択して生成することができます。

詳細は[SerializeReference](https://docs.yn01.dev/Alchemy/articles/ja/serialize-reference.html)のページを参照してください。

## Hierarchy

Alchemyを導入することで、Hierarchyを拡張するいくつかの機能が追加されます。

<img src="https://github.com/yn01-dev/Alchemy/blob/main/docs/images/img-hierarchy.png" width="600">

### トグルとアイコン

<img src="https://github.com/yn01-dev/Alchemy/blob/main/docs/images/gif-hierarchy-toggle.gif" width="600">

Hierarchyにオブジェクトのアクティブ/非アクティブを切り替えるトグルと、オブジェクトの持つコンポーネントのアイコンの表示を追加できます。これらはProjectSettingsから設定が可能です。

<img src="https://github.com/yn01-dev/Alchemy/blob/main/docs/images/img-project-settings.png" width="600">

### 装飾

また、CreateメニューからHierarchyを装飾するためのオブジェクトを作成可能になります。

<img src="https://github.com/yn01-dev/Alchemy/blob/main/docs/images/img-create-hierarchy-object.png" width="600">

これらのオブジェクトはビルド時に自動的に除外されます。(子オブジェクトを持つ場合は全てデタッチしてから削除されます。)
詳細は[Hierarchyの装飾](https://docs.yn01.dev/Alchemy/articles/ja/decorating-hierarchy.html)を参照してください。

## AlchemyEditorWindow

通常の`Editor`クラスではなく`AlchemyEditorWindow`クラスを継承することで、Alchemyの属性を用いてエディタウィンドウを作成することができるようになります。

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

`AlchemyEditorWindow`を継承して作成したウィンドウのデータは、プロジェクトのProjectSettingsフォルダにjson形式で保存されます。詳細は[EditorWindowのデータを保存する](https://docs.yn01.dev/Alchemy/articles/ja/saving-editor-window-data.html)のページを参照してください。

## シリアル化拡張を使用する

Dictionaryなどの通常のUnityがシリアル化できない型を編集したい場合、`[AlchemySerialize]`属性を使用してシリアル化を行うことができます。

シリアル化拡張を使用したい場合、[Unity.Serialization](https://docs.unity3d.com/Packages/com.unity.serialization@3.1/manual/index.html)パッケージが必要になります。また、リフレクションを用いたUnity.Serializationのシリアル化はUnity2022.1以前のAOT環境で動作しない可能性があります。詳細はパッケージのマニュアルを確認してください。

以下は、Alchemyのシリアル化拡張を用いて様々な型をシリアル化/Inspectorで編集可能にするサンプルです。

```cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Alchemy.Serialization; // Alchemy.Serialization名前空間をusingに追加

// [AlchemySerialize]属性を付加することでAlchemyのシリアル化拡張が有効化されます。
// 任意の基底クラスを持つ型に使用できますが、SourceGeneratorがコード生成を行うため対象型はpartialである必要があります。
[AlchemySerialize]
public partial class AlchemySerializationExample : MonoBehaviour
{
    // 対象のフィールドに[AlchemySerializeField]属性と[NonSerialized]属性を付加します。
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

シリアル化プロセスの技術的な詳細についてはドキュメントの[Alchemyのシリアル化プロセス](https://docs.yn01.dev/Alchemy/articles/ja/alchemy-serialization-process.html)を参照してください。

## ヘルプ

Unity forum: https://forum.unity.com/threads/released-alchemy-inspector-serialization-extensions.1523665/

## ライセンス

[MIT License](LICENSE)
