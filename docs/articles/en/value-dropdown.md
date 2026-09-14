# Value Dropdown Attribute

Use `[ValueDropdown]` to select a field or property value from choices supplied by your code. Choices can come from a field, a readable property, or a method. The picker supports search, grouped labels, and arrays or lists.

## Basic usage

Pass the provider member name with `nameof`. The provider must return an enumerable whose values are assignable to the field type. For arrays and `List<T>`, choices must match the element type.

```cs
using Alchemy.Inspector;
using UnityEngine;

public class ValueDropdownExample : MonoBehaviour
{
    [ValueDropdown(nameof(Difficulties))]
    public string difficulty = "Normal";

    static readonly string[] Difficulties = { "Easy", "Normal", "Hard" };
}
```

Providers can be public or private, including inherited members. A method must be non-generic and accept either no arguments or one `ValueDropdownContext` argument. A string itself is not a valid provider result; return a collection of strings instead.

Choices are evaluated when the picker opens, so reopening it refreshes dynamic providers. A null provider result produces an empty picker. Provider errors appear in the Inspector.

To use a static provider on another type, specify its type:

```cs
using Alchemy.Inspector;
using UnityEngine;

public static class DifficultyOptions
{
    public static string[] Values => new[] { "Easy", "Normal", "Hard" };
}

public class SharedDropdownExample : MonoBehaviour
{
    [ValueDropdown(typeof(DifficultyOptions), nameof(DifficultyOptions.Values))]
    public string difficulty = "Normal";
}
```

## Labels, groups, and disabled choices

Return `ValueDropdownItem<T>` entries to display labels separately from stored values. `ValueDropdownList<T>` provides convenient `Add` overloads for these entries.

```cs
using Alchemy.Inspector;
using UnityEngine;

public class LabeledDropdownExample : MonoBehaviour
{
    [ValueDropdown(nameof(Weapons), DropdownTitle = "Choose a weapon")]
    public int weaponId = 1;

    static ValueDropdownList<int> Weapons => new()
    {
        { "Melee/Sword", 1, "A balanced melee weapon." },
        { "Melee/Axe", 2, "A heavy melee weapon." },
        { "Ranged/Bow", 3, "Unlock this weapon first.", false },
    };
}
```

Slashes in labels create groups. Set `FlattenTreeView = true` to display full labels in a flat list. Disabled entries remain visible but cannot be selected. Null candidates are allowed for reference and nullable value types; destroyed Unity objects are disabled.

## Arrays and lists

By default, each element has a picker and the footer has an **Add from dropdown** button. The add picker lets you select multiple choices and apply them together.

```cs
using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;

public class ListDropdownExample : MonoBehaviour
{
    [ValueDropdown(nameof(Tags), IsUniqueList = true)]
    public List<string> tags = new();

    static readonly string[] Tags = { "Player", "Enemy", "Collectible" };
}
```

`IsUniqueList` disables choices already used in other elements and rejects selections that would introduce duplicates. It does not remove existing duplicates or validate edits made outside the picker. A `ValueDropdownList<T>.Comparer` controls matching and uniqueness; for example, use `StringComparer.OrdinalIgnoreCase` for case-insensitive string choices.

| ListMode | Behavior |
| - | - |
| `ElementsAndAdd` | Element pickers and an add picker. This is the default. |
| `ElementsOnly` | Element pickers with a normal `+` button instead of an add picker. |
| `AddOnly` | Normal element fields with an add picker. |

`ListViewSettings` can configure the list's appearance, selection, and reordering. Setting `ShowAddRemoveFooter = false` hides the add and remove controls.

## Context-aware providers

A provider can use the current owner, value, or element index to calculate choices:

```cs
using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;

public class ContextDropdownExample : MonoBehaviour
{
    [ValueDropdown(nameof(GetChoices))]
    public List<int> values = new();

    IEnumerable<int> GetChoices(ValueDropdownContext context)
    {
        var start = context.IsAdding ? values.Count : context.Index;
        return new[] { start, start + 1, start + 2 };
    }
}
```

| Context property | Meaning |
| - | - |
| `Root` | Serialized root object, or null for a standalone reflected value. |
| `Owner` | Object containing the decorated member, including a nested owner. |
| `CurrentValue` | Current scalar or element value; null when adding. |
| `Index` | Element index, or `-1` for a scalar or an add operation. |
| `IsAdding` | Whether the picker is adding collection elements. |

For multi-object editing, providers are evaluated for each target and the picker offers values available to every target.

## Options

| Option | Behavior |
| - | - |
| `Mode` | `Replace` replaces the normal field with a picker (default). `Append` keeps an editable field beside the picker. `AppendReadOnly` keeps the normal field visible but allows editing only through the picker. |
| `ListMode` | Controls element and add pickers, as described above. |
| `IsUniqueList` | Prevents duplicate picker selections in collections. Default: `false`. |
| `SearchThreshold` | Minimum choice count for showing search. Default: `10`. Set to `0` to always show search. |
| `DropdownTitle` | Custom picker title. |
| `FlattenTreeView` | Displays labels without navigating slash-separated groups. Default: `false`. |

For properties, combine `[ValueDropdown]` with `[ShowInInspector]`. A property without a setter, or a readonly field, cannot be changed through the picker. Showing a property does not make it serialized.

For reference-type choices that need independent instances, set `ValueDropdownList<T>.ValueFactory` to a function that copies the selected value. The factory runs once per selected value and destination when committing a selection; it does not run while displaying choices. Without a factory, the selected reference is reused. The destination must support storing the value, such as a compatible `[SerializeReference]` field for managed references.

Serialized selections support Undo and prefab overrides. If the target or collection changes while a picker is open, reopen the picker before selecting again.
