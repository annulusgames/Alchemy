# List View Settings Attribute

Changes how collections are displayed. This attribute can improve row readability and prevent users from changing the collection size or element order in the Inspector.

![img](../../../images/img-attribute-list-view-settings.png)

```cs 
[ListViewSettings(ShowAlternatingRowBackgrounds = AlternatingRowBackground.All, ShowFoldoutHeader = false)]
public int[] array1;

[ListViewSettings(Reorderable = false, ShowAddRemoveFooter = false, ShowBorder = false, ShowBoundCollectionSize = false)]
public Vector3[] array2 = new Vector3[]
{
    Vector3.zero,
    Vector3.one
};
```

| Parameter | Description |
| - | - |
| ShowAddRemoveFooter | Whether to display the footer used to add or remove elements |
| ShowAlternatingRowBackgrounds | Whether to change the background color for every other row |
| ShowBorder | Whether to display a border |
| ShowBoundCollectionSize | Whether to display the field used to change the collection size |
| ShowFoldoutHeader | Whether to display the foldout header |
| SelectionType | How elements can be selected |
| Reorderable | Whether elements can be reordered |
| ReorderMode | How reordering is displayed |
