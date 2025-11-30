#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Property drawer that disables editing for fields marked with <see cref="ReadOnlyInInspectorAttribute"/>.
/// </summary>
[CustomPropertyDrawer(typeof(ReadOnlyInInspectorAttribute))]
public class ReadOnlyInInspectorDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool previousState = GUI.enabled;
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = previousState;
    }
}
#endif
