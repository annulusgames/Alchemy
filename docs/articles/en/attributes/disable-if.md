# Disable If Attribute

Disables the field when the specified condition evaluates to true.

![img](../../../images/img-attribute-disable-if-false.png)

![img](../../../images/img-attribute-disable-if-true.png)

```cs
public bool isDisabled;

public bool IsDisabled => isDisabled;
public bool IsDisabledMethod() => isDisabled;

[DisableIf("isDisabled")]
public int disableIfField;

[DisableIf("IsDisabled")]
public int disableIfProperty;

[DisableIf("IsDisabledMethod")]
public int disableIfMethod;
```

| Parameter | Description |
| - | - |
| Condition | The name of the field, property, or method used to evaluate the condition. |
