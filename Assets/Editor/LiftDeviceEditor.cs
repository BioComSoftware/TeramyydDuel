#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LiftDevice), true)]
public class LiftDeviceEditor : Editor
{
    private SerializedProperty _allocatedPower;
    private SerializedProperty _maxLiftPower;

    void OnEnable()
    {
        _allocatedPower = serializedObject.FindProperty("allocatedPowerPerSecond");
        _maxLiftPower = serializedObject.FindProperty("maxLiftPowerPerSecond");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (_allocatedPower != null)
        {
            EditorGUILayout.LabelField("Power Generation", EditorStyles.boldLabel);
            if (_maxLiftPower != null)
            {
                EditorGUILayout.PropertyField(_maxLiftPower);
            }
            EditorGUILayout.PropertyField(_allocatedPower);
            EditorGUILayout.Space();
        }

        if (_maxLiftPower != null)
        {
            DrawPropertiesExcluding(serializedObject, "m_Script", "allocatedPowerPerSecond", "maxLiftPowerPerSecond");
        }
        else
        {
            DrawPropertiesExcluding(serializedObject, "m_Script", "allocatedPowerPerSecond");
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
