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

    [Header("Requirements")]
    public CrewSkill primarySkill = CrewSkill.Gunnery;
    [Tooltip("Minimum skill level required to accept an assignment.")]
    [Min(1f)] public float minimumSkillLevel = 1f;
    [Range(0, 4)] public int minimumCrewRequired = 1;
    [Range(1, 4)] public int maximumCrewAllowed = 1;

    [Header("Training")]
    [Tooltip("Skill used for progression when someone operates this station. Defaults to Primary Skill.")]
    public CrewSkill trainingSkill = CrewSkill.None;
    [Tooltip("Multiplier for how quickly stationed crew gain experience here.")]
    public float skillGainMultiplier = 1f;

    [Header("Status")] public bool enforceRequirements = true;

    readonly List<CrewMember> _assignedCrew = new List<CrewMember>();
    public IReadOnlyList<CrewMember> AssignedCrew => _assignedCrew;

    public bool HasRequiredCrew
    {
        get
        {
            if (!enforceRequirements)
                return true;

            int minRequired = Mathf.Max(0, minimumCrewRequired);
            return _assignedCrew.Count >= minRequired;
        }
    }

    void Awake()
    {
        EnsureStationId();
        maximumCrewAllowed = Mathf.Max(minimumCrewRequired, maximumCrewAllowed);
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
        if (maximumCrewAllowed < minimumCrewRequired)
        {
            maximumCrewAllowed = Mathf.Max(minimumCrewRequired, 1);
        }
    }

    internal bool CanAssign(CrewMember member)
    {
        if (member == null)
            return false;

        if (_assignedCrew.Contains(member))
            return true;

        if (_assignedCrew.Count >= maximumCrewAllowed)
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
