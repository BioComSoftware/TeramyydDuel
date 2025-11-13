using System.IO;
using UnityEngine;

// Minimal JSON save/load helper for PlayerProfile.
public static class SaveSystem
{
    public static string GetProfilePath(string captainId)
    {
        var safe = string.IsNullOrEmpty(captainId) ? "default" : captainId;
        return Path.Combine(Application.persistentDataPath, $"profile_{safe}.json");
    }

    public static void SaveProfile(PlayerProfile profile)
    {
        if (profile == null)
        {
            Debug.LogWarning("SaveSystem.SaveProfile: profile is null");
            return;
        }
        string path = GetProfilePath(profile.captainId);
        string json = JsonUtility.ToJson(profile, prettyPrint: true);
        try
        {
            File.WriteAllText(path, json);
            Debug.Log($"SaveSystem: Saved profile to {path}");
        }
        catch (IOException ex)
        {
            Debug.LogError($"SaveSystem: Failed to save profile: {ex.Message}");
        }
    }

    public static PlayerProfile LoadProfile(string captainId)
    {
        string path = GetProfilePath(captainId);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"SaveSystem: No profile at {path}; creating new.");
            return PlayerProfile.CreateNew(captainId);
        }
        try
        {
            string json = File.ReadAllText(path);
            var profile = JsonUtility.FromJson<PlayerProfile>(json);
            profile.captainId = string.IsNullOrEmpty(profile.captainId) ? captainId : profile.captainId;
            return profile;
        }
        catch (IOException ex)
        {
            Debug.LogError($"SaveSystem: Failed to load profile: {ex.Message}");
            return PlayerProfile.CreateNew(captainId);
        }
    }
}

