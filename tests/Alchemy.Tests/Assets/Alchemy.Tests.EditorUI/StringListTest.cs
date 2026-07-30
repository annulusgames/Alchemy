using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    public class StringListTest : MonoBehaviour
    {
        [SerializeField] string field;
        [SerializeField, DisableAlchemyEditor] string[] array;
        [SerializeField] List<string> list;
    }
}
