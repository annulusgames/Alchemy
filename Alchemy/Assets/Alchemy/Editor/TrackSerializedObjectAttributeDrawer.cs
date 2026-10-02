using UnityEditor;
using UnityEditor.UIElements;

namespace Alchemy.Editor.Drawers
{
    public abstract class TrackSerializedObjectAttributeDrawer : AlchemyAttributeDrawer
    {
        public override void OnCreateElement()
        {
            var trackedProperty = GetTrackedProperty();
            if (trackedProperty != null)
            {
                TargetElement.TrackPropertyValue(trackedProperty, _ => OnInspectorChanged());
            }
            else if (SerializedObject != null)
            {
                TargetElement.TrackSerializedObjectValue(SerializedObject, _ => OnInspectorChanged());
            }

            OnInspectorChanged();
            TargetElement.schedule.Execute(() => OnInspectorChanged());
        }

        /// <summary>
        /// Serialized property whose changes re-evaluate this drawer. Null tracks the whole serialized object.
        /// </summary>
        protected virtual SerializedProperty GetTrackedProperty() => null;

        protected abstract void OnInspectorChanged();
    }
}
