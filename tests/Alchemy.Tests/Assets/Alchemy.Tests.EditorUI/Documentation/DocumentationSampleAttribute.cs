using System;

namespace Alchemy.Tests.EditorUI
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class DocumentationSampleAttribute : Attribute
    {
        /// <summary>
        /// When false, docs generation skips Unity Inspector screenshot capture for this sample.
        /// </summary>
        public bool Capture { get; set; } = true;
    }
}
