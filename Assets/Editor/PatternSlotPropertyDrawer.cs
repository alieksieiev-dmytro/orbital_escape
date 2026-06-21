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

            EditorGUI.BeginProperty(position, label, property);

            Rect typeRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(typeRect, typeProperty, label);

            if ((PatternSlotType)typeProperty.intValue == PatternSlotType.Space)
            {
                if (lengthProperty.floatValue <= 0.0f)
                {
                    lengthProperty.floatValue = PatternSlot.DefaultSpaceLength;
                }

                Rect lengthRect = new Rect(
                    position.x,
                    typeRect.yMax + VerticalSpacing,
                    position.width,
                    EditorGUIUtility.singleLineHeight);
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(lengthRect, lengthProperty, new GUIContent("Length"));
                EditorGUI.indentLevel--;
                lengthProperty.floatValue = Mathf.Max(PatternSlot.MinimumSpaceLength, lengthProperty.floatValue);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty typeProperty = property.FindPropertyRelative("_type");
            bool isSpace = (PatternSlotType)typeProperty.intValue == PatternSlotType.Space;
            return isSpace
                ? EditorGUIUtility.singleLineHeight * 2.0f + VerticalSpacing
                : EditorGUIUtility.singleLineHeight;
        }
    }
}
