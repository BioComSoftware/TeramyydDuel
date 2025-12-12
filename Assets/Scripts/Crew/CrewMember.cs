using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
[AddComponentMenu("Teramyyd/Crew/Crew Member")]
public class CrewMember : MonoBehaviour
{
    static readonly Stack<CrewMemberState> _bootstrapStates = new Stack<CrewMemberState>();

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
    [Min(1f)] public float fighting = 1f;

    [Header("Progression")]
    [Tooltip("Hard cap for any skill level.")]
    public float maxSkillLevel = 10f;
    
    [Header("Skill Progression Rates")]
    [Tooltip("Cannon shots needed to go from level 1 to 10 in Gunnery.")]
    public int gunneryActionsFor1to10 = 200;
    [Tooltip("Seconds at station to go from level 1 to 10 in Navigation.")]
    public float navigationSecondsFor1to10 = 2000f;
    [Tooltip("Seconds at station to go from level 1 to 10 in Repair.")]
    public float repairSecondsFor1to10 = 2000f;
    [Tooltip("Seconds at engine station to go from level 1 to 10 in Power Engineering.")]
    public float powerEngineeringSecondsFor1to10 = 2000f;
    [Tooltip("Seconds at lift station to go from level 1 to 10 in Lift Engineering.")]
    public float liftEngineeringSecondsFor1to10 = 2000f;
    [Tooltip("Seconds in boarding combat to go from level 1 to 10 in Fighting.")]
    public float fightingSecondsFor1to10 = 200f;

    [Header("Assignment (Runtime)")]
    [Tooltip("Optional initial station identifier. CrewManager will try to attach this crew to the matching station when the scene loads.")]
    public string initialStationId = string.Empty;

    [Header("Visual Representation")]
    [Tooltip("Optional renderer to color based on health status. Leave null to skip health coloring.")]
    public Renderer healthIndicatorRenderer;
    [Tooltip("Material property name to modify for health color (default is _Color).")]
    public string healthColorProperty = "_Color";

    Material _healthMaterialInstance;

    public Health Health { get; private set; }
    public CrewStation AssignedStation { get; internal set; }
    internal string PendingStationId { get; set; } = string.Empty;

    public string CurrentStationId => AssignedStation != null ? AssignedStation.stationId : PendingStationId;

    void Awake()
    {
        Health = GetComponent<Health>();
        ApplyBootstrapStateIfPresent();
        EnsureCrewId();
        InitializeHealthMaterial();
    }
    
    void InitializeHealthMaterial()
    {
        if (healthIndicatorRenderer != null && healthIndicatorRenderer.material != null)
        {
            // Create a unique material instance for this crew member
            _healthMaterialInstance = new Material(healthIndicatorRenderer.material);
            healthIndicatorRenderer.material = _healthMaterialInstance;
            
            string msg = $"[CrewMember] {displayName}: Created health material instance";
            Debug.Log(msg);
            FileLogger.Log(msg, "Crew");
        }
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
        // Update health color indicator
        UpdateHealthColor();

        if (AssignedStation == null)
            return;

        // Only process time-based accrual in Update
        if (AssignedStation.accrualMethod != SkillAccrualMethod.Time)
            return;

        CrewSkill skill = AssignedStation.trainingSkill != CrewSkill.None
            ? AssignedStation.trainingSkill
            : AssignedStation.primarySkill;

        if (skill == CrewSkill.None)
            return;

        float gain = Time.deltaTime * AssignedStation.skillGainPerSecond;
        AddSkillProgress(skill, gain);
    }
    
    float GetSecondsFor1to10(CrewSkill skill)
    {
        switch (skill)
        {
            case CrewSkill.Navigation: return navigationSecondsFor1to10;
            case CrewSkill.Repair: return repairSecondsFor1to10;
            case CrewSkill.PowerEngineering: return powerEngineeringSecondsFor1to10;
            case CrewSkill.LiftEngineering: return liftEngineeringSecondsFor1to10;
            case CrewSkill.Fighting: return fightingSecondsFor1to10;
            default: return 2000f;
        }
    }

