using System;
using UnityEditor.UIElements;

namespace Alchemy.Editor.Drawers
{
    public abstract class TrackSerializedObjectAttributeDrawer<T> : AlchemyAttributeDrawer<T> where T : Attribute
    {
        public override void OnCreateElement()
        {
            TargetElement.TrackSerializedObjectValue(SerializedObject, x =>
            {
                OnInspectorChanged();
            });

            OnInspectorChanged();
            TargetElement.schedule.Execute(() => OnInspectorChanged());
        }

        protected abstract void OnInspectorChanged();
    }
}