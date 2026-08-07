# Order Attribute

Changes the display order of the member. The default order is 0, and members are displayed in ascending order. Uses the same scale as group `order`, so ungrouped members and sibling groups interleave by this value.

![img](../../../images/img-attribute-order.png)

```cs
[Order(2)]
public float foo;

[Order(1)]
public Vector3 bar;

[Order(0)]
public GameObject baz;
```

| Parameter | Description |
| - | - |
| Order | The display order of the member. Shares the same scale as group `order`. |
