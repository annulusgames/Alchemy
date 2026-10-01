using System;
using System.Collections;
using System.Reflection;
using Alchemy.Editor;
using Alchemy.Hierarchy;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class HierarchyHeaderSeparatorCacheTest
    {
        [SetUp]
        public void SetUp()
        {
            HierarchyHeaderDrawer.ClearHeaderCache();
            HierarchySeparatorDrawer.ClearSeparatorCache();
        }

        [TearDown]
        public void TearDown()
        {
            HierarchyHeaderDrawer.ClearHeaderCache();
            HierarchySeparatorDrawer.ClearSeparatorCache();
        }

        [Test]
        public void OnGUI_OrdinaryRow_CachesAbsenceAndReusesIt()
        {
            GameObject gameObject = null;
            try
            {
                gameObject = new GameObject("AlchemyOrdinaryRow");
                var id = RowId(gameObject);
                var rect = new Rect(0f, 0f, 160f, 16f);

                new HierarchyHeaderDrawer().OnGUI(id, rect);
                new HierarchySeparatorDrawer().OnGUI(id, rect);
                Assert.That((bool)HeaderCache()[id], Is.False);
                Assert.That((bool)SeparatorCache()[id], Is.False);

                gameObject.AddComponent<HierarchyHeader>();
                HeaderCache()[id] = false;
                SeparatorCache()[id] = false;

                new HierarchyHeaderDrawer().OnGUI(id, rect);
                new HierarchySeparatorDrawer().OnGUI(id, rect);
                Assert.That((bool)HeaderCache()[id], Is.False);
                Assert.That((bool)SeparatorCache()[id], Is.False);
            }
            finally
            {
                if (gameObject != null) UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AddedHeader_IsPickedUpAfterCacheInvalidation()
        {
            GameObject gameObject = null;
            try
            {
                gameObject = new GameObject("AlchemyAddedHeader");
                var id = RowId(gameObject);
                Assert.That(HierarchyHeaderDrawer.TryGetHeader(id, out _), Is.False);
                Assert.That((bool)HeaderCache()[id], Is.False);

                gameObject.AddComponent<HierarchyHeader>();
                // Component add/remove does not raise hierarchyChanged. A structural batch
                // (ChangeGameObjectStructure) may already have dropped the cache; property batches must not.
                // Re-seed absence, then run the same clear those structural events use.
                HeaderCache()[id] = false;
                HierarchyHeaderDrawer.ClearHeaderCache();

                Assert.That(HierarchyHeaderDrawer.TryGetHeader(id, out var row), Is.True);
                Assert.That(row, Is.EqualTo(gameObject));
                Assert.That((bool)HeaderCache()[id], Is.True);
            }
            finally
            {
                if (gameObject != null) UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CachedAbsence_SkipsComponentLookupUntilCleared()
        {
            GameObject header = null;
            GameObject separator = null;
            try
            {
                header = new GameObject("AlchemyCachedHeader");
                separator = new GameObject("AlchemyCachedSeparator");
                header.AddComponent<HierarchyHeader>();
                separator.AddComponent<HierarchySeparator>();
                var headerId = RowId(header);
                var separatorId = RowId(separator);

                HeaderCache()[headerId] = false;
                SeparatorCache()[separatorId] = false;
                Assert.That(HierarchyHeaderDrawer.TryGetHeader(headerId, out _), Is.False);
                Assert.That(HierarchySeparatorDrawer.TryGetSeparator(separatorId, out _), Is.False);

                HierarchyHeaderDrawer.ClearHeaderCache();
                HierarchySeparatorDrawer.ClearSeparatorCache();
                Assert.That(HierarchyHeaderDrawer.TryGetHeader(headerId, out var headerRow), Is.True);
                Assert.That(headerRow, Is.EqualTo(header));
                Assert.That(HierarchySeparatorDrawer.TryGetSeparator(separatorId, out var separatorRow), Is.True);
                Assert.That(separatorRow, Is.EqualTo(separator));
            }
            finally
            {
                if (header != null) UnityEngine.Object.DestroyImmediate(header);
                if (separator != null) UnityEngine.Object.DestroyImmediate(separator);
            }
        }

        [Test]
        public void HeaderAndSeparator_RememberPresenceIndependently()
        {
            GameObject header = null;
            GameObject separator = null;
            try
            {
                header = new GameObject("AlchemyHeaderPresence");
                separator = new GameObject("AlchemySeparatorPresence");
                header.AddComponent<HierarchyHeader>();
                separator.AddComponent<HierarchySeparator>();
                var headerId = RowId(header);
                var separatorId = RowId(separator);

                Assert.That(HierarchyHeaderDrawer.TryGetHeader(headerId, out var headerRow), Is.True);
                Assert.That(headerRow, Is.EqualTo(header));
                Assert.That(HierarchySeparatorDrawer.TryGetSeparator(headerId, out _), Is.False);
                Assert.That(HierarchySeparatorDrawer.TryGetSeparator(separatorId, out var separatorRow), Is.True);
                Assert.That(separatorRow, Is.EqualTo(separator));
                Assert.That(HierarchyHeaderDrawer.TryGetHeader(separatorId, out _), Is.False);

                Assert.That((bool)HeaderCache()[headerId], Is.True);
                Assert.That((bool)SeparatorCache()[headerId], Is.False);
                Assert.That((bool)SeparatorCache()[separatorId], Is.True);
                Assert.That((bool)HeaderCache()[separatorId], Is.False);

                HierarchyHeaderDrawer.ClearHeaderCache();
                HierarchySeparatorDrawer.ClearSeparatorCache();
                Assert.That(HeaderCache().Contains(headerId), Is.False);
                Assert.That(SeparatorCache().Contains(separatorId), Is.False);
            }
            finally
            {
                if (header != null) UnityEngine.Object.DestroyImmediate(header);
                if (separator != null) UnityEngine.Object.DestroyImmediate(separator);
            }
        }

        [Test]
        public void PropertyAndAssetChanges_DoNotCountAsStructural()
        {
            Assert.That(HierarchyCacheRefresh.IsStructuralChange(ObjectChangeKind.None), Is.False);
            Assert.That(HierarchyCacheRefresh.IsStructuralChange(ObjectChangeKind.ChangeGameObjectOrComponentProperties), Is.False);
            Assert.That(HierarchyCacheRefresh.IsStructuralChange(ObjectChangeKind.CreateAssetObject), Is.False);
            Assert.That(HierarchyCacheRefresh.IsStructuralChange(ObjectChangeKind.DestroyAssetObject), Is.False);
            Assert.That(HierarchyCacheRefresh.IsStructuralChange(ObjectChangeKind.ChangeAssetObjectProperties), Is.False);
            Assert.That(HierarchyCacheRefresh.IsStructuralChange(ObjectChangeKind.ChangeChildrenOrder), Is.False);
            Assert.That(HierarchyCacheRefresh.IsStructuralChange(ObjectChangeKind.ChangeRootOrder), Is.False);

            Assert.That(HierarchyCacheRefresh.IsStructuralChange(ObjectChangeKind.ChangeScene), Is.True);
            Assert.That(HierarchyCacheRefresh.IsStructuralChange(ObjectChangeKind.CreateGameObjectHierarchy), Is.True);
            Assert.That(HierarchyCacheRefresh.IsStructuralChange(ObjectChangeKind.ChangeGameObjectStructureHierarchy), Is.True);
            Assert.That(HierarchyCacheRefresh.IsStructuralChange(ObjectChangeKind.ChangeGameObjectStructure), Is.True);
            Assert.That(HierarchyCacheRefresh.IsStructuralChange(ObjectChangeKind.ChangeGameObjectParent), Is.True);
            Assert.That(HierarchyCacheRefresh.IsStructuralChange(ObjectChangeKind.DestroyGameObjectHierarchy), Is.True);
            Assert.That(HierarchyCacheRefresh.IsStructuralChange(ObjectChangeKind.UpdatePrefabInstances), Is.True);

            var stream = default(ObjectChangeEventStream);
            Assert.That(HierarchyCacheRefresh.ContainsStructuralChange(ref stream), Is.False);
        }

        [Test]
        public void MissingInstance_IsNotCached()
        {
#if UNITY_6000_4_OR_NEWER
            var missing = default(EntityId);
#else
            var missing = 0;
#endif
            Assert.That(HierarchyHeaderDrawer.TryGetHeader(missing, out var header), Is.False);
            Assert.That(header, Is.Null);
            Assert.That(HeaderCache().Contains(missing), Is.False);

            Assert.That(HierarchySeparatorDrawer.TryGetSeparator(missing, out var separator), Is.False);
            Assert.That(separator, Is.Null);
            Assert.That(SeparatorCache().Contains(missing), Is.False);
        }

        static IDictionary HeaderCache() => Cache(typeof(HierarchyHeaderDrawer), "HasHeaderCached");

        static IDictionary SeparatorCache() => Cache(typeof(HierarchySeparatorDrawer), "HasSeparatorCached");

        static IDictionary Cache(Type type, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null);
            return (IDictionary)field.GetValue(null);
        }

#if UNITY_6000_4_OR_NEWER
        static EntityId RowId(GameObject gameObject) => gameObject.GetEntityId();
#else
        static int RowId(GameObject gameObject) => gameObject.GetInstanceID();
#endif
    }
}
