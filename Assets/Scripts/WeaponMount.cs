using UnityEngine;

public class WeaponMount : MonoBehaviour
{
    // Reference to the currently mounted weapon
    private GameObject mountedWeapon;
    
    // Properties of the mount point
    public string mountType = "cannon";  // Type of weapons this mount can accept
    public bool isOccupied { get; private set; } = false;
    
    // Reference to the weapon's health component if it has one
    private Health weaponHealth;

    // Mount a new weapon
    public bool MountWeapon(GameObject weaponPrefab)
    {
        if (isOccupied || weaponPrefab == null)
            return false;

        // Instantiate the weapon as a child of this mount
        mountedWeapon = Instantiate(weaponPrefab, transform.position, transform.rotation, transform);
        
        // Try to get the health component
        weaponHealth = mountedWeapon.GetComponent<Health>();
        
        isOccupied = true;
        return true;
    }

    // Remove the current weapon
    public GameObject UnmountWeapon()
    {
        if (!isOccupied || mountedWeapon == null)
            return null;

        GameObject weapon = mountedWeapon;
        mountedWeapon = null;
        weaponHealth = null;
        isOccupied = false;
        
        // Detach from parent but don't destroy
        weapon.transform.SetParent(null);
        return weapon;
    }

    // Get the current weapon's health (if it has any)
    public Health GetWeaponHealth()
    {
        return weaponHealth;
    }

    // Check if a weapon type can be mounted here
    public bool CanMountWeaponType(string type)
    {
        return mountType.ToLower() == type.ToLower();
    }
}