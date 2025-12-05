using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[AddComponentMenu("Teramyyd/Crew/Crew Manager")]
public class CrewManager : MonoBehaviour
{
    static CrewManager _instance;
    public static CrewManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CrewManager>();
                if (_instance == null)
                {
                    var go = new GameObject("CrewManager");
                    _instance = go.AddComponent<CrewManager>();
                }
            }
            return _instance;
        }
    }

    public static bool HasInstance => _instance != null;

    [Header("Policy")]
    [Tooltip("When enabled, engines, lift devices, and weapon mounts will refuse to operate without the required crew assignments.")]
    public bool enforceCrewRequirements = false;

    public IEnumerable<CrewMember> RegisteredCrew => _crewById.Values;
    public IEnumerable<CrewStation> RegisteredStations => _stationsById.Values;

    readonly Dictionary<string, CrewMember> _crewById = new Dictionary<string, CrewMember>();
    readonly Dictionary<string, CrewStation> _stationsById = new Dictionary<string, CrewStation>();
    readonly Dictionary<CrewMember, string> _pendingCrewTargets = new Dictionary<CrewMember, string>();
    readonly Dictionary<string, List<CrewMember>> _pendingByStation = new Dictionary<string, List<CrewMember>>();

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterCrew(CrewMember crew)
    {
        if (crew == null)
            return;

        _crewById[crew.crewId] = crew;

        var state = CrewPersistenceManager.Instance.RegisterCrewMember(crew);
        string targetStationId = state != null ? state.assignedStationId : crew.initialStationId;

        if (!string.IsNullOrEmpty(targetStationId))
        {
            crew.PendingStationId = targetStationId;
            TryAssignCrewToStationId(crew, targetStationId);
        }
    }

    public void UnregisterCrew(CrewMember crew)
    {
        if (crew == null)
            return;

        _crewById.Remove(crew.crewId);

        if (crew.AssignedStation != null)
        {
            crew.AssignedStation.RemoveCrewInternal(crew);
        }

        _pendingCrewTargets.Remove(crew);
        foreach (var kvp in _pendingByStation)
        {
            kvp.Value.Remove(crew);
        }

        CrewPersistenceManager.Instance.UnregisterCrewMember(crew);
    }

    public void RegisterStation(CrewStation station)
    {
        if (station == null)
            return;

        station.EnsureStationId();
        _stationsById[station.stationId] = station;

        if (_pendingByStation.TryGetValue(station.stationId, out var pendingList))
        {
            var copy = pendingList.ToArray();
            foreach (var crew in copy)
            {
                TryAssignCrewToStation(crew, station);
            }
            _pendingByStation.Remove(station.stationId);
        }
    }

    public void HandleStationDisabled(CrewStation station)
    {
        if (station == null)
            return;

        _stationsById.Remove(station.stationId);

        var assignedCopy = station.AssignedCrew.Count > 0 ? station.AssignedCrew.ToArray() : null;
        if (assignedCopy != null)
        {
            foreach (var crew in assignedCopy)
            {
                station.RemoveCrewInternal(crew);
                QueuePendingAssignment(crew, station.stationId);
            }
        }
    }

    public bool TryAssignCrewToStation(CrewMember crew, CrewStation station)
    {
        if (crew == null || station == null)
            return false;

        if (!station.CanAssign(crew))
        {
            QueuePendingAssignment(crew, station.stationId);
            return false;
        }

        RemoveFromCurrentStation(crew);
        station.AddCrewInternal(crew);
        _pendingCrewTargets.Remove(crew);
        CrewPersistenceManager.Instance.UpdateCrewAssignment(crew.crewId, station.stationId);
        return true;
    }

    public bool TryAssignCrewToStationId(CrewMember crew, string stationId)
    {
        if (crew == null || string.IsNullOrEmpty(stationId))
            return false;

        if (_stationsById.TryGetValue(stationId, out var station))
        {
            return TryAssignCrewToStation(crew, station);
        }

        QueuePendingAssignment(crew, stationId);
        return false;
    }

    public void UnassignCrew(CrewMember crew)
    {
        if (crew == null)
            return;

        RemoveFromCurrentStation(crew);
        CrewPersistenceManager.Instance.UpdateCrewAssignment(crew.crewId, string.Empty);
    }

    void RemoveFromCurrentStation(CrewMember crew)
    {
        if (crew.AssignedStation != null)
        {
            crew.AssignedStation.RemoveCrewInternal(crew);
            crew.PendingStationId = string.Empty;
        }
    }

    void QueuePendingAssignment(CrewMember crew, string stationId)
    {
        if (crew == null || string.IsNullOrEmpty(stationId))
            return;

        crew.PendingStationId = stationId;
        _pendingCrewTargets[crew] = stationId;
        if (!_pendingByStation.TryGetValue(stationId, out var list))
        {
            list = new List<CrewMember>();
            _pendingByStation[stationId] = list;
        }
        if (!list.Contains(crew))
        {
            list.Add(crew);
        }
    }

    public bool MeetsRequirement(CrewStation station)
    {
        if (!enforceCrewRequirements)
            return true;

        if (station == null)
            return false;

        return station.HasRequiredCrew;
    }

    public bool MeetsRequirement(string stationId)
    {
        if (!enforceCrewRequirements)
            return true;

        if (string.IsNullOrEmpty(stationId))
            return false;

        return _stationsById.TryGetValue(stationId, out var station) && station.HasRequiredCrew;
    }

    public bool TryGetStation(string stationId, out CrewStation station)
    {
        if (string.IsNullOrEmpty(stationId))
        {
            station = null;
            return false;
        }

        return _stationsById.TryGetValue(stationId, out station);
    }

    public IEnumerable<CrewMember> GetUnassignedCrew()
    {
        foreach (var member in _crewById.Values)
        {
            if (member.AssignedStation == null && string.IsNullOrEmpty(member.PendingStationId))
            {
                yield return member;
            }
        }
    }
}
