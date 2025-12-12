using UnityEngine;

/// <summary>
/// Lightweight data container that defines how many crew members a station expects.
/// Place this on weapon/engine prefabs so mounts can pull the correct staffing limits at runtime.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Teramyyd/Crew/Crew Station Requirement Profile")]
public class CrewStationRequirementProfile : MonoBehaviour
{
    [Header("Requirements")]
    [Tooltip("Primary skill type required to operate this station.")]
    public CrewSkill primarySkill = CrewSkill.Gunnery;
    
    [Tooltip("Minimum skill level required to accept an assignment.")]
    [Min(1f)] public float minimumSkillLevel = 1f;

    [Min(0)]
    [Tooltip("Minimum crew that must be assigned before the station counts as staffed.")]
    [SerializeField] int minimumCrewRequired = 1;

    [Min(1)]
    [Tooltip("Absolute crew cap for this station. Determines how many anchors are created.")]
    [SerializeField] int maximumCrewAllowed = 1;

    [Header("Training")]
    [Tooltip("Skill used for progression when someone operates this station. Defaults to Primary Skill if set to None.")]
    public CrewSkill trainingSkill = CrewSkill.None;
    
    [Tooltip("How crew members gain experience at this station.")]
    public SkillAccrualMethod accrualMethod = SkillAccrualMethod.Event;
    
    [Tooltip("Skill gain per game second (for Time-based accrual).")]
    [Min(0f)] public float skillGainPerSecond = 0.01f;
    
    [Tooltip("Event type that triggers skill gain (for Event-based accrual).")]
    public SkillAccrualEvent accrualEvent = SkillAccrualEvent.PerFiring;
    
    [Tooltip("Skill gain per event occurrence (for Event-based accrual).")]
    [Min(0f)] public float skillGainPerEvent = 0.1f;

    [Header("Status")] 
    [Tooltip("If true, the station will not function unless the minimum crew is assigned.")]
    public bool enforceRequirements = true;

    public int MinimumCrewRequired => Mathf.Max(0, minimumCrewRequired);
    public int MaximumCrewAllowed => Mathf.Max(MinimumCrewRequired, maximumCrewAllowed);

    /// <summary>
    /// Applies all configured profile settings to a given CrewStation.
    /// </summary>
    public void ApplyTo(CrewStation station)
    {
        if (station == null)
            return;

        station.primarySkill = this.primarySkill;
        station.minimumSkillLevel = this.minimumSkillLevel;
        station.trainingSkill = this.trainingSkill == CrewSkill.None ? this.primarySkill : this.trainingSkill;
        station.accrualMethod = this.accrualMethod;
        station.skillGainPerSecond = this.skillGainPerSecond;
        station.accrualEvent = this.accrualEvent;
        station.skillGainPerEvent = this.skillGainPerEvent;
        station.enforceRequirements = this.enforceRequirements;
        
        station.SetCrewLimits(MinimumCrewRequired, MaximumCrewAllowed);
    }
}

/// <summary>
/// Defines how crew members gain experience at a station.
/// </summary>
public enum SkillAccrualMethod
{
    Time,   // Skill increases per game second
    Event   // Skill increases when specific events occur
}

/// <summary>
/// Defines specific event types that can trigger skill gain.
/// </summary>
public enum SkillAccrualEvent
{
    PerFiring  // For weapons: skill increases each time the weapon fires
    // Future: PerRepair, PerNavUpdate, PerEngineAdjustment, etc.
}
