using UnityEngine;

[CreateAssetMenu(menuName = "Teramyyd/Items/Weapon Definition", fileName = "WeaponDefinition")]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea] public string description;

    [Header("Economy")] 
    public int cost = 0;

    [Header("Prefab")] 
    public GameObject weaponPrefab; // the in-game weapon instance to mount
    public string weaponType = "cannon"; // matches WeaponMount.mountType acceptance

    [Header("Stats")]
    public int damage = 25;
    public float fireRate = 1f; // shots per second or cooldown proxy
    public float crewSkillRequired = 0f; // min recommended skill (0..1)
}

