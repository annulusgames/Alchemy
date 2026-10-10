using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Alchemy.Editor;
using Alchemy.Inspector;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class InspectorCallbackCacheTest
    {
        [SetUp]
        public void SetUp() => InspectorCallbackCacheLog.Entries.Clear();

        [Test]
        public void LifecycleMethods_MatchUncachedReflectionOrder()
        {
            var host = ScriptableObject.CreateInstance<InspectorCallbackCacheHost>();
            UnityEditor.Editor editor = null;
            try
            {
                editor = UnityEditor.Editor.CreateEditor(host, typeof(InspectorCallbackCacheEditor));
                Assert.That(editor, Is.Not.Null);

                var created = InspectorCallbackCacheLog.Entries.ToArray();
                AssertInvokes<OnInspectorEnableAttribute>(editor, "OnEnable", created);
                AssertInvokes<OnInspectorDisableAttribute>(editor, "OnDisable", null);
                AssertInvokes<OnInspectorDestroyAttribute>(editor, "OnDestroy", null);

                var other = ScriptableObject.CreateInstance<InspectorCallbackCacheHost>();
                UnityEditor.Editor otherEditor = null;
                try
                {
                    InspectorCallbackCacheLog.Entries.Clear();
                    otherEditor = UnityEditor.Editor.CreateEditor(other, typeof(InspectorCallbackCacheEditor));
                    Assert.That(otherEditor, Is.Not.Null);
                    var otherCreated = InspectorCallbackCacheLog.Entries.ToArray();
                    AssertInvokes<OnInspectorEnableAttribute>(otherEditor, "OnEnable", otherCreated);
                }
                finally
                {
                    if (otherEditor != null)
                        UnityEngine.Object.DestroyImmediate(otherEditor);
                    UnityEngine.Object.DestroyImmediate(other);
                }
            }
            finally
            {
                if (editor != null)
                    UnityEngine.Object.DestroyImmediate(editor);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MultipleTargets_InvokesInstanceAndStaticMethodsOncePerTarget()
        {
            var first = ScriptableObject.CreateInstance<InspectorCallbackCacheHost>();
            var second = ScriptableObject.CreateInstance<InspectorCallbackCacheHost>();
            UnityEditor.Editor editor = null;
            try
            {
                editor = UnityEditor.Editor.CreateEditor(
                    new UnityEngine.Object[] { first, second },
                    typeof(InspectorCallbackCacheEditor));
                Assert.That(editor, Is.Not.Null);
                Assert.That(editor.targets, Has.Length.EqualTo(2));

                var created = InspectorCallbackCacheLog.Entries.ToArray();
                AssertInvokes<OnInspectorEnableAttribute>(editor, "OnEnable", created);
                AssertInvokes<OnInspectorDisableAttribute>(editor, "OnDisable", null);
                AssertInvokes<OnInspectorDestroyAttribute>(editor, "OnDestroy", null);
            }
            finally
            {
                if (editor != null)
                    UnityEngine.Object.DestroyImmediate(editor);
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void TypeWithoutCallbacks_InvokesNothing()
        {
            var host = ScriptableObject.CreateInstance<InspectorCallbackCacheEmpty>();
            UnityEditor.Editor editor = null;
            try
            {
                editor = UnityEditor.Editor.CreateEditor(host, typeof(InspectorCallbackCacheEditor));
                Assert.That(editor, Is.Not.Null);
                AssertInvokes<OnInspectorEnableAttribute>(editor, "OnEnable", InspectorCallbackCacheLog.Entries.ToArray());
                AssertInvokes<OnInspectorDisableAttribute>(editor, "OnDisable", null);
                AssertInvokes<OnInspectorDestroyAttribute>(editor, "OnDestroy", null);
            }
            finally
            {
                if (editor != null)
                    UnityEngine.Object.DestroyImmediate(editor);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        static void AssertInvokes<T>(UnityEditor.Editor editor, string methodName, (string Label, UnityEngine.Object Instance)[] prelude) where T : Attribute
        {
            var expected = Expected<T>(editor.targets);
            if (prelude != null)
                Assert.That(prelude, Is.EqualTo(expected));

            InspectorCallbackCacheLog.Entries.Clear();
            InvokeLifecycle(editor, methodName);
            Assert.That(InspectorCallbackCacheLog.Entries, Is.EqualTo(expected));

            InspectorCallbackCacheLog.Entries.Clear();
            InvokeLifecycle(editor, methodName);
            Assert.That(InspectorCallbackCacheLog.Entries, Is.EqualTo(expected));
        }

        static void InvokeLifecycle(UnityEditor.Editor editor, string methodName)
        {
            var method = typeof(AlchemyEditor).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(editor, null);
        }

        static (string Label, UnityEngine.Object Instance)[] Expected<T>(UnityEngine.Object[] targets) where T : Attribute
        {
            var methods = ReflectionHelper.GetAllMethodsIncludingBaseNonPublic(targets[0].GetType())
                .Where(method => method.HasCustomAttribute<T>())
                .ToArray();

            var labels = new List<(string, UnityEngine.Object)>(methods.Length * targets.Length);
            foreach (var target in targets)
            {
                foreach (var method in methods)
                    labels.Add((method.DeclaringType.Name + "." + method.Name, method.IsStatic ? null : target));
            }

            return labels.ToArray();
        }
    }

    static class InspectorCallbackCacheLog
    {
        public static readonly List<(string Label, UnityEngine.Object Instance)> Entries = new();

        public static void Add(Type declaringType, string methodName, UnityEngine.Object instance)
        {
            Entries.Add((declaringType.Name + "." + methodName, instance));
        }
    }

    public class InspectorCallbackCacheBase : ScriptableObject
    {
        [OnInspectorEnable]
        void BaseEnable() => InspectorCallbackCacheLog.Add(typeof(InspectorCallbackCacheBase), nameof(BaseEnable), this);

        [OnInspectorDisable]
        void BaseDisable() => InspectorCallbackCacheLog.Add(typeof(InspectorCallbackCacheBase), nameof(BaseDisable), this);

        [OnInspectorDestroy]
        void BaseDestroy() => InspectorCallbackCacheLog.Add(typeof(InspectorCallbackCacheBase), nameof(BaseDestroy), this);

        [OnInspectorEnable]
        static void StaticEnable() => InspectorCallbackCacheLog.Add(typeof(InspectorCallbackCacheBase), nameof(StaticEnable), null);
    }

    public class InspectorCallbackCacheHost : InspectorCallbackCacheBase
    {
        [OnInspectorEnable]
        void DerivedEnable() => InspectorCallbackCacheLog.Add(typeof(InspectorCallbackCacheHost), nameof(DerivedEnable), this);

        [OnInspectorDisable]
        public void PublicDisable() => InspectorCallbackCacheLog.Add(typeof(InspectorCallbackCacheHost), nameof(PublicDisable), this);

        [OnInspectorDisable]
        static void StaticDisable() => InspectorCallbackCacheLog.Add(typeof(InspectorCallbackCacheHost), nameof(StaticDisable), null);

        [OnInspectorDestroy]
        void DerivedDestroy() => InspectorCallbackCacheLog.Add(typeof(InspectorCallbackCacheHost), nameof(DerivedDestroy), this);

        void Ignored() => InspectorCallbackCacheLog.Add(typeof(InspectorCallbackCacheHost), nameof(Ignored), this);
    }

    public class InspectorCallbackCacheEmpty : ScriptableObject { }

    [CanEditMultipleObjects]
    sealed class InspectorCallbackCacheEditor : AlchemyEditor { }
}
