# Order Attribute

メンバーの表示順を変更します。Orderのデフォルト値は0で、メンバーは昇順に表示されます。グループのorderと同じ尺度を使うため、グループ外メンバーと兄弟グループはこの値で交互に並びます。

![img](../../../images/img-attribute-order.png)

```cs
[Order(2)]
public float foo;

[Order(1)]
public Vector3 bar;

[Order(0)]
public GameObject baz;
```

| パラメータ | 説明 |
| - | - |
| Order | メンバーの表示順。グループのorderと同じ尺度です。 |
