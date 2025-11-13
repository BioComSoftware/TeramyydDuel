using UnityEngine;

[CreateAssetMenu(menuName = "Teramyyd/Items/Item Definition", fileName = "ItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;              // unique id used in data/profile
    public string displayName;
    [TextArea] public string description;

    [Header("Economy")] 
    public int baseValue = 0;      // gold value
    public bool stackable = true;
    public int maxStack = 99;
}

