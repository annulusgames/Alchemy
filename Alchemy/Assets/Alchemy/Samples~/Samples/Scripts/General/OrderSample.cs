using UnityEngine;
using Alchemy.Inspector;

namespace Alchemy.Samples
{
    public class OrderSample : MonoBehaviour
    {
        [Order(3)] public RectOffset qux;
        [Order(2)] public float foo;
        [Order(1)] public Vector3 bar;
        [Order(0)] public GameObject baz;
    }
}