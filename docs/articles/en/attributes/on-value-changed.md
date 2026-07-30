# On Value Changed Attribute

Executes a method with the specified name when the value of the field changes.

```cs 
[OnValueChanged("OnValueChanged")]
public int foo;

void OnValueChanged(int value)
{
    Debug.Log(value);
}
```

| Parameter | Description |
| - | - |
| MethodName | The name of the method to be called when the value changes. |
