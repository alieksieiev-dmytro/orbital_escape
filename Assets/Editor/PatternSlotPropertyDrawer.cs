using OrbitalEscape.Orbits.Generators.Patterns;
using UnityEditor;
using UnityEngine;

namespace OrbitalEscape.Editor
{
    [CustomPropertyDrawer(typeof(PatternSlot))]
    public sealed class PatternSlotPropertyDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2.0f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty typeProperty = property.FindPropertyRelative("_type");
            SerializedProperty lengthProperty = property.FindPropertyRelative("_length");
            SerializedProperty heightProperty = property.FindPropertyRelative("_height");
            SerializedProperty minHeightProperty = property.FindPropertyRelative("_minHeight");
            SerializedProperty maxHeightProperty = property.FindPropertyRelative("_maxHeight");

            EditorGUI.BeginProperty(position, label, property);

            Rect typeRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(typeRect, typeProperty, label);

            PatternSlotType type = (PatternSlotType)typeProperty.intValue;
            if (type == PatternSlotType.Space || type == PatternSlotType.Spike)
            {
                SerializedProperty valueProperty = type == PatternSlotType.Space
                    ? lengthProperty
                    : heightProperty;
                float defaultValue = type == PatternSlotType.Space
                    ? PatternSlot.DefaultSpaceLength
                    : PatternSlot.DefaultSpikeHeight;
                float minimumValue = type == PatternSlotType.Space
                    ? PatternSlot.MinimumSpaceLength
                    : PatternSlot.MinimumSpikeHeight;
                if (valueProperty.floatValue <= 0.0f)
                {
                    valueProperty.floatValue = defaultValue;
                }

                Rect valueRect = new Rect(
                    position.x,
                    typeRect.yMax + VerticalSpacing,
                    position.width,
                    EditorGUIUtility.singleLineHeight);
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(
                    valueRect,
                    valueProperty,
                    new GUIContent(type == PatternSlotType.Space ? "Length" : "Height"));
                EditorGUI.indentLevel--;
                valueProperty.floatValue = Mathf.Max(minimumValue, valueProperty.floatValue);
            }
            else if (type == PatternSlotType.RandomizedSpike)
            {
                if (minHeightProperty.floatValue <= 0.0f)
                {
                    minHeightProperty.floatValue = PatternSlot.DefaultSpikeHeight;
                }

                if (maxHeightProperty.floatValue <= 0.0f)
                {
                    maxHeightProperty.floatValue = PatternSlot.DefaultRandomizedSpikeMaxHeight;
                }

                Rect minRect = new Rect(
                    position.x,
                    typeRect.yMax + VerticalSpacing,
                    position.width,
                    EditorGUIUtility.singleLineHeight);
                Rect maxRect = new Rect(
                    position.x,
                    minRect.yMax + VerticalSpacing,
                    position.width,
                    EditorGUIUtility.singleLineHeight);
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(minRect, minHeightProperty, new GUIContent("Min Height"));
                EditorGUI.PropertyField(maxRect, maxHeightProperty, new GUIContent("Max Height"));
                EditorGUI.indentLevel--;
                minHeightProperty.floatValue = Mathf.Max(PatternSlot.MinimumSpikeHeight, minHeightProperty.floatValue);
                maxHeightProperty.floatValue = Mathf.Max(minHeightProperty.floatValue, maxHeightProperty.floatValue);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty typeProperty = property.FindPropertyRelative("_type");
            PatternSlotType type = (PatternSlotType)typeProperty.intValue;
            return type switch
            {
                PatternSlotType.Space or PatternSlotType.Spike =>
                    EditorGUIUtility.singleLineHeight * 2.0f + VerticalSpacing,
                PatternSlotType.RandomizedSpike =>
                    EditorGUIUtility.singleLineHeight * 3.0f + VerticalSpacing * 2.0f,
                _ => EditorGUIUtility.singleLineHeight
            };
        }
    }
}
