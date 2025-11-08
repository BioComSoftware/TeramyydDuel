using UnityEngine;

public class Weapon : MonoBehaviour
{
    public string weaponType = "cannon";
    public int damage = 25;
    public float range = 100f;
    public float fireRate = 1f;
    
    // Reference to the mount this weapon is attached to
    private WeaponMount currentMount;
    
    public void SetMount(WeaponMount mount)
    {
        currentMount = mount;
    }
    
    public WeaponMount GetMount()
    {
        return currentMount;
    }
    
    // Virtual method for weapon firing - override in specific weapon types
    public virtual void Fire()
    {
        Debug.Log($"Weapon {gameObject.name} fired!");
    }
}