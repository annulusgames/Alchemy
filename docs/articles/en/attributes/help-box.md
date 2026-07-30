# Help Box Attribute

Adds a note or warning above a field.

![img](../../../images/img-attribute-help-box.png)

```cs
[HelpBox("Custom Info")]
public float foo;

[HelpBox("Custom Warning", HelpBoxMessageType.Warning)]
public Vector2 bar;

[HelpBox("Custom Error", HelpBoxMessageType.Error)]
public GameObject baz;
```

| Parameter | Description |
| - | - |
| Message | The text to display inside the box. |
| MessageType | The type of message. |