    void EnsureCrewId()
    {
        if (!string.IsNullOrEmpty(crewId))
            return;

        crewId = $"crew_{Guid.NewGuid().ToString("N")}";
    }

    void ApplyBootstrapStateIfPresent()
    {
        CrewMemberState state = PopBootstrapState();
        if (state == null)
            return;

        if (!string.IsNullOrEmpty(state.crewId))
        {
            crewId = state.crewId;
        }

        if (!string.IsNullOrEmpty(state.displayName))
        {
            displayName = state.displayName;
        }

        gunnery = Mathf.Max(1f, state.gunnery);
        navigation = Mathf.Max(1f, state.navigation);
        repair = Mathf.Max(1f, state.repair);
        powerEngineering = Mathf.Max(1f, state.powerEngineering);
        liftEngineering = Mathf.Max(1f, state.liftEngineering);
        if (!string.IsNullOrEmpty(state.assignedStationId))
        {
            initialStationId = state.assignedStationId;
        }

        if (Health != null)
        {
            if (state.maxHealth > 0f)
            {
                Health.maxHealth = state.maxHealth;
            }

            float desiredHealth = state.currentHealth > 0f ? state.currentHealth : Health.maxHealth;
            Health.SetHealth(Mathf.Clamp(desiredHealth, 0f, Health.maxHealth));
        }
    }

    internal static void PushBootstrapState(CrewMemberState state)
    {
        if (state == null)
            return;

        _bootstrapStates.Push(state);
    }

    internal static void DiscardBootstrapState()
    {
        if (_bootstrapStates.Count > 0)
        {
            _bootstrapStates.Pop();
        }
    }

    static CrewMemberState PopBootstrapState()
    {
        return _bootstrapStates.Count > 0 ? _bootstrapStates.Pop() : null;
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
    
    /// <summary>
    /// Called by weapon systems when this crew member fires a weapon.
    /// Grants skill XP based on station's configured event-based progression.
    /// </summary>
    public void OnWeaponFired()
    {
        if (AssignedStation == null)
            return;

        // Only process if station uses event-based accrual with PerFiring event
        if (AssignedStation.accrualMethod != SkillAccrualMethod.Event)
            return;
        
        if (AssignedStation.accrualEvent != SkillAccrualEvent.PerFiring)
            return;

        CrewSkill skill = AssignedStation.trainingSkill != CrewSkill.None
            ? AssignedStation.trainingSkill
            : AssignedStation.primarySkill;

        if (skill == CrewSkill.None)
            return;

        AddSkillProgress(skill, AssignedStation.skillGainPerEvent);
    }

    void UpdateHealthColor()
    {
        if (_healthMaterialInstance == null || Health == null)
            return;

        float healthPercent = Health.maxHealth > 0f 
            ? Mathf.Clamp01(Health.currentHealth / Health.maxHealth) 
            : 1f;

        // Two-stage gradient: Green (100%) → Yellow (50%) → Red (0%)
        Color healthColor;
        if (healthPercent > 0.5f)
        {
            // 100% to 50%: Green → Yellow
            healthColor = Color.Lerp(Color.yellow, Color.green, (healthPercent - 0.5f) * 2f);
        }
        else
        {
            // 50% to 0%: Yellow → Red
            healthColor = Color.Lerp(Color.red, Color.yellow, healthPercent * 2f);
        }

        // Apply color to material instance
        // Try both URP (_BaseColor) and Standard (_Color) properties
        if (_healthMaterialInstance.HasProperty("_BaseColor"))
        {
            _healthMaterialInstance.SetColor("_BaseColor", healthColor);
        }
        else if (_healthMaterialInstance.HasProperty(healthColorProperty))
        {
            _healthMaterialInstance.SetColor(healthColorProperty, healthColor);
        }
        else
        {
            // Log once when material doesn't have the property
            if (Time.frameCount % 300 == 0) // Log every ~5 seconds
            {
                string msg = $"[CrewMember] {displayName}: Material doesn't have _BaseColor or {healthColorProperty} property";
                Debug.LogWarning(msg);
                FileLogger.Log(msg, "Crew");
            }
        }
    }
}
