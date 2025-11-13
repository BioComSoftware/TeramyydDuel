using System;
using System.Collections.Generic;
using UnityEngine;

// Serializable data model for long-term player progression and ship state.
[Serializable]
public class PlayerProfile
{
    public string captainId;
    public int gold;
    public float reputation;

    public List<OwnedShip> ships = new List<OwnedShip>();
    public string activeShipId; // shipDefId of the currently selected ship

    public Inventory inventory = new Inventory();
    public List<CrewMemberState> crew = new List<CrewMemberState>();

    public static PlayerProfile CreateNew(string captainId)
    {
        return new PlayerProfile
        {
            captainId = captainId,
            gold = 0,
            reputation = 0f,
            ships = new List<OwnedShip>(),
            activeShipId = string.Empty,
            inventory = new Inventory(),
            crew = new List<CrewMemberState>()
        };
    }
}

[Serializable]
public class OwnedShip
{
    public string shipDefId;
    public float hullHealth = 100f;
    public List<MountedWeaponState> mounts = new List<MountedWeaponState>();
}

[Serializable]
public class MountedWeaponState
{
    public string mountId;      // logical identifier matching a WeaponMount in scene/prefab
    public string weaponDefId;  // empty or null if unoccupied
}

[Serializable]
public class CrewMemberState
{
    public string crewDefId;
    public float skill01 = 0.5f;   // 0..1
    public string status = "Active"; // Active, Injured, Resting
    public int salaryPerCycle = 0;
}

[Serializable]
public class Inventory
{
    public List<ItemStack> items = new List<ItemStack>();

    public int GetCount(string itemId)
    {
        int total = 0;
        for (int i = 0; i < items.Count; i++)
            if (items[i].itemId == itemId) total += items[i].quantity;
        return total;
    }

    public void Add(string itemId, int amount)
    {
        if (amount <= 0) return;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemId == itemId)
            {
                items[i].quantity += amount;
                return;
            }
        }
        items.Add(new ItemStack { itemId = itemId, quantity = amount });
    }

    public bool Remove(string itemId, int amount)
    {
        if (amount <= 0) return true;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemId == itemId)
            {
                if (items[i].quantity < amount) return false;
                items[i].quantity -= amount;
                if (items[i].quantity == 0) items.RemoveAt(i);
                return true;
            }
        }
        return false;
    }
}

[Serializable]
public class ItemStack
{
    public string itemId;
    public int quantity;
}

