using UnityEditor.UIElements;

namespace Alchemy.Editor.Drawers
{
    public abstract class TrackSerializedObjectAttributeDrawer : AlchemyAttributeDrawer
    {
        public override void OnCreateElement()
        {
            if (SerializedObject != null)
            {
                TargetElement.TrackSerializedObjectValue(SerializedObject, x =>
                {
                    OnInspectorChanged();
                });
            }

            OnInspectorChanged();
            TargetElement.schedule.Execute(() => OnInspectorChanged());
        }

        protected abstract void OnInspectorChanged();
    }
}
