using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class MountedCannonState
{
    public string persistenceKey;
    public string mountId;
    public string hierarchyPath;
    public string weaponName;
    public float maxHealth;
    public float currentHealth;
    public float damageTaken;
}

[Serializable]
public class WeaponPersistenceSnapshot
{
    public string version = "1.0.0";
    public string lastSavedUtc = string.Empty;
    public List<MountedCannonState> mountedCannons = new List<MountedCannonState>();
}

/// <summary>
/// Centralized persistence helper for mounted weapons. Loads/saves a JSON snapshot in Assets/Resources
/// and keeps cannon health values in sync so mounts remember their damage between play sessions.
/// </summary>
public class WeaponPersistenceManager : MonoBehaviour
{
    private static WeaponPersistenceManager _instance;
    public static WeaponPersistenceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<WeaponPersistenceManager>();
                if (_instance == null)
                {
                    var go = new GameObject("WeaponPersistenceManager");
                    _instance = go.AddComponent<WeaponPersistenceManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Persistence")]
    [Tooltip("JSON resource name (under Assets/Resources) used to store persistent weapon state.")]
    public string resourceFileName = "GamePersistence";
    [Tooltip("Seconds between automatic save attempts when data is dirty.")]
    public float saveIntervalSeconds = 15f;
    [Tooltip("When disabled, changes are tracked but only saved manually via SaveSnapshot().")]
    public bool autoSaveEnabled = true;

    private WeaponPersistenceSnapshot _snapshot = new WeaponPersistenceSnapshot();
    private readonly Dictionary<string, MountedCannonState> _mountLookup = new Dictionary<string, MountedCannonState>();
    private readonly Dictionary<Health, UnityAction<float>> _healthChangeHandlers = new Dictionary<Health, UnityAction<float>>();
    private readonly Dictionary<Health, UnityAction> _deathHandlers = new Dictionary<Health, UnityAction>();

    private float _nextSaveTime;
    private bool _dirty;
    private bool _initialized;

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
        if (!_initialized)
            return;

        if (!autoSaveEnabled || !_dirty)
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

    public void RegisterMountedWeapon(WeaponMount mount)
    {
        if (mount == null)
            return;

        InitializeIfNeeded();

        string persistenceKey = BuildPersistenceKey(mount);
        string mountName = string.IsNullOrEmpty(mount.mountId) ? "(unnamed)" : mount.mountId;
        string hierarchyPath = GetHierarchyPath(mount.transform);

        Health health = mount.MountedWeaponHealth;
        string weaponName = mount.currentLauncher != null ? mount.currentLauncher.name : mountName;
        float maxHealth = health != null ? health.maxHealth : 0f;

        var state = GetOrCreateState(persistenceKey, mountName, hierarchyPath, weaponName, maxHealth);
        if (health != null)
        {
            if (state.currentHealth > 0f)
            {
                float clamped = Mathf.Clamp(state.currentHealth, 0f, health.maxHealth);
                health.SetHealth(clamped);
            }
            else
            {
                health.SetHealth(0f);
            }

            TrackHealth(mount.mountId, health, state);
        }
    }

    public void UnregisterMountedWeapon(WeaponMount mount)
    {
        if (mount == null)
            return;

        Health health = mount.MountedWeaponHealth;
        if (health == null)
            return;

        if (_healthChangeHandlers.TryGetValue(health, out var changeHandler))
        {
            health.onHealthChanged.RemoveListener(changeHandler);
            _healthChangeHandlers.Remove(health);
        }

        if (_deathHandlers.TryGetValue(health, out var deathHandler))
        {
            health.onDeath.RemoveListener(deathHandler);
            _deathHandlers.Remove(health);
        }
    }

    MountedCannonState GetOrCreateState(string persistenceKey, string mountName, string hierarchyPath, string weaponName, float maxHealth)
    {
        if (string.IsNullOrEmpty(persistenceKey))
        {
            persistenceKey = Guid.NewGuid().ToString();
        }

        if (!_mountLookup.TryGetValue(persistenceKey, out var state))
        {
            state = new MountedCannonState
            {
                persistenceKey = persistenceKey,
                mountId = mountName,
                hierarchyPath = hierarchyPath,
                weaponName = weaponName,
                maxHealth = Mathf.Max(1f, maxHealth),
                currentHealth = Mathf.Max(1f, maxHealth),
                damageTaken = 0
            };
            _snapshot.mountedCannons.Add(state);
            _mountLookup[persistenceKey] = state;
            _dirty = true;
        }
        else
        {
            if (state.persistenceKey != persistenceKey)
            {
                _mountLookup.Remove(state.persistenceKey);
                state.persistenceKey = persistenceKey;
                _mountLookup[persistenceKey] = state;
            }
            if (!string.IsNullOrEmpty(mountName))
            {
                state.mountId = mountName;
            }
            if (!string.IsNullOrEmpty(hierarchyPath))
            {
                state.hierarchyPath = hierarchyPath;
            }
            if (!string.IsNullOrEmpty(weaponName) && state.weaponName != weaponName)
            {
                state.weaponName = weaponName;
                if (maxHealth > 0)
                {
                    state.maxHealth = maxHealth;
                    state.currentHealth = maxHealth;
                    state.damageTaken = 0;
                }
                _dirty = true;
            }
            else if (maxHealth > 0f && !Mathf.Approximately(state.maxHealth, maxHealth))
            {
                state.maxHealth = maxHealth;
                state.currentHealth = Mathf.Min(state.currentHealth, state.maxHealth);
                state.damageTaken = Mathf.Max(0f, state.maxHealth - state.currentHealth);
                _dirty = true;
            }
        }

        return state;
    }

    void TrackHealth(string mountId, Health health, MountedCannonState state)
    {
        if (health == null || state == null)
            return;

        if (_healthChangeHandlers.TryGetValue(health, out var existing))
        {
            health.onHealthChanged.RemoveListener(existing);
        }
        if (_deathHandlers.TryGetValue(health, out var existingDeath))
        {
            health.onDeath.RemoveListener(existingDeath);
        }

        UnityAction<float> changeHandler = value => UpdateStateFromHealth(state, health, value);
        UnityAction deathHandler = () => UpdateStateFromHealth(state, health, 0f);
        health.onHealthChanged.AddListener(changeHandler);
        health.onDeath.AddListener(deathHandler);
        _healthChangeHandlers[health] = changeHandler;
        _deathHandlers[health] = deathHandler;
    }

    void UpdateStateFromHealth(MountedCannonState state, Health health, float newValue)
    {
        if (state == null || health == null)
            return;

        state.currentHealth = Mathf.Clamp(newValue, 0f, health.maxHealth);
        state.maxHealth = health.maxHealth;
        state.damageTaken = Mathf.Max(0f, state.maxHealth - state.currentHealth);
        _dirty = true;
    }

    void LoadSnapshot()
    {
        string json = null;
        string diskPath = GetResourceDiskPath();

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
                _snapshot = JsonUtility.FromJson<WeaponPersistenceSnapshot>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"WeaponPersistenceManager: Failed to parse {resourceFileName}.json. {ex.Message} Creating new snapshot.");
                _snapshot = new WeaponPersistenceSnapshot();
            }
        }
        else
        {
            _snapshot = new WeaponPersistenceSnapshot();
        }

        _mountLookup.Clear();
        if (_snapshot.mountedCannons == null)
        {
            _snapshot.mountedCannons = new List<MountedCannonState>();
        }
        foreach (var entry in _snapshot.mountedCannons)
        {
            string key = !string.IsNullOrEmpty(entry.persistenceKey) ? entry.persistenceKey : entry.mountId;
            if (string.IsNullOrEmpty(key))
            {
                key = Guid.NewGuid().ToString();
                entry.persistenceKey = key;
            }
            if (string.IsNullOrEmpty(entry.hierarchyPath))
            {
                entry.hierarchyPath = entry.mountId;
            }
            _mountLookup[key] = entry;
        }
    }

    void SaveSnapshot()
    {
        if (_snapshot == null)
        {
            _snapshot = new WeaponPersistenceSnapshot();
        }

        _snapshot.lastSavedUtc = DateTime.UtcNow.ToString("o");
        foreach (var entry in _snapshot.mountedCannons)
        {
            entry.damageTaken = (float)Math.Round(Mathf.Max(0f, entry.maxHealth - entry.currentHealth), 2, MidpointRounding.AwayFromZero);
            entry.currentHealth = (float)Math.Round(entry.currentHealth, 2, MidpointRounding.AwayFromZero);
            entry.maxHealth = (float)Math.Round(entry.maxHealth, 2, MidpointRounding.AwayFromZero);
        }

        string json = JsonUtility.ToJson(_snapshot, true);
        string diskPath = GetResourceDiskPath();
        File.WriteAllText(diskPath, json);
#if UNITY_EDITOR
    AssetDatabase.Refresh();
#endif
        _dirty = false;
    }

    string GetResourceDiskPath()
    {
        string resourceDir = Path.Combine(Application.dataPath, "Resources");
        if (!Directory.Exists(resourceDir))
        {
            Directory.CreateDirectory(resourceDir);
        }
        return Path.Combine(resourceDir, resourceFileName + ".json");
    }

    string BuildPersistenceKey(WeaponMount mount)
    {
        string namePart = string.IsNullOrEmpty(mount.mountId) ? "(unnamed)" : mount.mountId;
        string pathPart = GetHierarchyPath(mount.transform);
        return string.IsNullOrEmpty(pathPart) ? namePart : $"{namePart}|{pathPart}";
    }

    string GetHierarchyPath(Transform t)
    {
        if (t == null)
            return string.Empty;
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
