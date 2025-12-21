using System;
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
                _instance = FindFirstObjectByType<CrewManager>();
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

    [Header("Debug")]
    [Tooltip("Enables debug logging to console and Logs/game_debug.log.")]
    public bool debugLog = false;

    const string CrewPortraitsPath = "UI/Crew";

    public IEnumerable<CrewMember> RegisteredCrew => _crewById.Values;
    public IEnumerable<CrewStation> RegisteredStations => _stationsById.Values;

    readonly Dictionary<string, CrewMember> _crewById = new Dictionary<string, CrewMember>();
    readonly Dictionary<string, CrewStation> _stationsById = new Dictionary<string, CrewStation>();
    readonly Dictionary<CrewMember, string> _pendingCrewTargets = new Dictionary<CrewMember, string>();
    readonly Dictionary<string, List<CrewMember>> _pendingByStation = new Dictionary<string, List<CrewMember>>();
    readonly Dictionary<string, Sprite> _portraitLookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        LoadCrewPortraits();
    }

    void Start()
    {
        // Find and register all CrewStation components in the scene
        var stations = GetAllStationsIncludingInactive();
        
        foreach (var station in stations)
        {
            if (station != null && station.gameObject.activeInHierarchy)
            {
                RegisterStation(station);
            }
        }
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
        {
            return;
        }

        station.EnsureStationId();
        
        // Warn if we're overwriting an existing station with the same ID
        if (_stationsById.ContainsKey(station.stationId) && _stationsById[station.stationId] != station)
        {
        }
        
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
        {
            Debug.LogWarning("[CrewManager] TryAssignCrewToStation: crew or station is null");
            return false;
        }

        if (debugLog)
        {
            Debug.Log($"[CrewManager] TryAssignCrewToStation: {crew.displayName} -> {station.stationId}");
            FileLogger.Log($"TryAssignCrewToStation: {crew.displayName} -> {station.stationId}", "CrewManager");
        }

        if (!station.CanAssign(crew))
        {
            if (debugLog)
            {
                Debug.LogWarning($"[CrewManager] Station {station.stationId} cannot assign {crew.displayName}, queuing");
                FileLogger.Log($"Station {station.stationId} cannot assign {crew.displayName}, queuing", "CrewManager");
            }
            QueuePendingAssignment(crew, station.stationId);
            return false;
        }

        if (debugLog)
        {
            string beforeStation = crew.AssignedStation?.stationId ?? "null";
            Debug.Log($"[CrewManager] Before assignment - crew.AssignedStation = {beforeStation}");
            FileLogger.Log($"Before assignment - crew.AssignedStation = {beforeStation}", "CrewManager");
        }

        RemoveFromCurrentStation(crew);
        station.AddCrewInternal(crew);
        _pendingCrewTargets.Remove(crew);
        
        if (debugLog)
        {
            string afterStation = crew.AssignedStation?.stationId ?? "null";
            Debug.Log($"[CrewManager] After AddCrewInternal - crew.AssignedStation = {afterStation}");
            FileLogger.Log($"After AddCrewInternal - crew.AssignedStation = {afterStation}", "CrewManager");
        }
        
        CrewPersistenceManager.Instance.UpdateCrewAssignment(crew.crewId, station.stationId);
        
        if (debugLog)
        {
            Debug.Log($"[CrewManager] SUCCESS - {crew.displayName} assigned to {station.stationId}");
            FileLogger.Log($"SUCCESS - {crew.displayName} assigned to {station.stationId}", "CrewManager");
        }
        return true;
    }

    public bool TryAssignCrewToStationId(CrewMember crew, string stationId)
    {
        if (crew == null || string.IsNullOrEmpty(stationId))
        {
            Debug.LogWarning($"[CrewManager] TryAssignCrewToStationId: crew or stationId is null/empty");
            return false;
        }

        if (debugLog)
        {
            Debug.Log($"[CrewManager] TryAssignCrewToStationId: {crew.displayName} -> {stationId}");
            FileLogger.Log($"TryAssignCrewToStationId: {crew.displayName} -> {stationId}", "CrewManager");
        }

        CrewStation station = null;
        if (_stationsById.TryGetValue(stationId, out station))
        {
            bool result = TryAssignCrewToStation(crew, station);
            if (debugLog)
            {
                Debug.Log($"[CrewManager] TryAssignCrewToStationId: Result = {result}, crew.AssignedStation = {crew.AssignedStation?.stationId ?? "null"}");
                FileLogger.Log($"TryAssignCrewToStationId: Result = {result}, crew.AssignedStation = {crew.AssignedStation?.stationId ?? "null"}", "CrewManager");
            }
            return result;
        }

        // Station not registered, try to find it in the scene
        if (debugLog)
        {
            Debug.LogWarning($"[CrewManager] Station {stationId} not registered, searching scene...");
            FileLogger.Log($"Station {stationId} not registered, searching scene...", "CrewManager");
        }
        
        var allStations = GetAllStationsIncludingInactive();
        foreach (var st in allStations)
        {
            if (st.stationId == stationId)
            {
                if (debugLog)
                {
                    Debug.Log($"[CrewManager] Found station {stationId} in scene, registering it now");
                    FileLogger.Log($"Found station {stationId} in scene, registering it now", "CrewManager");
                }
                RegisterStation(st);
                
                bool result = TryAssignCrewToStation(crew, st);
                if (debugLog)
                {
                    Debug.Log($"[CrewManager] TryAssignCrewToStationId: Result = {result}, crew.AssignedStation = {crew.AssignedStation?.stationId ?? "null"}");
                    FileLogger.Log($"TryAssignCrewToStationId: Result = {result}, crew.AssignedStation = {crew.AssignedStation?.stationId ?? "null"}", "CrewManager");
                }
                return result;
            }
        }

        if (debugLog)
        {
            Debug.LogWarning($"[CrewManager] Station {stationId} not found anywhere, queuing pending assignment for {crew.displayName}");
            FileLogger.Log($"Station {stationId} not found anywhere, queuing pending assignment for {crew.displayName}", "CrewManager");
        }
        QueuePendingAssignment(crew, stationId);
        return false;
    }

    public void UnassignCrew(CrewMember crew)
    {
        if (crew == null)
            return;

        if (debugLog)
        {
            string beforeStation = crew.AssignedStation?.stationId ?? "null";
            Debug.Log($"[CrewManager] UnassignCrew: {crew.displayName}, AssignedStation before = {beforeStation}");
            FileLogger.Log($"UnassignCrew: {crew.displayName}, AssignedStation before = {beforeStation}", "CrewManager");
        }

        RemoveFromCurrentStation(crew);
        
        if (debugLog)
        {
            string afterStation = crew.AssignedStation?.stationId ?? "null";
            Debug.Log($"[CrewManager] UnassignCrew: {crew.displayName}, AssignedStation after = {afterStation}");
            FileLogger.Log($"UnassignCrew: {crew.displayName}, AssignedStation after = {afterStation}", "CrewManager");
        }
        
        // Force immediate save to prevent race conditions with UI refresh
        CrewPersistenceManager.Instance.UpdateCrewAssignment(crew.crewId, string.Empty, forceSave: true);
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

    static CrewStation[] GetAllStationsIncludingInactive()
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindObjectsByType<CrewStation>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        return UnityEngine.Object.FindObjectsOfType<CrewStation>(true);
#endif
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

    void LoadCrewPortraits()
    {
        _portraitLookup.Clear();
        
        // Load all sprites from the UI/Crew folder in Resources
        Sprite[] portraits = Resources.LoadAll<Sprite>(CrewPortraitsPath);
        
        if (debugLog)
        {
            Debug.Log($"[CrewManager] Loaded {portraits.Length} crew portraits from Resources/{CrewPortraitsPath}");
            FileLogger.Log($"Loaded {portraits.Length} crew portraits from Resources/{CrewPortraitsPath}", "CrewManager");
        }
        
        foreach (Sprite sprite in portraits)
        {
            if (sprite == null || string.IsNullOrEmpty(sprite.name))
                continue;
            
            // The sprite name should match the crewId (e.g., "crew_ryn_calder")
            _portraitLookup[sprite.name] = sprite;
            
            if (debugLog)
            {
                Debug.Log($"[CrewManager] Registered portrait: {sprite.name}");
                FileLogger.Log($"Registered portrait: {sprite.name}", "CrewManager");
            }
        }
    }

    public Sprite GetPortraitForCrew(CrewMember crew)
    {
        if (crew == null || string.IsNullOrEmpty(crew.crewId))
            return null;

        if (_portraitLookup.TryGetValue(crew.crewId, out Sprite portrait))
            return portrait;

        return null;
    }
}
