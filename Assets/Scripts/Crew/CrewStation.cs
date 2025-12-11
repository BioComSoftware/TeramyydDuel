using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Teramyyd/Crew/Crew Station")]
public class CrewStation : MonoBehaviour
{
    [Header("Identity")]
    public string stationId = string.Empty;
    [Tooltip("Friendly label for UI/debugging.")]
    public string displayName = "Crew Station";

    // Runtime-only configuration (set by CrewStationRequirementProfile.ApplyTo() or SetCrewLimits())
    [System.NonSerialized] public CrewSkill primarySkill = CrewSkill.Gunnery;
    [System.NonSerialized] public float minimumSkillLevel = 1f;
    [System.NonSerialized] public CrewSkill trainingSkill = CrewSkill.None;
    [System.NonSerialized] public float skillGainMultiplier = 1f;
    
    bool _enforceRequirements = true;
    public bool enforceRequirements 
    { 
        get => _enforceRequirements; 
        set => _enforceRequirements = value; 
    }
    
    int _minimumCrewRequired = 1;
    int _maximumCrewAllowed = 1;

    readonly List<CrewMember> _assignedCrew = new List<CrewMember>();
    public IReadOnlyList<CrewMember> AssignedCrew => _assignedCrew;
    public int AssignedCrewCount => _assignedCrew.Count;
    public bool HasAnyCrew => _assignedCrew.Count > 0;
    public bool IsUnderstaffed => enforceRequirements && _assignedCrew.Count > 0 && GetCrewRatio() < 1f;

    public int MinimumCrewRequired => _minimumCrewRequired;
    public int MaximumCrewAllowed => _maximumCrewAllowed;
    
    public bool HasRequiredCrew
    {
        get
        {
            if (!enforceRequirements)
                return true;

            int minRequired = Mathf.Max(0, _minimumCrewRequired);
            return _assignedCrew.Count >= minRequired;
        }
    }

    public float GetCrewRatio()
    {
        int required = Mathf.Max(1, _minimumCrewRequired);
        return _assignedCrew.Count / (float)required;
    }

    public float GetStaffingRatio() => Mathf.Clamp01(GetCrewRatio());

    void Awake()
    {
        EnsureStationId();
        ClampCrewLimits();
    }

    void OnEnable()
    {
        string msg = $"[CrewStation] OnEnable: {stationId} ({displayName})";
        Debug.Log(msg);
        FileLogger.Log(msg, "CrewStation");
        
        if (CrewManager.HasInstance)
        {
            CrewManager.Instance.RegisterStation(this);
        }
        else
        {
            Debug.LogWarning($"[CrewStation] OnEnable: CrewManager not available for {stationId}");
        }
    }

    void OnDisable()
    {
        if (CrewManager.HasInstance)
        {
            CrewManager.Instance.HandleStationDisabled(this);
        }
    }

    void OnValidate()
    {
        ClampCrewLimits();
    }

    internal bool CanAssign(CrewMember member)
    {
        if (member == null)
            return false;

        if (_assignedCrew.Contains(member))
            return true;

        if (_assignedCrew.Count >= _maximumCrewAllowed)
            return false;

        CrewSkill skill = primarySkill;
        if (skill != CrewSkill.None && member.GetSkillLevel(skill) < minimumSkillLevel)
            return false;

        return true;
    }

    internal void AddCrewInternal(CrewMember member)
    {
        if (member == null || _assignedCrew.Contains(member))
            return;

        _assignedCrew.Add(member);
        member.AssignedStation = this;
        member.PendingStationId = string.Empty;
    }

    internal void RemoveCrewInternal(CrewMember member)
    {
        if (member == null)
            return;

        if (_assignedCrew.Remove(member))
        {
            if (member.AssignedStation == this)
            {
                member.AssignedStation = null;
            }
            member.PendingStationId = string.Empty;
        }
    }

    internal void EnsureStationId()
    {
        if (!string.IsNullOrEmpty(stationId))
            return;

        stationId = $"station_{Guid.NewGuid().ToString("N")}";
    }

    public void SetCrewLimits(int minimumRequired, int maximumAllowed)
    {
        _minimumCrewRequired = Mathf.Max(0, minimumRequired);
        _maximumCrewAllowed = Mathf.Max(_minimumCrewRequired, maximumAllowed);
        ClampCrewLimits();
    }

    void ClampCrewLimits()
    {
        _minimumCrewRequired = Mathf.Max(0, _minimumCrewRequired);
        _maximumCrewAllowed = Mathf.Max(_minimumCrewRequired, _maximumCrewAllowed);
    }

    public float GetBestSkillLevel()
    {
        CrewSkill skill = primarySkill;
        if (skill == CrewSkill.None || _assignedCrew.Count == 0)
            return 0f;

        float best = 0f;
        foreach (var crew in _assignedCrew)
        {
            if (crew == null)
                continue;

            best = Mathf.Max(best, crew.GetSkillLevel(skill));
        }

        return best;
    }
}
