using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class CrewMemberState
{
    public string crewId;
    public string displayName;
    public float gunnery = 1f;
    public float navigation = 1f;
    public float repair = 1f;
    public float powerEngineering = 1f;
    public float liftEngineering = 1f;
    public float maxHealth;
    public float currentHealth;
    public string assignedStationId;
}

[Serializable]
public class CrewPersistenceSnapshot
{
    public string version = "1.0.0";
    public string lastSavedUtc = string.Empty;
    public List<CrewMemberState> crewMembers = new List<CrewMemberState>();
}

public class CrewPersistenceManager : MonoBehaviour
{
    private static CrewPersistenceManager _instance;
    public static CrewPersistenceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<CrewPersistenceManager>();
                if (_instance == null)
                {
                    var go = new GameObject("CrewPersistenceManager");
                    _instance = go.AddComponent<CrewPersistenceManager>();
                }
            }
            return _instance;
        }
    }

    public CrewPersistenceSnapshot CurrentSnapshot
    {
        get
        {
            InitializeIfNeeded();
            return _snapshot;
        }
    }

    public IReadOnlyList<CrewMemberState> CrewStates
    {
        get
        {
            InitializeIfNeeded();
            return _snapshot?.crewMembers;
        }
    }

    [Header("Persistence")]
    public string resourceFileName = "CrewPersistence";
    public float saveIntervalSeconds = 30f;
    public bool autoSaveEnabled = true;
    
    [Header("Debug")]
    [Tooltip("Enable debug logging to Console and log file")]
    public bool debugLog = false;

    readonly Dictionary<string, CrewMemberState> _crewLookup = new Dictionary<string, CrewMemberState>();
    readonly Dictionary<Health, UnityAction<float>> _healthChangeHandlers = new Dictionary<Health, UnityAction<float>>();
    readonly Dictionary<Health, UnityAction> _deathHandlers = new Dictionary<Health, UnityAction>();

    CrewPersistenceSnapshot _snapshot = new CrewPersistenceSnapshot();
    float _nextSaveTime;
    bool _dirty;
    bool _initialized;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeIfNeeded();
    }

    void InitializeIfNeeded()
    {
        if (_initialized)
            return;

        LoadSnapshot();
        _nextSaveTime = Time.unscaledTime + saveIntervalSeconds;
        _initialized = true;
    }

    void Update()
    {
        if (!_initialized || !autoSaveEnabled || !_dirty)
            return;

        if (Time.unscaledTime >= _nextSaveTime)
        {
            SaveSnapshot();
            _nextSaveTime = Time.unscaledTime + saveIntervalSeconds;
        }
    }

    void OnApplicationQuit()
    {
        if (_dirty)
        {
            SaveSnapshot();
        }
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public CrewMemberState RegisterCrewMember(CrewMember crew)
    {
        if (crew == null)
            return null;

        InitializeIfNeeded();

        var state = GetOrCreateState(crew);
        ApplySkillState(crew, state);
        ApplyHealthState(crew, state);
        TrackHealth(crew.Health, state);
        return state;
    }

    public void UnregisterCrewMember(CrewMember crew)
    {
        if (crew == null)
            return;

        if (crew.Health != null && _healthChangeHandlers.TryGetValue(crew.Health, out var changeHandler))
        {
            crew.Health.onHealthChanged.RemoveListener(changeHandler);
            _healthChangeHandlers.Remove(crew.Health);
        }

        if (crew.Health != null && _deathHandlers.TryGetValue(crew.Health, out var deathHandler))
        {
            crew.Health.onDeath.RemoveListener(deathHandler);
            _deathHandlers.Remove(crew.Health);
        }
    }

    public void UpdateCrewAssignment(string crewId, string stationId, bool forceSave = false)
    {
        if (string.IsNullOrEmpty(crewId))
            return;

        if (_crewLookup.TryGetValue(crewId, out var state))
        {
            string newValue = string.IsNullOrEmpty(stationId) ? string.Empty : stationId;
            if (state.assignedStationId != newValue)
            {
                string oldStation = string.IsNullOrEmpty(state.assignedStationId) ? "UNASSIGNED" : state.assignedStationId;
                string newStation = string.IsNullOrEmpty(newValue) ? "UNASSIGNED" : newValue;
                
                state.assignedStationId = newValue;
                MarkDirty();
                
                if (debugLog)
                {
                    string msg = $"[CrewPersistence] UpdateCrewAssignment: {crewId} moved from {oldStation} to {newStation} (forceSave={forceSave})";
                    Debug.Log(msg);
                    FileLogger.Log(msg, "CrewPersistence");
                }
                
                // Immediately save when force flag is set to prevent race conditions
                if (forceSave)
                {
                    SaveSnapshot();
                }
            }
        }
        else if (debugLog)
        {
            string msg = $"[CrewPersistence] UpdateCrewAssignment: WARNING - crew {crewId} not found in lookup!";
            Debug.LogWarning(msg);
            FileLogger.Log(msg, "CrewPersistence");
        }
    }

    CrewMemberState GetOrCreateState(CrewMember crew)
    {
        if (!_crewLookup.TryGetValue(crew.crewId, out var state))
        {
            state = new CrewMemberState
            {
                crewId = crew.crewId,
                displayName = crew.displayName,
                gunnery = crew.gunnery,
                navigation = crew.navigation,
                repair = crew.repair,
                powerEngineering = crew.powerEngineering,
                liftEngineering = crew.liftEngineering,
                maxHealth = crew.Health != null ? crew.Health.maxHealth : 100f,
                currentHealth = crew.Health != null ? crew.Health.currentHealth : 100f,
                assignedStationId = crew.initialStationId
            };
            _snapshot.crewMembers.Add(state);
            _crewLookup[crew.crewId] = state;
            MarkDirty();
        }
        else
        {
            // State already exists from JSON - JSON is authoritative
            // Do NOT overwrite state with prefab values
            // Only update displayName in case it was changed in the prefab
            if (!string.IsNullOrEmpty(crew.displayName))
            {
                state.displayName = crew.displayName;
            }
        }

        return state;
    }

    void ApplySkillState(CrewMember crew, CrewMemberState state)
    {
        if (crew == null || state == null)
            return;

        if (debugLog)
        {
            string msg = $"[CrewPersistence] ApplySkillState to {crew.crewId}: Before=(G:{crew.gunnery:F1} N:{crew.navigation:F1}), JSON=(G:{state.gunnery:F1} N:{state.navigation:F1})";
            Debug.Log(msg);
            FileLogger.Log(msg, "CrewPersistence");
        }

        crew.SetSkillLevel(CrewSkill.Gunnery, Mathf.Max(1f, state.gunnery));
        crew.SetSkillLevel(CrewSkill.Navigation, Mathf.Max(1f, state.navigation));
        crew.SetSkillLevel(CrewSkill.Repair, Mathf.Max(1f, state.repair));
        crew.SetSkillLevel(CrewSkill.PowerEngineering, Mathf.Max(1f, state.powerEngineering));
        crew.SetSkillLevel(CrewSkill.LiftEngineering, Mathf.Max(1f, state.liftEngineering));
        
        if (debugLog)
        {
            string msg = $"[CrewPersistence] ApplySkillState to {crew.crewId}: After=(G:{crew.gunnery:F1} N:{crew.navigation:F1})";
            Debug.Log(msg);
            FileLogger.Log(msg, "CrewPersistence");
        }
    }

    void ApplyHealthState(CrewMember crew, CrewMemberState state)
    {
        if (crew == null || crew.Health == null || state == null)
            return;

        float healthToApply = Mathf.Clamp(state.currentHealth, 0f, crew.Health.maxHealth);
        crew.Health.SetHealth(healthToApply);
    }

    void TrackHealth(Health health, CrewMemberState state)
    {
        if (health == null || state == null)
            return;

        if (_healthChangeHandlers.TryGetValue(health, out var existingChange))
        {
            health.onHealthChanged.RemoveListener(existingChange);
        }

        if (_deathHandlers.TryGetValue(health, out var existingDeath))
        {
            health.onDeath.RemoveListener(existingDeath);
        }

        UnityAction<float> changeHandler = value =>
        {
            state.currentHealth = Mathf.Clamp(value, 0f, state.maxHealth);
            MarkDirty();
        };

        UnityAction deathHandler = () =>
        {
            state.currentHealth = 0f;
            MarkDirty();
        };

        health.onHealthChanged.AddListener(changeHandler);
        health.onDeath.AddListener(deathHandler);
        _healthChangeHandlers[health] = changeHandler;
        _deathHandlers[health] = deathHandler;
    }

    void LoadSnapshot()
    {
        string json = null;
        string diskPath = GetResourceDiskPath();
        
        if (debugLog)
        {
            string msg = $"[CrewPersistence] LoadSnapshot: Attempting to load from {diskPath}";
            Debug.Log(msg);
            FileLogger.Log(msg, "CrewPersistence");
        }

        if (File.Exists(diskPath))
        {
            json = File.ReadAllText(diskPath);
        }
        else
        {
            TextAsset asset = Resources.Load<TextAsset>(resourceFileName);
            if (asset != null)
            {
                json = asset.text;
            }
        }

        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                _snapshot = JsonUtility.FromJson<CrewPersistenceSnapshot>(json);
                
                if (debugLog)
                {
                    string msg = $"[CrewPersistence] LoadSnapshot: Successfully loaded {_snapshot.crewMembers.Count} crew members from JSON";
                    Debug.Log(msg);
                    FileLogger.Log(msg, "CrewPersistence");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CrewPersistenceManager: Failed to parse {resourceFileName}.json ({ex.Message}). Creating new snapshot.");
                _snapshot = new CrewPersistenceSnapshot();
            }
        }
        else
        {
            _snapshot = new CrewPersistenceSnapshot();
        }

        _crewLookup.Clear();
        
        // Build lookup and detect duplicates
        var uniqueCrewMembers = new List<CrewMemberState>();
        foreach (var entry in _snapshot.crewMembers)
        {
            if (entry == null || string.IsNullOrEmpty(entry.crewId))
                continue;

            if (!_crewLookup.ContainsKey(entry.crewId))
            {
                _crewLookup.Add(entry.crewId, entry);
                uniqueCrewMembers.Add(entry);
                
                if (debugLog)
                {
                    string station = string.IsNullOrEmpty(entry.assignedStationId) ? "UNASSIGNED" : entry.assignedStationId;
                    string msg = $"[CrewPersistence] Loaded crew: {entry.crewId} ({entry.displayName}) -> Station: {station}";
                    Debug.Log(msg);
                    FileLogger.Log(msg, "CrewPersistence");
                }
            }
            else
            {
                // Duplicate detected - log warning and skip
                string msg = $"[CrewPersistence] WARNING: Duplicate crew ID '{entry.crewId}' found in JSON and removed";
                Debug.LogWarning(msg);
                FileLogger.Log(msg, "CrewPersistence");
            }
        }
        
        // Replace the list with deduplicated version
        if (uniqueCrewMembers.Count != _snapshot.crewMembers.Count)
        {
            _snapshot.crewMembers = uniqueCrewMembers;
            MarkDirty(); // Save cleaned-up version
            string msg = $"[CrewPersistence] Removed {_snapshot.crewMembers.Count - uniqueCrewMembers.Count} duplicate entries";
            Debug.Log(msg);
            FileLogger.Log(msg, "CrewPersistence");
        }
    }

    public void SaveSnapshot()
    {
        if (_snapshot == null)
            return;

        _snapshot.lastSavedUtc = DateTime.UtcNow.ToString("O");
        string json = JsonUtility.ToJson(_snapshot, true);
        string path = GetResourceDiskPath();
        
        if (debugLog)
        {
            string msg = $"[CrewPersistence] SaveSnapshot: Saving {_snapshot.crewMembers.Count} crew members to {path}";
            Debug.Log(msg);
            FileLogger.Log(msg, "CrewPersistence");
            
            foreach (var entry in _snapshot.crewMembers)
            {
                string station = string.IsNullOrEmpty(entry.assignedStationId) ? "UNASSIGNED" : entry.assignedStationId;
                string msg2 = $"[CrewPersistence]   - {entry.crewId} ({entry.displayName}) -> Station: {station}";
                Debug.Log(msg2);
                FileLogger.Log(msg2, "CrewPersistence");
            }
        }
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(path, json);
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
        _dirty = false;
    }

    public void UpdateCrewSkills(CrewMember crew)
    {
        if (crew == null)
            return;

        InitializeIfNeeded();

        var state = GetOrCreateState(crew);
        state.gunnery = crew.gunnery;
        state.navigation = crew.navigation;
        state.repair = crew.repair;
        state.powerEngineering = crew.powerEngineering;
        state.liftEngineering = crew.liftEngineering;
        MarkDirty();
    }

    string GetResourceDiskPath()
    {
#if UNITY_EDITOR
        string assetPath = $"Assets/Resources/{resourceFileName}.json";
        string fullPath = Path.Combine(Application.dataPath, "Resources", resourceFileName + ".json");
        if (!Directory.Exists(Path.Combine(Application.dataPath, "Resources")))
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources"));
        }
        return fullPath;
#else
        return Path.Combine(Application.persistentDataPath, resourceFileName + ".json");
#endif
    }

    void MarkDirty()
    {
        _dirty = true;
    }
}
