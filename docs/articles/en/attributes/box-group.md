# Box Group Attribute

Creates a group that wraps multiple members in a box for display.

![img](../../../images/img-attribute-box-group.png)

```cs 
[BoxGroup("Group1")]
public float foo;

[BoxGroup("Group1")]
public Vector3 bar;

[BoxGroup("Group1")]
public GameObject baz;

[BoxGroup("Group1/Group2")]
public float alpha;

[BoxGroup("Group1/Group2")]
public Vector3 beta;

[BoxGroup("Group1/Group2")]
public GameObject gamma;
```

| Parameter | Description |
| - | - |
| GroupPath | Specifies the path of the group. Groups can be nested by separating them with `/`. |
