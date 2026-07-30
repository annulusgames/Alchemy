using System;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    public interface IExample { }

    [Serializable]
    public sealed class ExampleA : IExample
    {
        public float alpha;
    }

    [Serializable]
    public sealed class ExampleB : IExample
    {
        public Vector3 beta;
    }

    [Serializable]
    public sealed class ExampleC : IExample
    {
        public GameObject gamma;
    }

    [DocumentationSample]
    public class SerializeReferenceTest : MonoBehaviour
    {
        [SerializeReference]
        public IExample example;

        [SerializeReference]
        public IExample[] exampleArray;
    }
}
