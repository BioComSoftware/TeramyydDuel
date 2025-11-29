using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
[AddComponentMenu("Teramyyd/Crew/Crew Member")]
public class CrewMember : MonoBehaviour
{
    [Header("Identity")]
    public string crewId = string.Empty;
    public string displayName = "Crew Member";
    public CrewRole specialization = CrewRole.General;

    [Header("Ratings (1-10)")]
    [Range(1, 10)] public int gunnery = 5;
    [Range(1, 10)] public int navigation = 5;
    [Range(1, 10)] public int driveEngineering = 5;
    [Range(1, 10)] public int liftEngineering = 5;

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

    void EnsureCrewId()
    {
        if (!string.IsNullOrEmpty(crewId))
            return;

        crewId = $"crew_{Guid.NewGuid().ToString("N")}";
    }
}
