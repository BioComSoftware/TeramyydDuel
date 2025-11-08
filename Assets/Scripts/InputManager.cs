using UnityEngine;

// Small wrapper around Unity's Input system to centralize controls — swap to new Input System later if desired
public static class InputManager
{
    public static float GetRoll() => Input.GetAxis("Horizontal");
    public static float GetPitch() => Input.GetAxis("Vertical");
    public static bool IsFiring() => Input.GetButton("Fire1");
}
