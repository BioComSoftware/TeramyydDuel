# Engine Chadburn Keyboard Controls Implementation

## Overview
Added keyboard controls for the engine chadburn (Engine Order Telegraph) using A and S keys to control forward and reverse power.

## Key Bindings

### A Key - Forward Movement Control
- **Hold A:** Chadburn rotates clockwise continuously at `engineChadburnRotationSpeed` (degrees/second)
- **Release A:** Chadburn stops at current position
- **CTRL+A:** Instantly snap chadburn to maximum clockwise position (full ahead)
- **LEFT-SHIFT+A:** Instantly snap chadburn to 0° (stop)

### S Key - Reverse Movement Control
- **Hold S:** Chadburn rotates counter-clockwise continuously at `engineChadburnRotationSpeed` (degrees/second)
- **Release S:** Chadburn stops at current position
- **CTRL+S:** Instantly snap chadburn to maximum counter-clockwise position (full astern)
- **LEFT-SHIFT+S:** Instantly snap chadburn to 0° (stop)

## Configuration

### New Settings Added

**keybindings.json:**
```json
"engineForward": "A",
"engineReverse": "S",
"engineChadburnRotationSpeed": 45.0
```

**DefaultKeybindings.cs:**
```csharp
public KeyCode defaultEngineForward = KeyCode.A;
public KeyCode defaultEngineReverse = KeyCode.S;
public float defaultEngineChadburnRotationSpeed = 45f;
```

**KeyBindingConfig.cs:**
- Added runtime properties: `engineForward`, `engineReverse`, `engineChadburnRotationSpeed`
- Player can rebind keys via `SetRuntimeKey("engineForward", newKey)`
- Player can change rotation speed via `SetRuntimeFloat("engineChadburnRotationSpeed", newSpeed)`

## Implementation Details

### ChadburnController.cs Changes

**Added Methods:**
1. `Update()` - Checks for keyboard input when not mouse-dragging
2. `HandleKeyboardInput()` - Processes A/S keys with modifier detection

**Key Features:**
- Keyboard input disabled during mouse dragging (no conflict)
- Continuous rotation when key held (smooth acceleration/deceleration)
- Instant snap with CTRL modifier (emergency full power)
- Instant stop with LEFT-SHIFT modifier (emergency stop)
- Respects max rotation limits (±maxRotationDegrees)
- Uses same SetRotation() method as mouse drag (consistent behavior)

**Modifier Priority:**
1. CTRL+A/S = Snap to max (only on key down)
2. SHIFT+A/S = Snap to zero (only on key down)
3. A/S alone = Continuous rotation (every frame while held)

### Physics Integration

**Ship Response:**
- Keys control chadburn position ONLY (not direct ship movement)
- Chadburn position controls engine power via existing Engine.cs
- Ship accelerates/decelerates based on engine power through normal physics
- CTRL+A (snap to full ahead) does NOT instantly change ship speed
- Ship must physically accelerate to reach target speed
- LEFT-SHIFT+A (snap to stop) does NOT instantly stop ship
- Ship must physically decelerate from current velocity

## Usage Examples

### Gradual Acceleration
1. Hold A key
2. Chadburn rotates clockwise at 45°/second
3. Release A when desired power reached
4. Ship gradually accelerates to match chadburn setting

### Emergency Full Speed
1. Press CTRL+A
2. Chadburn instantly snaps to maximum forward position
3. Ship begins accelerating to maximum speed
4. Acceleration follows normal physics curve

### Emergency Stop
1. Press LEFT-SHIFT+A (or LEFT-SHIFT+S)
2. Chadburn instantly snaps to 0°
3. Engine power cuts to zero
4. Ship begins decelerating from current velocity
5. Deceleration follows normal physics (with drag/resistance)

### Fine Control
1. Tap A repeatedly for small increments
2. Each tap rotates chadburn by ~2-3° (45°/sec × ~0.05sec frame)
3. Precise power adjustments without overshooting

## Developer Notes

### Customization
- Change `defaultEngineChadburnRotationSpeed` in DefaultKeybindings Inspector
- Higher values = faster rotation when key held
- Lower values = finer control
- Recommended range: 30-90 degrees/second

### Testing Checklist
- [ ] A key rotates chadburn forward (clockwise)
- [ ] S key rotates chadburn reverse (counter-clockwise)
- [ ] CTRL+A snaps to max forward
- [ ] CTRL+S snaps to max reverse
- [ ] SHIFT+A snaps to zero
- [ ] SHIFT+S snaps to zero
- [ ] Release key stops rotation at current position
- [ ] Mouse drag still works (keyboard disabled during drag)
- [ ] Ship acceleration/deceleration follows physics
- [ ] Multiple rapid key taps work smoothly

### Performance
- Keyboard input only processed when not dragging
- No continuous input polling when using mouse
- SetRotation() called at most once per frame per key
- Debug logging optional (disable for production)

## Files Modified

1. `/Assets/Resources/keybindings.json` - Added engine control keys and rotation speed
2. `/Assets/Scripts/Input/KeyBindingConfig.cs` - Added runtime properties and serialization
3. `/Assets/Scripts/Input/DefaultKeybindings.cs` - Added default values for developer
4. `/Assets/Scripts/ChadburnController.cs` - Added Update() and HandleKeyboardInput() methods

## Player Configuration

Players can modify these settings by:
1. Editing `keybindings.json` directly
2. Using in-game settings menu (if implemented) via:
   - `KeyBindingConfig.Instance.SetRuntimeKey("engineForward", KeyCode.W)`
   - `KeyBindingConfig.Instance.SetRuntimeFloat("engineChadburnRotationSpeed", 60f)`
   - `KeyBindingConfig.Instance.SaveToJSONFile()`

To reset to defaults:
```csharp
KeyBindingConfig.Instance.ResetToDefaults();
KeyBindingConfig.Instance.SaveToJSONFile();
```
