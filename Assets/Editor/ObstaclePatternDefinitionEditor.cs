using OrbitalEscape.Orbits.Generators;
using OrbitalEscape.Orbits.Generators.Patterns;
using UnityEditor;
using UnityEngine;

namespace OrbitalEscape.Editor
{
    [CustomEditor(typeof(ObstaclePatternDefinition))]
    public sealed class ObstaclePatternDefinitionEditor : UnityEditor.Editor
    {
        private const string ConfigRoot = "Assets/Resources/Configs/";

        private SpikeConfig _spikeConfig;
        private MiniSpikeConfig _miniSpikeConfig;
        private ImpulseTrapConfig _impulseTrapConfig;
        private GravityWellConfig _gravityWellConfig;
        private LaserGateConfig _laserGateConfig;
        private RiftConfig _riftConfig;

        private void OnEnable()
        {
            _spikeConfig = LoadConfig<SpikeConfig>("SpikeConfig.asset");
            _miniSpikeConfig = LoadConfig<MiniSpikeConfig>("MiniSpikeConfig.asset");
            _impulseTrapConfig = LoadConfig<ImpulseTrapConfig>("ImpulseTrapConfig.asset");
            _gravityWellConfig = LoadConfig<GravityWellConfig>("GravityWellConfig.asset");
            _laserGateConfig = LoadConfig<LaserGateConfig>("LaserGateConfig.asset");
            _riftConfig = LoadConfig<RiftConfig>("RiftConfig.asset");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
                {
                    if (IsSlotList(property))
                    {
                        DrawSlotList(property);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(property, true);
                    }
                }

                if (IsSlotList(property))
                {
                    DrawLaneLength(property);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static bool IsSlotList(SerializedProperty property)
        {
            return property.propertyPath == "_orbit1Slots" || property.propertyPath == "_orbit2Slots";
        }

        private void DrawSlotList(SerializedProperty slots)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUI.GetPropertyHeight(slots, true));
            Rect header = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            if (Event.current.type == EventType.ContextClick && header.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                if (slots.arraySize > 1)
                {
                    ObstaclePatternDefinition pattern = (ObstaclePatternDefinition)target;
                    string propertyPath = slots.propertyPath;
                    menu.AddItem(new GUIContent("Reverse"), false, () => ReverseSlots(pattern, propertyPath));
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Reverse"));
                }

                menu.ShowAsContext();
                Event.current.Use();
            }

            EditorGUI.PropertyField(rect, slots, true);
        }

        private static void ReverseSlots(ObstaclePatternDefinition pattern, string propertyPath)
        {
            if (pattern == null)
            {
                return;
            }

            SerializedObject patternObject = new SerializedObject(pattern);
            SerializedProperty slots = patternObject.FindProperty(propertyPath);
            if (slots == null || slots.arraySize < 2)
            {
                return;
            }

            Undo.RecordObject(pattern, "Reverse Pattern Slots");
            for (int i = 0; i < slots.arraySize - 1; i++)
            {
                slots.MoveArrayElement(slots.arraySize - 1, i);
            }

            patternObject.ApplyModifiedProperties();
        }

        private void DrawLaneLength(SerializedProperty slots)
        {
            if (!TryCreateLengthProvider(out PatternSlotLengthProvider lengthProvider))
            {
                EditorGUILayout.HelpBox(
                    "Total length unavailable because one or more obstacle configs are missing.",
                    MessageType.Warning);
                return;
            }

            float totalLength = 0.0f;
            for (int i = 0; i < slots.arraySize; i++)
            {
                SerializedProperty slot = slots.GetArrayElementAtIndex(i);
                PatternSlotType type = (PatternSlotType)slot.FindPropertyRelative("_type").intValue;
                if (type == PatternSlotType.Space)
                {
                    float authoredLength = slot.FindPropertyRelative("_length").floatValue;
                    totalLength += authoredLength > 0.0f
                        ? Mathf.Max(PatternSlot.MinimumSpaceLength, authoredLength)
                        : PatternSlot.DefaultSpaceLength;
                }
                else
                {
                    totalLength += lengthProvider.GetObstacleLength(type);
                }
            }

            GUIContent content = new GUIContent(
                $"Total length: {totalLength:0.###} units",
                "Sum of authored Space lengths and obstacle lengths from their gameplay configs.");
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 4.0f);
            EditorGUI.LabelField(rect, content, EditorStyles.helpBox);
        }

        private bool TryCreateLengthProvider(out PatternSlotLengthProvider lengthProvider)
        {
            if (_spikeConfig == null ||
                _miniSpikeConfig == null ||
                _impulseTrapConfig == null ||
                _gravityWellConfig == null ||
                _laserGateConfig == null ||
                _riftConfig == null)
            {
                lengthProvider = default;
                return false;
            }

            lengthProvider = new PatternSlotLengthProvider(
                _spikeConfig.SpikeArcLength,
                _miniSpikeConfig.SpikeArcLength,
                _impulseTrapConfig.TrapArcLength,
                _gravityWellConfig.FieldArcLength,
                _riftConfig.RiftLength,
                _laserGateConfig.Width);
            return true;
        }

        private static T LoadConfig<T>(string fileName) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(ConfigRoot + fileName);
        }
    }
}
