# Required Attribute

Displays a warning when no object reference is assigned to the field.

![img](../../../images/img-attribute-required.png)

```cs 
[Required]
public GameObject requiredField1;

[Required("Custom message")]
public Material requiredField2;
```

| Parameter | Description |
| - | - |
| Message | Text to display in the warning |
