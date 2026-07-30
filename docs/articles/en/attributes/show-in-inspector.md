# Show In Inspector Attribute

Displays nonserialized fields and properties in the Inspector. Writable members can be edited, but their values are not serialized or persisted.

![img](../../../images/img-attribute-show-in-inspector.png)

```cs 
[NonSerialized, ShowInInspector]
public int field;

[NonSerialized, ShowInInspector]
public SampleClass classField = new();

[ShowInInspector]
public int Getter => 10;

[field: NonSerialized, ShowInInspector]
public string Property { get; set; } = string.Empty;
```
