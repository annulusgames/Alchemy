using System;
using UnityEngine.UIElements;

namespace Alchemy.Editor
{
    /// <summary>
    /// Base class for implementing Alchemy group attribute drawing process.
    /// </summary>
    public abstract class AlchemyGroupDrawer
    {
        /// <summary>
        /// Create a visual element that will be the root of the group.
        /// </summary>
        /// <param name="label">Label text</param>
        public abstract VisualElement CreateRootElement(string label);

        /// <summary>
        /// Returns the corresponding visual element when the root visual element differs depending on the attribute value.
        /// </summary>
        /// <param name="attribute">Target attribute</param>
        public virtual VisualElement GetGroupElement(Attribute attribute) => null;

        /// <summary>
        /// ID used to identify the group.
        /// </summary>
        public string UniqueId => uniqueId;

        /// <summary>
        /// Drawing order of the group.
        /// </summary>
        public int Order => order;

        string uniqueId;
        int order;

        internal void SetUniqueId(string id)
        {
            this.uniqueId = id;
        }

        internal void SetOrder(int order)
        {
            this.order = order;
        }
    }
}
