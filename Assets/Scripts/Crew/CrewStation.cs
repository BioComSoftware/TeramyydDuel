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
    public CrewRole requiredRole = CrewRole.General;
    [Tooltip("When true, any crew member may fill this station regardless of specialization.")]
    public bool allowGeneralists = true;
    [Range(0, 4)] public int minimumCrewRequired = 1;
    [Range(1, 4)] public int maximumCrewAllowed = 1;

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
        CrewManager.Instance.RegisterStation(this);
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

        if (!allowGeneralists && member.specialization != requiredRole)
            return false;

        return true;
    }

    internal void AddCrewInternal(CrewMember member)
    {
        if (member == null || _assignedCrew.Contains(member))
            return;

        _assignedCrew.Add(member);
        member.AssignedStation = this;
        member.PendingStationId = stationId;
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
        }
    }

    internal void EnsureStationId()
    {
        if (!string.IsNullOrEmpty(stationId))
            return;

        stationId = $"station_{Guid.NewGuid().ToString("N")}";
    }
}
