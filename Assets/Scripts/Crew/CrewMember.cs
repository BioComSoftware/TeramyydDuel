using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
[AddComponentMenu("Teramyyd/Crew/Crew Member")]
public class CrewMember : MonoBehaviour
{
    [Header("Identity")]
    public string crewId = string.Empty;
    public string displayName = "Crew Member";
    [Header("Skill Ratings (start at 1)")]
    [Min(1f)] public float gunnery = 1f;
    [Min(1f)] public float navigation = 1f;
    [Min(1f)] public float repair = 1f;
    [Tooltip("Power / drive engineering skill level.")]
    [Min(1f)] public float powerEngineering = 1f;
    [Min(1f)] public float liftEngineering = 1f;

    [Header("Progression")]
    [Tooltip("Hard cap for any skill level.")]
    public float maxSkillLevel = 10f;
    [Tooltip("Seconds of active duty required to gain 1 skill level (before multipliers).")]
    public float secondsPerSkillPoint = 180f;

    [Header("Assignment (Runtime)")]
    [Tooltip("Optional initial station identifier. CrewManager will try to attach this crew to the matching station when the scene loads.")]
    public string initialStationId = string.Empty;

    public Health Health { get; private set; }
    public CrewStation AssignedStation { get; internal set; }
    internal string PendingStationId { get; set; } = string.Empty;

    public string CurrentStationId => AssignedStation != null ? AssignedStation.stationId : PendingStationId;

    void Awake()
    {
        Health = GetComponent<Health>();
        EnsureCrewId();
    }

    void OnEnable()
    {
        CrewManager.Instance.RegisterCrew(this);
    }

    void OnDisable()
    {
        if (CrewManager.HasInstance)
        {
            CrewManager.Instance.UnregisterCrew(this);
        }
    }

    void Update()
    {
        if (AssignedStation == null)
            return;

        CrewSkill skill = AssignedStation.trainingSkill != CrewSkill.None
            ? AssignedStation.trainingSkill
            : AssignedStation.primarySkill;

        if (skill == CrewSkill.None)
            return;

        float gain = Time.deltaTime / Mathf.Max(1f, secondsPerSkillPoint);
        gain *= Mathf.Max(0.1f, AssignedStation.skillGainMultiplier);
        AddSkillProgress(skill, gain);
    }

    void EnsureCrewId()
    {
        if (!string.IsNullOrEmpty(crewId))
            return;

        crewId = $"crew_{Guid.NewGuid().ToString("N")}";
    }

    public float GetSkillLevel(CrewSkill skill)
    {
        switch (skill)
        {
            case CrewSkill.Gunnery: return gunnery;
            case CrewSkill.Navigation: return navigation;
            case CrewSkill.Repair: return repair;
            case CrewSkill.PowerEngineering: return powerEngineering;
            case CrewSkill.LiftEngineering: return liftEngineering;
            default: return 0f;
        }
    }

    public void SetSkillLevel(CrewSkill skill, float value)
    {
        float clamped = Mathf.Clamp(value, 1f, maxSkillLevel);
        switch (skill)
        {
            case CrewSkill.Gunnery:
                gunnery = clamped;
                break;
            case CrewSkill.Navigation:
                navigation = clamped;
                break;
            case CrewSkill.Repair:
                repair = clamped;
                break;
            case CrewSkill.PowerEngineering:
                powerEngineering = clamped;
                break;
            case CrewSkill.LiftEngineering:
                liftEngineering = clamped;
                break;
        }
    }

    public void AddSkillProgress(CrewSkill skill, float delta)
    {
        if (skill == CrewSkill.None || delta <= 0f)
            return;

        float current = GetSkillLevel(skill);
        if (current >= maxSkillLevel)
            return;

        float newLevel = Mathf.Clamp(current + delta, 1f, maxSkillLevel);
        if (newLevel <= current + Mathf.Epsilon)
            return;

        SetSkillLevel(skill, newLevel);

        if (CrewPersistenceManager.Instance != null)
        {
            CrewPersistenceManager.Instance.UpdateCrewSkills(this);
        }
    }
}
