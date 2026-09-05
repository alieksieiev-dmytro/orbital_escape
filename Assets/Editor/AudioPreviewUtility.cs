using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace OrbitalEscape.Audio.Editor
{
    internal static class AudioPreviewUtility
    {
        private const BindingFlags MethodFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Type AudioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        private static readonly MethodInfo PlayPreviewClipMethod = AudioUtilType?.GetMethod(
            "PlayPreviewClip",
            MethodFlags,
            null,
            new[] { typeof(AudioClip), typeof(int), typeof(bool) },
            null);
        private static readonly MethodInfo StopAllPreviewClipsMethod = AudioUtilType?.GetMethod(
            "StopAllPreviewClips",
            MethodFlags);

        public static bool IsSupported => PlayPreviewClipMethod != null && StopAllPreviewClipsMethod != null;

        public static void Play(AudioClip clip, int startTimeMilliseconds)
        {
            if (!IsSupported || clip == null)
            {
                return;
            }

            Stop();
            int startSample = GetStartSample(clip, startTimeMilliseconds);
            PlayPreviewClipMethod.Invoke(null, new object[] { clip, startSample, false });
        }

        public static void Stop()
        {
            StopAllPreviewClipsMethod?.Invoke(null, null);
        }

        private static int GetStartSample(AudioClip clip, int startTimeMilliseconds)
        {
            if (clip.samples <= 0 || clip.frequency <= 0)
            {
                return 0;
            }

            long requestedSample = (long)Mathf.Max(0, startTimeMilliseconds) * clip.frequency / 1000L;
            return (int)Math.Min(requestedSample, clip.samples - 1L);
        }
    }
}
