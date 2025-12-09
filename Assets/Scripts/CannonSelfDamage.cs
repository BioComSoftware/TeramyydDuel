using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Deprecated wrapper preserved so existing prefabs dont lose references. Cannon now owns self-wear directly.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("")]
public sealed class CannonSelfDamage : MonoBehaviour
{
    void Reset() => RetireComponent();

    void Awake() => RetireComponent();

    void RetireComponent()
    {
        string message = "[CannonSelfDamage] This component is obsolete. Configure damagePerShot on the Cannon instead.";
        Debug.LogWarning(message, this);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.DestroyObjectImmediate(this);
            return;
        }
#endif
        Destroy(this);
    }
}
