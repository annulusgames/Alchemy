using System.Collections;
using System.Collections.Generic;
using Alchemy.Editor.Elements;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.Editor
{
    public class DictionaryFieldTest
    {
        [UnityTest]
        public IEnumerator Test_ByteKeyInputCanBeCreated()
        {
            var field = new DictionaryField(new Dictionary<byte, int>(), "Dictionary");
            var window = EditorTestUtility.ShowInWindow(field);
            try
            {
                yield return null;

                var addButton = EditorTestUtility.QueryRequired<Button>(
                    field,
                    button => button.text == "+ Add");
                EditorTestUtility.Click(addButton);

                EditorTestUtility.QueryRequired<HashMapFieldBase.HashMapItemBase>(field);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }
    }
}
