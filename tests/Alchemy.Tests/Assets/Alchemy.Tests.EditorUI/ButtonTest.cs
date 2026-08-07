using System.Text;
using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class ButtonTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        [Button]
        public void Foo()
        {
            Debug.Log("Foo");
        }

        [Button]
        public void Foo(int parameter)
        {
            Debug.Log("Foo: " + parameter);
        }

        [Button]
        public void Foo(DocumentationSampleClass parameter)
        {
            var builder = new StringBuilder();
            builder.AppendLine();
            builder.Append("foo = ").AppendLine(parameter.foo.ToString());
            builder.Append("bar = ").AppendLine(parameter.bar.ToString());
            builder.Append("baz = ").Append(
                parameter.baz == null ? "Null" : parameter.baz.ToString());
            Debug.Log("Foo: " + builder.ToString());
        }
        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
