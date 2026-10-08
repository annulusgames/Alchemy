using System;
using System.Linq;
using System.Reflection;
using Alchemy.Editor;
using Alchemy.Editor.Drawers;
using Alchemy.Inspector;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class DrawerTypeLookupTest
    {
        static Type invokedDrawer;

        [Test]
        public void FindGroupDrawerType_ReturnsBuiltinDrawer()
        {
            var first = AlchemyEditorUtility.FindGroupDrawerType(new GroupAttribute("Group"));
            var second = AlchemyEditorUtility.FindGroupDrawerType(new GroupAttribute("Other"));

            Assert.That(first, Is.EqualTo(typeof(GroupDrawer)));
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void FindGroupDrawerType_ReturnsNullWhenNothingMatches()
        {
            Assert.That(
                AlchemyEditorUtility.FindGroupDrawerType(new DrawerLookupMissingGroupAttribute()),
                Is.Null);
        }

        [Test]
        public void FindGroupDrawerType_KeepsFirstTypeCacheMatch()
        {
            var attribute = new DrawerLookupGroupAttribute();
            var resolved = AlchemyEditorUtility.FindGroupDrawerType(attribute);
            var expected = TypeCache.GetTypesWithAttribute<CustomGroupDrawerAttribute>()
                .FirstOrDefault(x => x.GetCustomAttribute<CustomGroupDrawerAttribute>().targetAttributeType == attribute.GetType());

            Assert.That(expected, Is.Not.Null);
            Assert.That(resolved, Is.EqualTo(expected));
            Assert.That(AlchemyEditorUtility.FindGroupDrawerType(attribute), Is.EqualTo(expected));
        }

        [Test]
        public void ExecutePropertyDrawers_AppliesBuiltinReadOnlyDrawer()
        {
            var element = new VisualElement();
            var member = typeof(DrawerLookupHost).GetField(nameof(DrawerLookupHost.readOnlyValue));
            AlchemyAttributeDrawer.ExecutePropertyDrawers(null, null, new DrawerLookupHost(), member, element);

            Assert.That(element.enabledSelf, Is.False);
        }

        [Test]
        public void ExecutePropertyDrawers_SkipsAttributeWithoutDrawer()
        {
            invokedDrawer = null;
            var element = new VisualElement();
            var member = typeof(DrawerLookupHost).GetField(nameof(DrawerLookupHost.missing));
            AlchemyAttributeDrawer.ExecutePropertyDrawers(null, null, new DrawerLookupHost(), member, element);

            Assert.That(invokedDrawer, Is.Null);
            Assert.That(element.enabledSelf, Is.True);
        }

        [Test]
        public void GetDrawerTypeForType_CachesExactDrawerAndNullMiss()
        {
            var drawer = InternalAPIHelper.GetDrawerTypeForType(typeof(DrawerTypeLookupTarget), false);
            Assert.That(drawer, Is.EqualTo(typeof(DrawerTypeLookupTargetDrawer)));
            Assert.That(InternalAPIHelper.GetDrawerTypeForType(typeof(DrawerTypeLookupTarget), false), Is.EqualTo(drawer));
            Assert.That(InternalAPIHelper.GetDrawerTypeForType(typeof(DrawerTypeLookupTarget), true), Is.EqualTo(drawer));

            Assert.That(InternalAPIHelper.GetDrawerTypeForType(typeof(DrawerTypeLookupPlain), false), Is.Null);
            Assert.That(InternalAPIHelper.GetDrawerTypeForType(typeof(DrawerTypeLookupPlain), false), Is.Null);
            Assert.That(InternalAPIHelper.GetDrawerTypeForType(typeof(DrawerTypeLookupPlain), true), Is.Null);
            Assert.That(InternalAPIHelper.GetDrawerTypeForType(typeof(DrawerTypeLookupPlain), true), Is.Null);
        }

        [Test]
        public void GetDrawerTypeForType_KeepsManagedReferenceFlagSeparateFromCachedMiss()
        {
            var child = typeof(DrawerTypeLookupDerived);
            Assert.That(InternalAPIHelper.GetDrawerTypeForType(child, false), Is.Null);

            var managed = InternalAPIHelper.GetDrawerTypeForType(child, true);
            Assert.That(managed, Is.EqualTo(typeof(DrawerTypeLookupBaseDrawer)));

            // Unity's own cache is keyed only by type, so a later miss would otherwise see the managed hit.
            Assert.That(InternalAPIHelper.GetDrawerTypeForType(child, false), Is.Null);
            Assert.That(InternalAPIHelper.GetDrawerTypeForType(child, true), Is.EqualTo(managed));
        }

#if UNITY_6000_0_OR_NEWER
        [TestCase(false)]
        [TestCase(true)]
        public void GetDrawerTypeForType_RefreshesAfterRenderPipelineLifecycleChange(bool dispose)
        {
            var prepare = typeof(RenderPipelineManager).GetMethod("TryPrepareRenderPipeline", BindingFlags.Static | BindingFlags.NonPublic);
            var lookup = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ScriptAttributeUtility")
                .GetMethod("GetDrawerTypeForType", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = GraphicsSettings.currentRenderPipeline;
            var asset = ScriptableObject.CreateInstance<DrawerLookupPipelineAsset>();
            try
            {
                prepare.Invoke(null, new object[] { dispose ? asset : null });
                // Unity's normal PropertyField lookup populates this cache with the
                // supported pipelines before Alchemy queries it with a null pipeline list.
                var expected = lookup.Invoke(null, new object[]
                {
                    typeof(PipelineDrawerTarget), new[] { typeof(DrawerLookupPipelineAsset) }, false
                });
                Assert.That(expected, Is.EqualTo(typeof(PipelineTargetDrawer)));
                Assert.That(InternalAPIHelper.GetDrawerTypeForType(typeof(PipelineDrawerTarget), false), Is.EqualTo(expected));

                prepare.Invoke(null, new object[] { dispose ? null : asset });

                Assert.That(lookup.Invoke(null, new object[] { typeof(PipelineDrawerTarget), null, false }), Is.Null);
                Assert.That(InternalAPIHelper.GetDrawerTypeForType(typeof(PipelineDrawerTarget), false), Is.Null);
            }
            finally
            {
                prepare.Invoke(null, new object[] { previous });
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        sealed class PipelineDrawerTarget { }

        [CustomPropertyDrawer(typeof(PipelineDrawerTarget))]
        [SupportedOnRenderPipeline(typeof(DrawerLookupPipelineAsset))]
        sealed class PipelineTargetDrawer : PropertyDrawer { }

        sealed class DrawerLookupPipelineAsset : RenderPipelineAsset
        {
            protected override RenderPipeline CreatePipeline() => new DrawerLookupPipeline();
        }

        sealed class DrawerLookupPipeline : RenderPipeline
        {
            protected override void Render(ScriptableRenderContext context, Camera[] cameras) { }
        }
#endif

        [Test]
        public void ExecutePropertyDrawers_KeepsFirstSubclassMatch()
        {
            var expected = TypeCache.GetTypesWithAttribute(typeof(CustomAttributeDrawerAttribute))
                .FirstOrDefault(x =>
                    x.IsSubclassOf(typeof(AlchemyAttributeDrawer)) &&
                    x.GetCustomAttribute<CustomAttributeDrawerAttribute>().targetAttributeType == typeof(DrawerLookupMarkerAttribute));

            invokedDrawer = null;
            var element = new VisualElement();
            var member = typeof(DrawerLookupHost).GetField(nameof(DrawerLookupHost.marked));
            AlchemyAttributeDrawer.ExecutePropertyDrawers(null, null, new DrawerLookupHost(), member, element);

            Assert.That(expected, Is.Not.Null);
            Assert.That(invokedDrawer, Is.EqualTo(expected));
        }

        sealed class DrawerTypeLookupTarget
        {
            public int value;
        }

        sealed class DrawerTypeLookupPlain
        {
            public int value;
        }

        class DrawerTypeLookupBase
        {
            public int value;
        }

        sealed class DrawerTypeLookupDerived : DrawerTypeLookupBase
        {
        }

        [CustomPropertyDrawer(typeof(DrawerTypeLookupTarget))]
        sealed class DrawerTypeLookupTargetDrawer : PropertyDrawer
        {
        }

        [CustomPropertyDrawer(typeof(DrawerTypeLookupBase), false)]
        sealed class DrawerTypeLookupBaseDrawer : PropertyDrawer
        {
        }

        sealed class DrawerLookupHost
        {
            [DrawerLookupMarker]
            public int marked;

            [DrawerLookupMissing]
            public int missing;

            [ReadOnly]
            public int readOnlyValue;
        }

        sealed class DrawerLookupMarkerAttribute : Attribute { }

        sealed class DrawerLookupMissingAttribute : Attribute { }

        sealed class DrawerLookupMissingGroupAttribute : PropertyGroupAttribute { }

        sealed class DrawerLookupGroupAttribute : PropertyGroupAttribute { }

        [CustomGroupDrawer(typeof(DrawerLookupGroupAttribute))]
        sealed class DrawerLookupGroupDrawerA : AlchemyGroupDrawer
        {
            public override VisualElement CreateRootElement(string label)
            {
                return new VisualElement();
            }
        }

        [CustomGroupDrawer(typeof(DrawerLookupGroupAttribute))]
        sealed class DrawerLookupGroupDrawerB : AlchemyGroupDrawer
        {
            public override VisualElement CreateRootElement(string label)
            {
                return new VisualElement();
            }
        }

        [CustomAttributeDrawer(typeof(DrawerLookupMarkerAttribute))]
        sealed class DrawerLookupNotADrawer
        {
        }

        [CustomAttributeDrawer(typeof(DrawerLookupMarkerAttribute))]
        sealed class DrawerLookupMarkerDrawerA : AlchemyAttributeDrawer
        {
            public override void OnCreateElement()
            {
                invokedDrawer = GetType();
            }
        }

        [CustomAttributeDrawer(typeof(DrawerLookupMarkerAttribute))]
        sealed class DrawerLookupMarkerDrawerB : AlchemyAttributeDrawer
        {
            public override void OnCreateElement()
            {
                invokedDrawer = GetType();
            }
        }
    }
}
