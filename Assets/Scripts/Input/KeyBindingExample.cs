using UnityEngine;

// ============================================================================================
// IMPORTANT: THIS IS A REFERENCE/DOCUMENTATION SCRIPT - NOT FOR PRODUCTION USE
// ============================================================================================
//
// PURPOSE:
// This script exists solely as CODE DOCUMENTATION and EXAMPLES for the two-layer keybinding
// system implemented in KeyBindingConfig.cs. It demonstrates the correct API usage patterns.
//
// WHO USES THIS:
// - HUMAN DEVELOPERS: Reference this when implementing settings menus or keybinding features
// - AI AGENTS: Use this to understand the KeyBindingConfig API and provide correct code examples
// - CODE REVIEWERS: Reference to verify proper KeyBindingConfig usage in other scripts
//
// WHEN TO USE THIS:
// - Building a settings/options menu with key rebinding UI
// - Implementing "Reset to Defaults" functionality
// - Need to understand how to read/write keybinding values
// - Teaching others how the two-layer system works
//
// THIS SCRIPT IS NOT:
// - A production component (don't attach to game objects in final build)
// - A settings menu implementation (copy patterns from here to your real menu)
// - Required for KeyBindingConfig to function (it's pure documentation)
//
// TWO-LAYER SYSTEM RECAP:
// LAYER 1 (Inspector Defaults): defaultFireAllWeapons, defaultBridgeView, etc.
//   - Set by developer in Unity Inspector
//   - NEVER change at runtime or when player modifies settings
//   - Used only for "Reset to Defaults" function
//
// LAYER 2 (Runtime/Player Settings): fireAllWeapons, bridgeView, etc. (via properties)
//   - Loaded from keybindings.json at startup
//   - AUTHORITATIVE values used during gameplay
//   - Modified by player through settings menu
//   - Saved back to keybindings.json when changed
//
// USAGE PATTERN:
// 1. Read values: KeyBindingConfig.Instance.fireAllWeapons (gets player's current setting)
// 2. Modify values: SetRuntimeKey("fireAllWeapons", newKey)
// 3. Save changes: SaveToJSONFile()
// 4. Reset all: ResetToDefaults() then SaveToJSONFile()
//
// AI AGENT INSTRUCTIONS:
// When a developer asks about keybinding implementation, reference the patterns in this file.
// Always distinguish between default* fields (Inspector) and property access (runtime).
// Never modify KeyBindingConfig fields directly - always use SetRuntimeKey/Float/Bool methods.
// ============================================================================================

/// <summary>
/// Example demonstrating how to use the two-layer KeyBindingConfig system.
/// This script shows common use cases for developers and settings menus.
/// DO NOT USE IN PRODUCTION - This is documentation/reference code only.
/// </summary>
public class KeyBindingExample : MonoBehaviour
{
    void Start()
    {
        // DURING GAMEPLAY: Read from runtime values (these are the authoritative source)
        KeyCode currentFireKey = KeyBindingConfig.Instance.fireAllWeapons;
        Debug.Log($"Player's fire key is: {currentFireKey}");

        // Check if a specific key is pressed during gameplay
        if (Input.GetKeyDown(KeyBindingConfig.Instance.bridgeView))
        {
            Debug.Log("Player pressed their configured bridge view key!");
        }
    }

    // EXAMPLE: Player wants to rebind the fire key in settings menu
    public void PlayerRebindsFireKey(KeyCode newKey)
    {
        // Update the runtime value
        KeyBindingConfig.Instance.SetRuntimeKey("fireAllWeapons", newKey);
        
        // Save to JSON file to persist the change
        KeyBindingConfig.Instance.SaveToJSONFile();
        
        Debug.Log($"Player rebound fire key to: {newKey}");
    }

    // EXAMPLE: Player wants to change ship wheel auto-return speed
    public void PlayerChangesAutoReturnSpeed(float newSpeed)
    {
        // Update the runtime value
        KeyBindingConfig.Instance.SetRuntimeFloat("autoReturnSpeedDegPerSec", newSpeed);
        
        // Save to JSON file
        KeyBindingConfig.Instance.SaveToJSONFile();
        
        Debug.Log($"Player changed auto-return speed to: {newSpeed}");
    }

    // EXAMPLE: Player clicks "Reset to Defaults" button
    public void PlayerResetsToDefaults()
    {
        // This copies all developer defaults (Inspector values) to runtime values
        string json = KeyBindingConfig.Instance.ResetToDefaults();
        
        // Write to disk
        KeyBindingConfig.Instance.SaveToJSONFile();
        
        Debug.Log("Reset all keybindings to developer defaults");
    }

    // EXAMPLE: Get the developer's default value (for showing in UI)
    public void ShowDefaultValue()
    {
        // Access developer defaults from DefaultKeybindings component (these never change)
        DefaultKeybindings defaults = DefaultKeybindings.Instance;
        if (defaults != null)
        {
            KeyCode defaultFireKey = defaults.defaultFireAllWeapons;
            Debug.Log($"Developer default fire key is: {defaultFireKey}");
            Debug.Log("This value never changes regardless of player settings");
        }
    }

    // EXAMPLE: Check what the player's current setting is vs the default
    public void CompareCurrentToDefault()
    {
        KeyCode playerCurrent = KeyBindingConfig.Instance.fireAllWeapons;
        DefaultKeybindings defaults = DefaultKeybindings.Instance;
        
        if (defaults != null)
        {
            KeyCode developerDefault = defaults.defaultFireAllWeapons;
            
            if (playerCurrent != developerDefault)
            {
                Debug.Log($"Player has customized fire key: {playerCurrent} (default: {developerDefault})");
            }
            else
            {
                Debug.Log("Player is using default fire key");
            }
        }
    }
}
