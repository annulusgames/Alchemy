using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Alchemy.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class HierarchyTreeMapDrawerTest
    {
        [SetUp]
        public void SetUp() => SiblingCache().Clear();

        [TearDown]
        public void TearDown() => SiblingCache().Clear();

        [Test]
        public void TreeMapTextures_KeepCachedNull()
        {
            var field = typeof(HierarchyTreeMapDrawer).GetField("TextureCached", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null);
            var cache = (Dictionary<string, Texture2D>)field.GetValue(null);
            var previous = new Dictionary<string, Texture2D>();
            var keys = new[]
            {
                nameof(HierarchyTreeMapDrawer.TreeMapCurrent),
                nameof(HierarchyTreeMapDrawer.TreeMapLast),
                nameof(HierarchyTreeMapDrawer.TreeMapLevel),
                nameof(HierarchyTreeMapDrawer.TreeMapLine),
            };

            foreach (var key in keys)
            {
                if (cache.TryGetValue(key, out var tex)) previous[key] = tex;
                cache[key] = null;
            }

            try
            {
                Assert.That(HierarchyTreeMapDrawer.TreeMapCurrent, Is.Null);
                Assert.That(HierarchyTreeMapDrawer.TreeMapLast, Is.Null);
                Assert.That(HierarchyTreeMapDrawer.TreeMapLevel, Is.Null);
                Assert.That(HierarchyTreeMapDrawer.TreeMapLine, Is.Null);
                foreach (var key in keys) Assert.That(cache[key], Is.Null);
            }
            finally
            {
                foreach (var key in keys)
                {
                    if (previous.ContainsKey(key)) cache[key] = previous[key];
                    else cache.Remove(key);
                }
            }
        }

        [Test]
        public void IsLastSibling_MatchesChildOrderAndReusesCacheUntilHierarchyChanges()
        {
            GameObject parent = null;
            GameObject first = null;
            GameObject second = null;
            try
            {
                parent = new GameObject("AlchemyTreeMapParent");
                first = new GameObject("AlchemyTreeMapFirst");
                second = new GameObject("AlchemyTreeMapSecond");
                first.transform.SetParent(parent.transform);
                Assert.That(HierarchyTreeMapDrawer.IsLastSibling(first.transform), Is.True);

                second.transform.SetParent(parent.transform);
                // hierarchyChanged is not raised here, so the cached flag is reused until the handler runs.
                Assert.That(HierarchyTreeMapDrawer.IsLastSibling(first.transform), Is.True);

                HierarchyTreeMapDrawer.ClearLastSiblingCache();
                Assert.That(HierarchyTreeMapDrawer.IsLastSibling(first.transform), Is.False);
                Assert.That(HierarchyTreeMapDrawer.IsLastSibling(second.transform), Is.True);

                SiblingCache()[SiblingKey(second.transform)] = false;
                Assert.That(HierarchyTreeMapDrawer.IsLastSibling(second.transform), Is.False);

                var extra = new GameObject("AlchemyTreeMapExtra");
                try
                {
                    Assert.That(HierarchyTreeMapDrawer.IsLastSibling(second.transform), Is.False);
                    HierarchyTreeMapDrawer.ClearLastSiblingCache();
                    Assert.That(HierarchyTreeMapDrawer.IsLastSibling(second.transform), Is.True);
                }
                finally
                {
                    Object.DestroyImmediate(extra);
                }
            }
            finally
            {
                if (first != null) Object.DestroyImmediate(first);
                if (second != null) Object.DestroyImmediate(second);
                if (parent != null) Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void IsLastSibling_MatchesRootOrder()
        {
            GameObject first = null;
            GameObject last = null;
            try
            {
                first = new GameObject("AlchemyTreeMapRootA");
                last = new GameObject("AlchemyTreeMapRootB");
                first.transform.SetAsFirstSibling();
                last.transform.SetAsLastSibling();

                Assert.That(first.transform.GetSiblingIndex(), Is.Not.EqualTo(first.scene.rootCount - 1));
                Assert.That(last.transform.GetSiblingIndex(), Is.EqualTo(last.scene.rootCount - 1));
                Assert.That(HierarchyTreeMapDrawer.IsLastSibling(first.transform), Is.False);
                Assert.That(HierarchyTreeMapDrawer.IsLastSibling(last.transform), Is.True);
            }
            finally
            {
                if (first != null) Object.DestroyImmediate(first);
                if (last != null) Object.DestroyImmediate(last);
            }
        }

        static IDictionary SiblingCache()
        {
            var field = typeof(HierarchyTreeMapDrawer).GetField("LastSiblingCached", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null);
            return (IDictionary)field.GetValue(null);
        }

        static object SiblingKey(Transform transform)
        {
#if UNITY_6000_4_OR_NEWER
            return transform.GetEntityId();
#else
            return transform.GetInstanceID();
#endif
        }
    }
}
