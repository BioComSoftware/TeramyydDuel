using UnityEngine;

// Specialized mount for ProjectileLauncher-based weapons (e.g., Cannon).
// Designer setup:
// - Place this component at the desired mount location on the Ship.
// - Provide two pivots:
//     yawBase    (rotates around local Y for left/right sweep)
//     pitchBarrel(rotates around local X for up/down elevation)
// - Set yaw/pitch limits in degrees.
// Runtime usage:
// - Call Mount(prefab) to place a ProjectileLauncher under pitchBarrel (local zeroed).
// - Drive aiming with SetYawPitch/ApplyYawDelta/ApplyPitchDelta (game logic controls movement).
public class ProjectileLauncherMount : MonoBehaviour
{
    [Header("Identity")]
    public string mountId = "Mount_01";
    [Tooltip("Accepted launcher type (e.g., 'cannon'). Informational for now.")]
    public string acceptedType = "cannon";

    [Header("Pivots")]
    [Tooltip("Yaw pivot (left/right), rotates around local Y.")]
    public Transform yawBase;
    [Tooltip("Pitch pivot (up/down), rotates around local X. The launcher is parented here.")]
    public Transform pitchBarrel;

    [Header("Limits (degrees)")]
    [Tooltip("Total left+right arc. Current yaw is clamped to ±(yawLimitDeg/2) around mount forward.")]
    public float yawLimitDeg = 45f;
    [Tooltip("Maximum degrees the barrel can elevate above center.")]
    public float pitchUpDeg = 15f;
    [Tooltip("Maximum degrees the barrel can depress below center.")]
    public float pitchDownDeg = 15f;

    [Header("State")] 
    public bool isOccupied;
    public ProjectileLauncher currentLauncher;
    public GameObject currentObject;

    [Header("Testing (optional)")]
    [Tooltip("If set, Start() will mount this prefab for quick testing.")]
    public GameObject autoPopulatePrefab;
    public bool autoPopulateOnStart = false;

    float _yaw;   // signed degrees, left(-)/right(+)
    float _pitch; // signed degrees, up(+)/down(-)

    void Reset()
    {
        if (yawBase == null) yawBase = transform;
        if (pitchBarrel == null)
        {
            var go = new GameObject("PitchBarrel");
            go.transform.SetParent(yawBase != null ? yawBase : transform, false);
            pitchBarrel = go.transform;
        }
    }

    void Start()
    {
        if (autoPopulateOnStart && autoPopulatePrefab != null && !isOccupied)
        {
            Mount(autoPopulatePrefab);
        }
        ApplyRotations();
    }

    public bool Mount(GameObject prefab)
    {
        if (isOccupied || prefab == null || pitchBarrel == null) return false;
        var go = Instantiate(prefab, pitchBarrel);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;

        // Align the launcher's firing axis to the mount's forward (+Z).
        // Current launchers fire along spawnPoint.up (local +Y) by convention.
        // Compute a world-space rotation that maps the launcher axis to pitchBarrel.forward.
        currentLauncher = go.GetComponent<ProjectileLauncher>();
        Transform axisT = (currentLauncher != null && currentLauncher.spawnPoint != null)
            ? currentLauncher.spawnPoint : go.transform;
        Vector3 fromWorld = axisT.up;            // launcher firing axis (+Y) in world
        Vector3 toWorld   = pitchBarrel.forward; // desired world direction
        if (fromWorld.sqrMagnitude > 1e-6f && toWorld.sqrMagnitude > 1e-6f)
        {
            Quaternion delta = Quaternion.FromToRotation(fromWorld, toWorld);
            go.transform.rotation = delta * go.transform.rotation;
        }

        currentObject = go;
        isOccupied = true;
        return true;
    }

    public GameObject Unmount()
    {
        if (!isOccupied) return null;
        var obj = currentObject;
        currentObject = null;
        currentLauncher = null;
        isOccupied = false;
        if (obj != null) Destroy(obj);
        return null;
    }

    public void SetYawPitch(float yawDeg, float pitchDeg)
    {
        float halfYaw = Mathf.Max(0f, yawLimitDeg * 0.5f);
        _yaw = Mathf.Clamp(yawDeg, -halfYaw, halfYaw);
        _pitch = Mathf.Clamp(pitchDeg, -Mathf.Abs(pitchDownDeg), Mathf.Abs(pitchUpDeg));
        ApplyRotations();
    }

    public void ApplyYawDelta(float deltaDeg)
    {
        SetYawPitch(_yaw + deltaDeg, _pitch);
    }

    public void ApplyPitchDelta(float deltaDeg)
    {
        SetYawPitch(_yaw, _pitch + deltaDeg);
    }

    public (float yawDeg, float pitchDeg) GetYawPitch()
    {
        return (_yaw, _pitch);
    }

    void ApplyRotations()
    {
        if (yawBase != null)
            yawBase.localRotation = Quaternion.Euler(0f, _yaw, 0f);
        if (pitchBarrel != null)
            pitchBarrel.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }
}
