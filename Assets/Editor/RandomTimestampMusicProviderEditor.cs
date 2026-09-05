using System;
using UnityEditor;
using UnityEngine;

namespace OrbitalEscape.Audio.Editor
{
    [CustomEditor(typeof(RandomTimestampMusicProvider))]
    public sealed class RandomTimestampMusicProviderEditor : UnityEditor.Editor
    {
        private SerializedProperty _trackProperty;
        private SerializedProperty _timestampsProperty;
        private int _selectedTimestampIndex;
        private int _previewTimestampMilliseconds;

        private void OnEnable()
        {
            _trackProperty = serializedObject.FindProperty("_track");
            _timestampsProperty = serializedObject.FindProperty("_startTimestampsMilliseconds");
            UseConfiguredTimestamp(0);
        }

        private void OnDisable()
        {
            AudioPreviewUtility.Stop();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_trackProperty);
            EditorGUILayout.PropertyField(_timestampsProperty, true);
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audio Preview", EditorStyles.boldLabel);

            AudioClip track = _trackProperty.objectReferenceValue as AudioClip;
            if (track == null)
            {
                EditorGUILayout.HelpBox("Assign a track to enable timestamp preview.", MessageType.Info);
                return;
            }

            DrawConfiguredTimestampSelector();
            _previewTimestampMilliseconds = Mathf.Max(
                0,
                EditorGUILayout.IntField("Preview Timestamp (ms)", _previewTimestampMilliseconds));

            int trackLengthMilliseconds = Mathf.RoundToInt(track.length * 1000.0f);
            EditorGUILayout.LabelField("Track Length", FormatTimestamp(trackLengthMilliseconds));
            if (_previewTimestampMilliseconds >= trackLengthMilliseconds && trackLengthMilliseconds > 0)
            {
                EditorGUILayout.HelpBox(
                    "The preview timestamp exceeds the track length and will be clamped to the final sample.",
                    MessageType.Warning);
            }

            if (!AudioPreviewUtility.IsSupported)
            {
                EditorGUILayout.HelpBox("Audio preview is unavailable in this Unity Editor version.", MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Play From Timestamp"))
                {
                    AudioPreviewUtility.Play(track, _previewTimestampMilliseconds);
                }

                if (GUILayout.Button("Stop"))
                {
                    AudioPreviewUtility.Stop();
                }
            }
        }

        private void DrawConfiguredTimestampSelector()
        {
            int timestampCount = _timestampsProperty.arraySize;
            if (timestampCount == 0)
            {
                EditorGUILayout.HelpBox("No configured timestamps. Preview starts at 0 ms.", MessageType.None);
                return;
            }

            _selectedTimestampIndex = Mathf.Clamp(_selectedTimestampIndex, 0, timestampCount - 1);
            string[] options = new string[timestampCount];
            for (int i = 0; i < timestampCount; i++)
            {
                int timestamp = Mathf.Max(0, _timestampsProperty.GetArrayElementAtIndex(i).intValue);
                options[i] = $"{i + 1}: {FormatTimestamp(timestamp)}";
            }

            EditorGUI.BeginChangeCheck();
            int selectedIndex = EditorGUILayout.Popup("Configured Timestamp", _selectedTimestampIndex, options);
            if (EditorGUI.EndChangeCheck())
            {
                UseConfiguredTimestamp(selectedIndex);
            }
        }

        private void UseConfiguredTimestamp(int index)
        {
            int timestampCount = _timestampsProperty?.arraySize ?? 0;
            if (timestampCount == 0)
            {
                _selectedTimestampIndex = 0;
                _previewTimestampMilliseconds = 0;
                return;
            }

            _selectedTimestampIndex = Mathf.Clamp(index, 0, timestampCount - 1);
            _previewTimestampMilliseconds = Mathf.Max(
                0,
                _timestampsProperty.GetArrayElementAtIndex(_selectedTimestampIndex).intValue);
        }

        private static string FormatTimestamp(int milliseconds)
        {
            TimeSpan time = TimeSpan.FromMilliseconds(Mathf.Max(0, milliseconds));
            return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}.{time.Milliseconds:000}";
        }
    }
}
