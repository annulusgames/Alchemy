# Foldout Group Attribute

Creates collapsible groups for multiple members.

![img](../../../images/img-attribute-foldout-group.png)

```cs 
[FoldoutGroup("Group1")]
public float foo;

[FoldoutGroup("Group1")]
public Vector3 bar;

[FoldoutGroup("Group1")]
public GameObject baz;

[FoldoutGroup("Group2")] 
public float alpha;

[FoldoutGroup("Group2")]
public Vector3 beta;

[FoldoutGroup("Group2")]
public GameObject gamma;
```

| Parameter | Description |
| - | - |
| GroupPath | Specifies the path of the group. Groups can be nested using `/`. |
