using UnityEngine;

/// <summary>
/// Lightweight data container that defines how many crew members a station expects.
/// Place this on weapon/engine prefabs so mounts can pull the correct staffing limits at runtime.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Teramyyd/Crew/Crew Station Requirement Profile")]
public class CrewStationRequirementProfile : MonoBehaviour
{
    [Min(0)]
    [Tooltip("Minimum crew that must be assigned before the station counts as staffed.")]
    [SerializeField] int minimumCrewRequired = 1;

    [Min(1)]
    [Tooltip("Absolute crew cap for this station.")]
    [SerializeField] int maximumCrewAllowed = 2;

    public int MinimumCrewRequired => Mathf.Max(0, minimumCrewRequired);
    public int MaximumCrewAllowed => Mathf.Max(MinimumCrewRequired, maximumCrewAllowed);

    /// <summary>
    /// Applies the configured limits to a station.
    /// </summary>
    public void ApplyTo(CrewStation station)
    {
        if (station == null)
            return;

        station.SetCrewLimits(MinimumCrewRequired, MaximumCrewAllowed);
    }
}
