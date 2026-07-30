# Disable Alchemy Editor Attribute

Disables `AlchemyEditor` for the target class and uses the default Inspector instead. When this attribute is added to a field, only that field is rendered using the default `PropertyField`.

![img](../../../images/img-attribute-disable-alchemy-editor.png)

```cs
[DisableAlchemyEditor]
public class DisableAlchemyEditorExample : MonoBehaviour
{
    public float foo;
    public Vector3 bar;
    public GameObject baz;
}
```
