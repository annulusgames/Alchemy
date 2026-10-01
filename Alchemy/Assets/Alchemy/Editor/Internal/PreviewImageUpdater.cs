using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Alchemy.Editor
{
    internal sealed class PreviewImageUpdater
    {
        public const int DefaultMaxAttempts = 64;

        readonly VisualElement scheduler;
        readonly Image image;
        readonly Func<UnityEngine.Object, Texture> getPreview;
        readonly int maxAttempts;

        IVisualElementScheduledItem job;
        UnityEngine.Object target;
        int attempts;
        bool exhaustedWithoutPreview;

        public PreviewImageUpdater(
            VisualElement scheduler,
            Image image,
            Func<UnityEngine.Object, Texture> getPreview = null,
            int maxAttempts = DefaultMaxAttempts)
        {
            this.scheduler = scheduler;
            this.image = image;
            this.getPreview = getPreview ?? AssetPreview.GetAssetPreview;
            this.maxAttempts = maxAttempts;
        }

        public bool HasActiveJob => job != null;
        public int Attempts => attempts;
        public UnityEngine.Object Target => target;

        public void Update(UnityEngine.Object reference)
        {
            if (reference == null)
            {
                Stop();
                image.image = null;
                target = null;
                attempts = 0;
                exhaustedWithoutPreview = false;
                return;
            }

            if (reference != target)
            {
                Stop();
                image.image = null;
                target = reference;
                attempts = 0;
                exhaustedWithoutPreview = false;
            }
            else if (job != null || exhaustedWithoutPreview)
            {
                // Already polling, or the last job spent its budget without a texture.
                return;
            }

            // Bound each job, but let later Inspector notifications retry or refresh
            // the same reference. Keep its current image until a replacement is ready.
            attempts = 0;
            var completed = false;
            var captured = reference;
            job = scheduler.schedule.Execute(() =>
            {
                if (image.panel == null)
                {
                    Stop();
                    return;
                }

                if (captured != target) return;

                attempts++;
                var preview = getPreview(captured);
                if (preview != null)
                {
                    image.image = preview;
                }

                completed = preview != null || attempts >= maxAttempts;
                if (completed)
                {
                    exhaustedWithoutPreview = preview == null;
                    job = null;
                }
            }).Until(() =>
                image.panel == null
                || completed
                || captured != target
            );
        }

        public void Stop()
        {
            job?.Pause();
            job = null;
        }
    }
}
