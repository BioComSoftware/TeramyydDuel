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
        string msg = $"[CrewManager] Start: Found {stations.Length} CrewStation components in scene";
        Debug.Log(msg);
        FileLogger.Log(msg, "CrewManager");
        
        foreach (var station in stations)
        {
            if (station != null && station.gameObject.activeInHierarchy)
            {
                RegisterStation(station);
            }
        }
        
        string msg2 = $"[CrewManager] Start: Registered {_stationsById.Count} stations";
        Debug.Log(msg2);
        FileLogger.Log(msg2, "CrewManager");
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
            Debug.LogWarning("[CrewManager] RegisterStation: station is null");
            return;
        }

        station.EnsureStationId();
        
        string msg = $"[CrewManager] RegisterStation: {station.stationId} ({station.displayName}) on GameObject '{station.gameObject.name}'";
        Debug.Log(msg);
        FileLogger.Log(msg, "CrewManager");
        
        // Warn if we're overwriting an existing station with the same ID
        if (_stationsById.ContainsKey(station.stationId) && _stationsById[station.stationId] != station)
        {
            string warnMsg = $"[CrewManager] WARNING: Station ID '{station.stationId}' is already registered to '{_stationsById[station.stationId].gameObject.name}'. Overwriting with '{station.gameObject.name}'. Ensure all stations have unique IDs!";
            Debug.LogWarning(warnMsg);
            FileLogger.Log(warnMsg, "CrewManager");
        }
        
        _stationsById[station.stationId] = station;

        if (_pendingByStation.TryGetValue(station.stationId, out var pendingList))
        {
            string pendingMsg = $"[CrewManager] RegisterStation: Processing {pendingList.Count} pending assignments for {station.stationId}";
            Debug.Log(pendingMsg);
            FileLogger.Log(pendingMsg, "CrewManager");
            
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

        string msg1 = $"[CrewManager] TryAssignCrewToStation: {crew.displayName} -> {station.stationId}";
        Debug.Log(msg1);
        FileLogger.Log(msg1, "CrewManager");

        if (!station.CanAssign(crew))
        {
            string msg2 = $"[CrewManager] TryAssignCrewToStation: Station {station.stationId} cannot assign {crew.displayName}, queuing";
            Debug.LogWarning(msg2);
            FileLogger.Log(msg2, "CrewManager");
            QueuePendingAssignment(crew, station.stationId);
            return false;
        }

        string beforeStation = crew.AssignedStation?.stationId ?? "null";
        string msg3 = $"[CrewManager] TryAssignCrewToStation: Before assignment - crew.AssignedStation = {beforeStation}";
        Debug.Log(msg3);
        FileLogger.Log(msg3, "CrewManager");

        RemoveFromCurrentStation(crew);
        station.AddCrewInternal(crew);
        _pendingCrewTargets.Remove(crew);
        
        string afterStation = crew.AssignedStation?.stationId ?? "null";
        string msg4 = $"[CrewManager] TryAssignCrewToStation: After AddCrewInternal - crew.AssignedStation = {afterStation}";
        Debug.Log(msg4);
        FileLogger.Log(msg4, "CrewManager");
        
        CrewPersistenceManager.Instance.UpdateCrewAssignment(crew.crewId, station.stationId);
        
        string msg5 = $"[CrewManager] TryAssignCrewToStation: SUCCESS - {crew.displayName} assigned to {station.stationId}";
        Debug.Log(msg5);
        FileLogger.Log(msg5, "CrewManager");
        return true;
    }

    public bool TryAssignCrewToStationId(CrewMember crew, string stationId)
    {
        if (crew == null || string.IsNullOrEmpty(stationId))
        {
            Debug.LogWarning($"[CrewManager] TryAssignCrewToStationId: crew or stationId is null/empty");
            return false;
        }

        string msg = $"[CrewManager] TryAssignCrewToStationId: {crew.displayName} -> {stationId}";
        Debug.Log(msg);
        FileLogger.Log(msg, "CrewManager");

        CrewStation station = null;
        if (_stationsById.TryGetValue(stationId, out station))
        {
            bool result = TryAssignCrewToStation(crew, station);
            string resultMsg = $"[CrewManager] TryAssignCrewToStationId: Result = {result}, crew.AssignedStation = {crew.AssignedStation?.stationId ?? "null"}";
            Debug.Log(resultMsg);
            FileLogger.Log(resultMsg, "CrewManager");
            return result;
        }

        // Station not registered, try to find it in the scene
        string searchMsg = $"[CrewManager] Station {stationId} not registered, searching scene...";
        Debug.LogWarning(searchMsg);
        FileLogger.Log(searchMsg, "CrewManager");
        
        var allStations = GetAllStationsIncludingInactive();
        foreach (var st in allStations)
        {
            if (st.stationId == stationId)
            {
                string foundMsg = $"[CrewManager] Found station {stationId} in scene, registering it now";
                Debug.Log(foundMsg);
                FileLogger.Log(foundMsg, "CrewManager");
                RegisterStation(st);
                
                bool result = TryAssignCrewToStation(crew, st);
                string resultMsg = $"[CrewManager] TryAssignCrewToStationId: Result = {result}, crew.AssignedStation = {crew.AssignedStation?.stationId ?? "null"}";
                Debug.Log(resultMsg);
                FileLogger.Log(resultMsg, "CrewManager");
                return result;
            }
        }

        string queueMsg = $"[CrewManager] Station {stationId} not found anywhere, queuing pending assignment for {crew.displayName}";
        Debug.LogWarning(queueMsg);
        FileLogger.Log(queueMsg, "CrewManager");
        QueuePendingAssignment(crew, stationId);
        return false;
    }

    public void UnassignCrew(CrewMember crew)
    {
        if (crew == null)
            return;

        string beforeStation = crew.AssignedStation?.stationId ?? "null";
        string msg = $"[CrewManager] UnassignCrew: {crew.displayName}, AssignedStation before = {beforeStation}";
        Debug.Log(msg);
        FileLogger.Log(msg, "CrewManager");

        RemoveFromCurrentStation(crew);
        
        string afterStation = crew.AssignedStation?.stationId ?? "null";
        string msg2 = $"[CrewManager] UnassignCrew: {crew.displayName}, AssignedStation after = {afterStation}";
        Debug.Log(msg2);
        FileLogger.Log(msg2, "CrewManager");
        
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
        
        string msg = $"[CrewManager] Loaded {portraits.Length} crew portraits from Resources/{CrewPortraitsPath}";
        Debug.Log(msg);
        FileLogger.Log(msg, "CrewManager");
        
        foreach (Sprite sprite in portraits)
        {
            if (sprite == null || string.IsNullOrEmpty(sprite.name))
                continue;
            
            // The sprite name should match the crewId (e.g., "crew_ryn_calder")
            _portraitLookup[sprite.name] = sprite;
            
            string spriteMsg = $"[CrewManager] Registered portrait: {sprite.name}";
            Debug.Log(spriteMsg);
            FileLogger.Log(spriteMsg, "CrewManager");
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
