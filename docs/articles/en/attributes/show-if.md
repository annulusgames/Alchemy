# Show If Attribute

Displays the field in the Inspector when the specified condition evaluates to true.

![img](../../../images/img-attribute-show-if-false.png)

![img](../../../images/img-attribute-show-if-true.png)

```cs
public bool show;

public bool Show => show;
public bool IsShowTrue() => show;

[ShowIf("show")]
public int showIfField;

[ShowIf("Show")]
public int showIfProperty;

[ShowIf("IsShowTrue")]
public int showIfMethod;
```

| Parameter | Description |
| - | - |
| Condition | The name of the field, property, or method used to evaluate the condition. |
