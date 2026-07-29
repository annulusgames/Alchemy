using UnityEditor.UIElements;

namespace Alchemy.Editor.Drawers
{
    public abstract class TrackSerializedObjectAttributeDrawer : AlchemyAttributeDrawer
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
