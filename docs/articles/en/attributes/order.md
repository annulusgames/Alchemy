# Order Attribute

Changes the display order of the field. The default order is 0, and members are displayed in ascending order.

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
| Order | The display order of the member. |
