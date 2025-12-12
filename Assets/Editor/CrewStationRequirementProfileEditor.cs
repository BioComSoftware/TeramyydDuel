using UnityEngine;
using UnityEditor;

/// <summary>  
/// Custom inspector for CrewStationRequirementProfile that shows/hides fields based on accrual method.
/// </summary>
[CustomEditor(typeof(CrewStationRequirementProfile))]
public class CrewStationRequirementProfileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CrewStationRequirementProfile profile = (CrewStationRequirementProfile)target;

        // Requirements Header
        EditorGUILayout.LabelField("Requirements", EditorStyles.boldLabel);
        profile.primarySkill = (CrewSkill)EditorGUILayout.EnumPopup(
            new GUIContent("Primary Skill", "Primary skill type required to operate this station."),
            profile.primarySkill);
        
        profile.minimumSkillLevel = EditorGUILayout.FloatField(
            new GUIContent("Minimum Skill Level", "Minimum skill level required to accept an assignment."),
            Mathf.Max(1f, profile.minimumSkillLevel));

        SerializedProperty minCrewProp = serializedObject.FindProperty("minimumCrewRequired");
        EditorGUILayout.PropertyField(minCrewProp, new GUIContent("Minimum Crew Required", 
            "Minimum crew that must be assigned before the station counts as staffed."));
        minCrewProp.intValue = Mathf.Max(0, minCrewProp.intValue);

        SerializedProperty maxCrewProp = serializedObject.FindProperty("maximumCrewAllowed");
        EditorGUILayout.PropertyField(maxCrewProp, new GUIContent("Maximum Crew Allowed", 
            "Absolute crew cap for this station. Determines how many anchors are created."));
        maxCrewProp.intValue = Mathf.Max(1, maxCrewProp.intValue);

        EditorGUILayout.Space();

        // Training Header
        EditorGUILayout.LabelField("Training", EditorStyles.boldLabel);
        profile.trainingSkill = (CrewSkill)EditorGUILayout.EnumPopup(
            new GUIContent("Training Skill", "Skill used for progression when someone operates this station. Defaults to Primary Skill if set to None."),
            profile.trainingSkill);

        profile.accrualMethod = (SkillAccrualMethod)EditorGUILayout.EnumPopup(
            new GUIContent("Accrual Method", "How crew members gain experience at this station."),
            profile.accrualMethod);

        // Show fields conditionally based on accrual method
        if (profile.accrualMethod == SkillAccrualMethod.Time)
        {
            EditorGUI.indentLevel++;
            profile.skillGainPerSecond = EditorGUILayout.FloatField(
                new GUIContent("Skill Gain Per Second", "Skill gain per game second (for Time-based accrual)."),
                Mathf.Max(0f, profile.skillGainPerSecond));
            EditorGUI.indentLevel--;
        }
        else if (profile.accrualMethod == SkillAccrualMethod.Event)
        {
            EditorGUI.indentLevel++;
            profile.accrualEvent = (SkillAccrualEvent)EditorGUILayout.EnumPopup(
                new GUIContent("Accrual Event", "Event type that triggers skill gain."),
                profile.accrualEvent);

            profile.skillGainPerEvent = EditorGUILayout.FloatField(
                new GUIContent("Skill Gain Per Event", "Skill gain per event occurrence."),
                Mathf.Max(0f, profile.skillGainPerEvent));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Status Header
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        profile.enforceRequirements = EditorGUILayout.Toggle(
            new GUIContent("Enforce Requirements", "If true, the station will not function unless the minimum crew is assigned."),
            profile.enforceRequirements);

        // Apply changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
