using Alchemy.Inspector;
using UnityEngine;

public class GroupOrderTest : MonoBehaviour
{
    [Group("Group3", 30)]
    [SerializeField]
    private float _valueFloatGroup3;
    
    [Group("Group3", 30)]
    [SerializeField]
    private string _valueStringGroup3;
    
    [Group("Group2", 20)]
    [SerializeField]
    private string _valueStringGroup2;
    
    [Group("Group1", 10)]
    [SerializeField]
    private float _valueFloatGroup1;
    
    [Group("Group1", 10)]
    [SerializeField]
    private string _valueStringGroup1;
    
    [Group("Group2", 20)]
    [SerializeField]
    private float _valueFloatGroup2;
    
    [Group("Group3" , 30)]
    [SerializeField]
    private bool _valueBoolGroup3;
    
    [Group("Group2", 20)]
    [SerializeField]
    private bool _valueBoolGroup2;
    
    [Group("Group1", 10)]
    [SerializeField]
    private bool _valueBoolGroup1;
}
