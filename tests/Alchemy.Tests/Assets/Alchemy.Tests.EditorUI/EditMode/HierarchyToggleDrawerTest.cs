using Alchemy.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class HierarchyToggleDrawerTest
    {
        [Test]
        public void IsEnabled_MatchesPublicBoolEnabledAndTracksChanges()
        {
            var gameObject = new GameObject("HierarchyIconEnabled");
            try
            {
                var light = gameObject.AddComponent<Light>();
                var renderer = gameObject.AddComponent<MeshRenderer>();
                var lodGroup = gameObject.AddComponent<LODGroup>();

                AssertMatches(light);
                AssertMatches(renderer);
                AssertMatches(lodGroup);
                AssertMatches(gameObject.transform);

                light.enabled = false;
                renderer.enabled = false;
                lodGroup.enabled = false;

                AssertMatches(light);
                AssertMatches(renderer);
                AssertMatches(lodGroup);
                Assert.That(HierarchyToggleDrawer.IsEnabled(lodGroup), Is.False);

                lodGroup.enabled = true;
                AssertMatches(lodGroup);
                Assert.That(HierarchyToggleDrawer.IsEnabled(lodGroup), Is.True);
                AssertMatches(gameObject.transform);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        static void AssertMatches(Component component)
        {
            Assert.That(HierarchyToggleDrawer.IsEnabled(component), Is.EqualTo(PublicBoolEnabled(component)));
        }

        static bool PublicBoolEnabled(Component component)
        {
            var property = component.GetType().GetProperty("enabled", typeof(bool));
            return (bool)(property?.GetValue(component, null) ?? true);
        }
    }
}
