# ATTITUDE LEVERS - SETUP GUIDE
## Step-by-Step Instructions for Roll and Pitch Controls in Unity 6.2

The Attitude Levers provide **visual-only** control of the ship's orientation. These levers allow the ship to pitch and roll independently of its velocity vector—creating a "drift" effect similar to space flight where the ship can appear nose-down while climbing, or roll sideways while flying straight.

---

## OVERVIEW

**What They Do:**

**Roll Lever (Bank Control):**
- **0° (vertical)** = Wings level (no roll)
- **Clockwise rotation** = Right wing down (positive roll)
- **Counter-clockwise rotation** = Left wing down (negative roll)
- Range: ±maxRollDegrees (e.g., ±45° or ±60°)
- **IMPORTANT**: Roll is PURELY VISUAL—ship maintains its trajectory

**Pitch Lever (Nose Control):**
- **90° (horizontal)** = Level flight (zero pitch)
- **Rotation above 90°** = Nose up (positive pitch)
- **Rotation below 90°** = Nose down (negative pitch)
- Asymmetric range: Different limits for nose-up vs nose-down
- **IMPORTANT**: Pitch is PURELY VISUAL—ship maintains its trajectory

**How They Work:**
- Player drags levers with mouse (telegraph/Chadburn style)
- Lever rotation directly controls ship's visual orientation
- Ship's velocity vector is **completely independent** of attitude
- Ship can climb while pitched nose-down, or fly straight while rolled 45°
- Creates realistic "airship drift" effect

**Example Behaviors:**
- Ship climbing at 10 m/s while pitched 20° nose-down
- Ship flying straight while rolled 45° to the right
- Ship descending while pitched 30° nose-up
- Velocity and attitude are decoupled—no physics interaction

---

## PHASE 1: PREPARE YOUR SPRITES

### Step 1: Create Lever Base Sprites
You'll need background images for the lever mechanisms:

**Roll Lever Base:**
- Circular or semicircular gauge face
- Markings showing roll degrees (e.g., 0° center, ±30°, ±45°)
- Labels: "LEFT BANK" / "RIGHT BANK" or "PORT" / "STARBOARD"
- Recommended size: 200px × 200px
- Save as: `RollLeverBase.png`

**Pitch Lever Base:**
- Semicircular gauge face (90° arc typical)
- Markings showing pitch angles
- Labels: "NOSE UP" / "NOSE DOWN" or "CLIMB" / "DIVE"
- Lever starts horizontal (90°) for level flight
- Recommended size: 200px × 150px
- Save as: `PitchLeverBase.png`

**Import Settings:**
1. Place sprites in `Assets/Sprites/UI/`
2. Select each sprite in Project window
3. In Inspector (Texture Import Settings):
   - **Texture Type**: Sprite (2D and UI)
   - **Sprite Mode**: Single
   - **Pixels Per Unit**: 100
   - **Filter Mode**: Bilinear
   - **Compression**: None
   - Click **Apply**

### Step 2: Create Lever Handle Sprites
Create draggable lever handles:

**Roll Lever Handle:**
- Vertical lever/handle pointing up at 0° (wings level)
- Simple stick or traditional lever design
- Should clearly indicate direction when rotated
- Recommended size: 10px wide × 80px tall
- Pivot: **Bottom-center (0.5, 0)** - rotates around base
- Save as: `RollLeverHandle.png`

**Pitch Lever Handle:**
- Horizontal lever/handle pointing right at 90° (level flight)
- Similar style to roll lever for consistency
- Recommended size: 80px wide × 10px tall (or rotated vertical design)
- Pivot: **Left-center (0, 0.5)** or **Bottom-center (0.5, 0)** depending on design
- Save as: `PitchLeverHandle.png`

**Import Settings for Lever Handles:**
1. Place in `Assets/Sprites/UI/`
2. Select in Project window
3. In Inspector:
   - **Texture Type**: Sprite (2D and UI)
   - **Sprite Mode**: Single
   - **Pixels Per Unit**: 100
   - **Pivot**: **CRITICAL - Set correctly for rotation**
     - Roll Lever: (0.5, 0) - bottom-center
     - Pitch Lever: (0, 0.5) - left-center OR (0.5, 0) - bottom-center
   - **Filter Mode**: Bilinear
   - **Compression**: None
   - Click **Apply**

**Note on Transparency:** PNG files with alpha channels will automatically display transparently in Unity—no additional settings required.

---

## PHASE 2: ADD TO CANVAS

### Step 3: Locate Your HUD Canvas
1. Find your existing `HUD_Canvas` in the Hierarchy
2. This should be the same Canvas with:
   - Instrument panel (bottom-center)
   - Chadburn controls (bottom-left or bottom-right)
   - Ship wheel (if you have one)

**Layout Recommendations:**
- **Roll Lever**: Left side, mid-height (easy thumb/finger access)
- **Pitch Lever**: Right side, mid-height (mirrors roll lever)
- Keep away from bottom (instrument panel) and center (visibility)
- Symmetrical placement looks clean and professional

### Step 4: Create Roll Lever Container
1. **Right-click** `HUD_Canvas` → **Create Empty**
2. Name it: `RollLever_Container`
3. In Inspector (RectTransform):
   - **Anchors**: Left side, vertical center
     - **Anchors Min**: (0, 0.5)
     - **Anchors Max**: (0, 0.5)
   - **Pivot**: (0.5, 0.5)
   - **Pos X**: 150 (distance from left edge)
   - **Pos Y**: 0 (centered vertically)
   - **Width**: 200
   - **Height**: 200

### Step 5: Create Pitch Lever Container
1. **Right-click** `HUD_Canvas` → **Create Empty**
2. Name it: `PitchLever_Container`
3. In Inspector (RectTransform):
   - **Anchors**: Right side, vertical center
     - **Anchors Min**: (1, 0.5)
     - **Anchors Max**: (1, 0.5)
   - **Pivot**: (0.5, 0.5)
   - **Pos X**: -150 (distance from right edge, negative for left of anchor)
   - **Pos Y**: 0 (centered vertically)
   - **Width**: 200
   - **Height**: 200

---

## PHASE 3: BUILD ROLL LEVER

### Step 6: Create Roll Lever Base Background
1. **Right-click** `RollLever_Container` → **UI** → **Image**
2. Name it: `RollLever_Base`
3. In Inspector (Image component):
   - **Source Image**: Drag `RollLeverBase` sprite
   - **Color**: White (255, 255, 255, 255)
   - **Preserve Aspect**: ✓
   - **Raycast Target**: ✗ (not clickable)
4. In Inspector (RectTransform):
   - **Anchors Min**: (0.5, 0.5)
   - **Anchors Max**: (0.5, 0.5)
   - **Pivot**: (0.5, 0.5)
   - **Pos X**: 0
   - **Pos Y**: 0
   - **Width**: 200
   - **Height**: 200

### Step 7: Create Roll Lever Handle
1. **Right-click** `RollLever_Container` → **UI** → **Image**
2. Name it: `RollLever_Handle`
3. In Inspector (Image component):
   - **Source Image**: Drag `RollLeverHandle` sprite
   - **Color**: White (255, 255, 255, 255) - will change to orange at extremes
   - **Preserve Aspect**: ✓ (CRITICAL - prevents distortion when rotating)
   - **Image Type**: Simple (NOT Sliced)
   - **Raycast Target**: ✓ (MUST be checked for dragging)
4. In Inspector (RectTransform):
   - **Anchors Min**: (0.5, 0.5)
   - **Anchors Max**: (0.5, 0.5)
   - **Pivot**: **(0.5, 0)** ← CRITICAL - bottom-center for rotation
   - **Pos X**: 0
   - **Pos Y**: 0
   - **Width**: 10
   - **Height**: 80
   - **Rotation Z**: 0 (vertical = wings level)

### Step 8: Add RollLeverController Script
1. Select `RollLever_Handle` object
2. **Add Component** → Search for `RollLeverController`
3. In Inspector (RollLeverController component):

**References:**
- **Lever Image**: Drag `RollLever_Handle` (itself) here
- **Lever Transform**: Drag `RollLever_Handle` (itself) here
- **Target Ship**: Leave empty (auto-discovers ShipCharacteristics)

**Roll Constraints:**
- **Max Roll Degrees**: 45
  - Maximum bank angle (symmetric: ±45°)
  - 0° = wings level, +45° = right wing down, -45° = left wing down
  - Adjust based on desired maneuverability (30° conservative, 60° aggressive)

**Lever Mechanics:**
- **Lever Base Transform**: Drag `RollLever_Container` here
- **Drag Radius**: 100 (pixels from lever base for drag calculation)
- **Snap To Angle**: 0
  - Set to 0 for smooth analog control
  - Set to 5 or 10 for snapped positions (e.g., 0°, ±10°, ±20°, etc.)
- **Return To Center On Release**: ✗ (unchecked)
  - Leave unchecked—pilot must manually return to wings level
  - Check if you want auto-centering (arcade-style)

**Visual Feedback:**
- **Center Color**: White (255, 255, 255) - wings level
- **Max Roll Color**: Orange (255, 165, 0) - maximum bank

**Audio (Optional):**
- **Lever Move Sound**: Drag audio clip for lever movement
- **Lever Stop Sound**: Drag audio clip when lever reaches limit

**Debug:**
- **Debug Log**: ✓ (check for testing, uncheck for release)

---

## PHASE 4: BUILD PITCH LEVER

### Step 9: Create Pitch Lever Base Background
1. **Right-click** `PitchLever_Container` → **UI** → **Image**
2. Name it: `PitchLever_Base`
3. In Inspector (Image component):
   - **Source Image**: Drag `PitchLeverBase` sprite
   - **Color**: White (255, 255, 255, 255)
   - **Preserve Aspect**: ✓
   - **Raycast Target**: ✗ (not clickable)
4. In Inspector (RectTransform):
   - **Anchors Min**: (0.5, 0.5)
   - **Anchors Max**: (0.5, 0.5)
   - **Pivot**: (0.5, 0.5)
   - **Pos X**: 0
   - **Pos Y**: 0
   - **Width**: 200
   - **Height**: 200

### Step 10: Create Pitch Lever Handle
1. **Right-click** `PitchLever_Container` → **UI** → **Image**
2. Name it: `PitchLever_Handle`
3. In Inspector (Image component):
   - **Source Image**: Drag `PitchLeverHandle` sprite
   - **Color**: White (255, 255, 255, 255) - will change based on pitch
   - **Preserve Aspect**: ✓ (CRITICAL - prevents distortion when rotating)
   - **Image Type**: Simple (NOT Sliced)
   - **Raycast Target**: ✓ (MUST be checked for dragging)
4. In Inspector (RectTransform):
   - **Anchors Min**: (0.5, 0.5)
   - **Anchors Max**: (0.5, 0.5)
   - **Pivot**: **(0.5, 0)** ← CRITICAL - bottom-center for rotation
   - **Pos X**: 0
   - **Pos Y**: 0
   - **Width**: 10
   - **Height**: 80
   - **Rotation Z**: 90 (horizontal = level flight)

### Step 11: Add PitchLeverController Script
1. Select `PitchLever_Handle` object
2. **Add Component** → Search for `PitchLeverController`
3. In Inspector (PitchLeverController component):

**References:**
- **Lever Image**: Drag `PitchLever_Handle` (itself) here
- **Lever Transform**: Drag `PitchLever_Handle` (itself) here
- **Target Ship**: Leave empty (auto-discovers ShipCharacteristics)

**Pitch Constraints (ASYMMETRIC):**
- **Max Pitch Up Degrees**: 30
  - Maximum nose-up angle (lever rotates counter-clockwise from 90°)
  - 90° = level, 60° = +30° nose up
  - Typical range: 20-45° (airships have limited climb attitude)
- **Max Pitch Down Degrees**: 20
  - Maximum nose-down angle (lever rotates clockwise from 90°)
  - 90° = level, 110° = -20° nose down
  - Usually less than pitch-up (diving is more restricted)
- **Level Flight Angle**: 90
  - Lever angle representing level flight (0° pitch)
  - Default 90° (horizontal lever) is intuitive
  - Don't change unless you have a specific design reason

**Lever Mechanics:**
- **Lever Base Transform**: Drag `PitchLever_Container` here
- **Drag Radius**: 100 (pixels from lever base for drag calculation)
- **Snap To Angle**: 0
  - Set to 0 for smooth analog control
  - Set to 5 or 10 for snapped positions
- **Return To Center On Release**: ✗ (unchecked)
  - Leave unchecked—pilot must manually return to level
  - Check for auto-leveling (arcade-style)

**Visual Feedback:**
- **Level Flight Color**: White (255, 255, 255) - level attitude
- **Nose Up Color**: Green (0, 255, 0) - climbing attitude
- **Nose Down Color**: Red (255, 0, 0) - diving attitude

**Audio (Optional):**
- **Lever Move Sound**: Drag audio clip for lever movement
- **Lever Stop Sound**: Drag audio clip when lever reaches limit

**Debug:**
- **Debug Log**: ✓ (check for testing, uncheck for release)

---

## PHASE 5: VERIFY SHIP INTEGRATION

### Step 12: Check ShipCharacteristics Component
1. Select your ship GameObject in Hierarchy
2. Verify `ShipCharacteristics` component exists
3. The attitude control methods are now built-in:
   - `SetRollAttitude(float degrees)`
   - `SetPitchAttitude(float degrees)`
   - `currentRollDegrees` (read-only property)
   - `currentPitchDegrees` (read-only property)

**IMPORTANT:** The ShipCharacteristics script has been updated to support attitude control. If you see errors about missing methods, ensure you're using the latest version of ShipCharacteristics.cs.

### Step 13: Verify Rigidbody Settings
1. Select your ship GameObject
2. In Inspector, find `Rigidbody` component
3. Check constraints:
   - **Constraints** → **Freeze Rotation**: Should be **UNCHECKED**
   - The attitude levers need to freely control rotation
   - Velocity will NOT be affected by attitude changes

**Note:** The updated ShipCharacteristics removes the `FreezeRotation` constraint to allow manual attitude control. The ship's velocity vector remains independent of its visual orientation.

---

## PHASE 6: TESTING

### Step 14: Test Roll Lever in Play Mode
1. **Enter Play Mode**
2. **Click and drag** the roll lever handle
3. **Verify behaviors:**
   - Lever rotates smoothly (or snaps if snap enabled)
   - Ship rolls left/right visually
   - Lever color transitions: White → Orange at extremes
   - Console shows: "Roll lever: 30.0° → Ship roll: 30.0°"
4. **Test trajectory independence:**
   - Fly ship straight forward (velocity constant)
   - Roll ship to 45° with lever
   - Ship should maintain straight trajectory while visually banked
   - Velocity vector unchanged

### Step 15: Test Pitch Lever in Play Mode
1. Still in Play Mode
2. **Click and drag** the pitch lever handle
3. **Verify behaviors:**
   - Lever starts at 90° (horizontal = level flight)
   - Rotating counter-clockwise (above 90°) = nose up, GREEN color
   - Rotating clockwise (below 90°) = nose down, RED color
   - Console shows: "Pitch lever: 60.0° → Ship pitch: 30.0°"
4. **Test trajectory independence:**
   - Ship climbing at +10 m/s vertical velocity
   - Pitch ship nose-down to -20° with lever
   - Ship should continue climbing while visually nose-down
   - Vertical velocity unchanged

### Step 16: Test Combined Attitude Control
1. Still in Play Mode
2. **Combine roll and pitch:**
   - Set roll to +30° (right wing down)
   - Set pitch to +15° (nose up)
   - Ship should display combined attitude
3. **Verify independent axes:**
   - Changing roll doesn't affect pitch
   - Changing pitch doesn't affect roll
4. **Test velocity independence:**
   - Set ship moving at 20 knots, climbing 5 m/s
   - Change attitude dramatically (roll 45°, pitch -20°)
   - Airspeed indicator should be constant
   - Vertical speed indicator should be constant
   - Ship maintains trajectory regardless of visual orientation

### Step 17: Test Return to Level Flight
1. Set extreme attitude (roll ±45°, pitch ±30°)
2. Manually drag both levers to center:
   - Roll to 0° (vertical)
   - Pitch to 90° (horizontal)
3. Ship should return to wings-level, nose-level attitude
4. Alternatively, you can programmatically call:
   ```csharp
   shipCharacteristics.ResetAttitude();
   ```

### Step 18: Troubleshooting

**Levers won't drag:**
- Check `RollLever_Handle` and `PitchLever_Handle` have **Image** components with **Raycast Target** checked
- Verify scripts are on the lever handle objects (not containers)
- Check **Lever Transform** and **Lever Base Transform** fields are assigned
- Ensure Canvas has a **Graphic Raycaster** component

**Levers rotate from wrong point:**
- Check lever handle RectTransform **Pivot** is set to (0.5, 0) - bottom-center
- Verify sprite's import settings have correct Pivot
- If using horizontal lever design for pitch, pivot might need to be (0, 0.5) - left-center

**Ship doesn't rotate with levers:**
- Check Console for error messages
- Verify `Target Ship` is assigned or ShipCharacteristics exists
- Ensure ShipCharacteristics has updated attitude control methods
- Check that Rigidbody does NOT have FreezeRotation constraint

**Ship rotation affects velocity (wrong behavior):**
- This should NOT happen—attitude is purely visual
- Check that movement scripts use velocity, NOT transform.forward
- Verify engines apply thrust to Rigidbody.velocity, not transform-relative
- Attitude should be cosmetic overlay on physics simulation

**Levers stretch/distort when rotating:**
- Select lever handle Image component
- Check **Preserve Aspect** is enabled
- Make sure **Image Type** is set to **Simple**

**Pitch lever angles are backwards:**
- Verify **Level Flight Angle** is 90
- Check sprite is oriented correctly (should point right at 90°)
- Adjust **Max Pitch Up** and **Max Pitch Down** if needed

**Colors don't change:**
- Verify color fields are set in inspector
- Check lever handles have `Image` components
- Try setting colors to more extreme values for visibility

**Lever snaps to wrong angles:**
- If **Snap To Angle** is non-zero, lever will snap to increments
- Set to 0 for smooth control
- Verify **Max Roll Degrees** and **Max Pitch** values are reasonable

---

## PHASE 7: POLISH AND CUSTOMIZATION

### Step 19: Add Optional Labels
Create text labels for clarity:

**Roll Lever Labels:**
1. **Right-click** `RollLever_Container` → **UI** → **Text - TextMeshPro**
2. Name it: `RollLever_Label`
3. Text: "BANK CONTROL" or "ROLL"
4. Position above or below the lever base
5. Font size: 14-18
6. Color: White or light gray
7. Alignment: Center

**Pitch Lever Labels:**
1. **Right-click** `PitchLever_Container` → **UI** → **Text - TextMeshPro**
2. Name it: `PitchLever_Label`
3. Text: "PITCH CONTROL" or "ATTITUDE"
4. Position above or below the lever base
5. Font size: 14-18
6. Color: White or light gray
7. Alignment: Center

### Step 20: Add Optional Numeric Readouts
Display current attitude in degrees:

**Roll Readout:**
1. **Right-click** `RollLever_Container` → **UI** → **Text - TextMeshPro**
2. Name it: `RollLever_Readout`
3. Position: Below lever, centered
4. Default text: "0°"
5. Font size: 16-20, bold
6. Color: Cyan or white
7. **Add script** to update text with `shipCharacteristics.currentRollDegrees`

**Pitch Readout:**
1. **Right-click** `PitchLever_Container` → **UI** → **Text - TextMeshPro**
2. Name it: `PitchLever_Readout`
3. Position: Below lever, centered
4. Default text: "0°"
5. Font size: 16-20, bold
6. Color: Green or white
7. **Add script** to update text with `shipCharacteristics.currentPitchDegrees`

### Step 21: Adjust Attitude Limits
Customize control ranges for different ship types:

**Small Agile Airship (Fighter-style):**
- **Max Roll Degrees**: 60 (can bank steeply)
- **Max Pitch Up**: 45 (aggressive climbs)
- **Max Pitch Down**: 30 (steep dives)

**Medium Cargo Airship (Standard):**
- **Max Roll Degrees**: 45 (moderate banking)
- **Max Pitch Up**: 30 (comfortable climbs)
- **Max Pitch Down**: 20 (safe descents)

**Large Battleship (Heavy/Slow):**
- **Max Roll Degrees**: 30 (gentle banking only)
- **Max Pitch Up**: 20 (limited climb attitude)
- **Max Pitch Down**: 15 (shallow dives)

**Experimental/Acrobatic:**
- **Max Roll Degrees**: 90 (can roll completely on side)
- **Max Pitch Up**: 60 (near-vertical climbs)
- **Max Pitch Down**: 45 (dramatic dives)

### Step 22: Add Snap Positions
For tactical/precise control:

**Smooth (Default):**
- **Snap To Angle**: 0
- Fully analog control

**Coarse Steps:**
- **Snap To Angle**: 15
- Roll positions: 0°, ±15°, ±30°, ±45°
- Pitch positions: 60°, 75°, 90°, 105°, 120°

**Fine Steps:**
- **Snap To Angle**: 5
- Many discrete positions for precision

### Step 23: Add Audio Feedback
1. Find or create audio clips:
   - **Lever Move Sound**: Mechanical ratchet, hydraulic hiss
   - **Lever Stop Sound**: Solid clunk or mechanical lock
2. Import to `Assets/Audio/UI/`
3. Drag clips into controller components:
   - `RollLeverController` → Lever Move/Stop Sound
   - `PitchLeverController` → Lever Move/Stop Sound
4. Adjust AudioSource settings if needed (volume, 2D sound)

---

## HIERARCHY STRUCTURE

```
HUD_Canvas
├── [Other HUD elements - instruments, Chadburn, etc.]
├── RollLever_Container (left side, mid-height)
│   ├── RollLever_Base (Image - background gauge)
│   ├── RollLever_Handle (Image + RollLeverController script)
│   ├── RollLever_Label (TextMeshPro - Optional)
│   └── RollLever_Readout (TextMeshPro - Optional)
└── PitchLever_Container (right side, mid-height)
    ├── PitchLever_Base (Image - background gauge)
    ├── PitchLever_Handle (Image + PitchLeverController script)
    ├── PitchLever_Label (TextMeshPro - Optional)
    └── PitchLever_Readout (TextMeshPro - Optional)
```

---

## QUICK REFERENCE: KEY SETTINGS

**RollLever_Handle RectTransform:**
- Pivot: **(0.5, 0)** ← Bottom-center, CRITICAL for rotation
- Anchors: Center of parent (0.5, 0.5)
- Position: (0, 0) - centered in container
- Rotation Z: 0 (vertical = wings level starting position)

**RollLever_Handle Image:**
- Preserve Aspect: ✓ MUST be checked
- Image Type: Simple
- Raycast Target: ✓ MUST be checked for dragging

**RollLeverController:**
- Lever Transform: Self-reference
- Lever Base Transform: RollLever_Container
- Max Roll Degrees: 45 (symmetric: ±45°)
- Drag Radius: 100
- Snap To Angle: 0 (smooth) or 5/10/15 (snapped)
- Return To Center On Release: ✗ (manual control)

**PitchLever_Handle RectTransform:**
- Pivot: **(0.5, 0)** ← Bottom-center, CRITICAL for rotation
- Anchors: Center of parent (0.5, 0.5)
- Position: (0, 0) - centered in container
- Rotation Z: 90 (horizontal = level flight starting position)

**PitchLever_Handle Image:**
- Preserve Aspect: ✓ MUST be checked
- Image Type: Simple
- Raycast Target: ✓ MUST be checked for dragging

**PitchLeverController:**
- Lever Transform: Self-reference
- Lever Base Transform: PitchLever_Container
- Level Flight Angle: 90 (horizontal lever)
- Max Pitch Up Degrees: 30 (lever rotates to 60°)
- Max Pitch Down Degrees: 20 (lever rotates to 110°)
- Drag Radius: 100
- Snap To Angle: 0 (smooth) or 5/10/15 (snapped)
- Return To Center On Release: ✗ (manual control)

**Attitude Formulas:**
```
Roll:
- Lever Angle = Ship Roll (1:1 mapping)
- 0° lever = 0° roll (wings level)
- +45° lever = +45° roll (right wing down)
- -45° lever = -45° roll (left wing down)

Pitch:
- Ship Pitch = 90° - Lever Angle
- 90° lever = 0° pitch (level flight)
- 60° lever = +30° pitch (nose up)
- 110° lever = -20° pitch (nose down)
```

---

## INTEGRATION WITH OTHER SYSTEMS

**With Instrument Panel:**
- **Attitude Indicator**: Shows current roll/pitch visually
- Levers and instrument should match—verify synchronization
- Attitude Indicator reads from ShipCharacteristics (same source)

**With Velocity Controls (Chadburn, Lift Levers):**
- Attitude levers control **appearance** only
- Chadburn controls **forward speed** (horizontal velocity)
- Lift levers control **climb/descent rate** (vertical velocity)
- All systems independent—adjust any without affecting others

**With Ship Wheel (Yaw Control):**
- Ship wheel controls **heading** (Y-axis rotation)
- Roll/Pitch levers control **bank/pitch** (X/Z-axis rotation)
- All three axes independent
- Ship can roll while turning, pitch while heading changes

**With Keyboard Controls:**
- Attitude levers can coexist with keyboard pitch/roll inputs
- Last input wins (lever or keyboard)
- Consider disabling one for consistency
- Or implement priority system (lever overrides keyboard)

---

## TIPS & BEST PRACTICES

1. **Realistic Pivot**: Levers rotate around base (bottom-center pivot)
2. **Color Feedback**: Visual cues help pilot understand current attitude
3. **Asymmetric Pitch**: Nose-up typically has greater range than nose-down (airship physics)
4. **No Auto-Center**: Pilot must manually level ship (more engaging)
5. **Velocity Independence**: Ship trajectory never changes due to attitude—critical design principle
6. **Testing**: Fly with extreme attitudes to verify velocity truly independent
7. **Audio**: Subtle mechanical sounds enhance immersion
8. **Labels**: Clear labeling prevents confusion between roll/pitch controls

---

## ADVANCED: PROGRAMMATIC CONTROL

Control levers from other scripts:

```csharp
// Get references to levers
RollLeverController rollLever = FindFirstObjectByType<RollLeverController>();
PitchLeverController pitchLever = FindFirstObjectByType<PitchLeverController>();

// Set roll attitude
rollLever.SetLeverAngle(30f); // 30° right wing down
rollLever.SetLeverAngle(-45f); // 45° left wing down
rollLever.SetLeverAngle(0f); // Wings level

// Set pitch attitude
pitchLever.SetLeverAngle(70f); // Nose up (ship pitch = +20°)
pitchLever.SetLeverAngle(110f); // Nose down (ship pitch = -20°)
pitchLever.SetLeverAngle(90f); // Level flight (ship pitch = 0°)

// Read current state
float currentRoll = rollLever.CurrentLeverAngle; // -maxRoll to +maxRoll
float currentPitchLever = pitchLever.CurrentLeverAngle; // ~60° to ~110°
float currentPitchShip = 90f - currentPitchLever; // Convert to ship pitch

// Direct ship control (bypass levers)
ShipCharacteristics ship = FindFirstObjectByType<ShipCharacteristics>();
ship.SetRollAttitude(45f); // Set roll directly
ship.SetPitchAttitude(-20f); // Set pitch directly
ship.ResetAttitude(); // Return to wings-level, nose-level
```

---

## PHYSICS CONSIDERATIONS

**Current Implementation:**
- Attitude changes modify `transform.eulerAngles` directly
- Rigidbody does NOT have FreezeRotation constraint
- Velocity vector (`rb.velocity`) is NEVER modified by attitude
- Ship's "Being" (trajectory) is separate from "Appearance" (orientation)

**Design Philosophy (Heideggerian):**
- Attitude is **aesthetic overlay** on physics simulation
- Ship's ontological state (position, velocity) unchanged by visual orientation
- Player controls two independent aspects:
  - **Trajectory**: Via thrust (engines), lift (altitude), heading (wheel)
  - **Attitude**: Via roll/pitch levers (visual only)

**Future Enhancements:**
- Add attitude-based aerodynamic effects (optional realism)
- Implement attitude influence on drag/lift (if desired)
- Add auto-leveling autopilot mode
- Create attitude trim system (set and forget attitude offset)

---

## EXAMPLE CONFIGURATIONS

**Small Fighter Airship:**
- Max Roll: 60°
- Max Pitch Up: 45°
- Max Pitch Down: 30°
- Snap: 0 (smooth)
- Return To Center: ✗

**Medium Cargo Ship:**
- Max Roll: 45°
- Max Pitch Up: 30°
- Max Pitch Down: 20°
- Snap: 5 (fine steps)
- Return To Center: ✗

**Large Battleship:**
- Max Roll: 30°
- Max Pitch Up: 20°
- Max Pitch Down: 15°
- Snap: 10 (coarse steps)
- Return To Center: ✗

**Arcade Speedboat:**
- Max Roll: 90°
- Max Pitch Up: 60°
- Max Pitch Down: 45°
- Snap: 0 (smooth)
- Return To Center: ✓ (auto-levels when released)

**Space Fighter (Zero-G Style):**
- Max Roll: 180° (can roll upside-down)
- Max Pitch Up: 90° (vertical orientation)
- Max Pitch Down: 90° (vertical orientation)
- Snap: 0 (smooth)
- Return To Center: ✗

---

## NEXT STEPS

- Add autopilot mode (maintains attitude automatically)
- Create attitude trim controls (fine-tune attitude offsets)
- Implement attitude presets (buttons for level flight, specific angles)
- Add attitude rate damping (smooth transitions)
- Create visual indicators on levers (LED lights for limits)
- Add haptic feedback for limit reaches (if supported)
- Implement attitude-velocity coupling toggle (realistic vs arcade mode)

---

## DEBUGGING CHECKLIST

If levers aren't working correctly, verify:

**Setup Checklist:**
- ✓ Lever handle sprites imported with correct pivot points
- ✓ Lever handles have Image components with Raycast Target enabled
- ✓ RollLeverController attached to RollLever_Handle
- ✓ PitchLeverController attached to PitchLever_Handle
- ✓ Lever Transform and Lever Base Transform assigned
- ✓ Target Ship assigned or ShipCharacteristics exists in scene
- ✓ Canvas has Graphic Raycaster component
- ✓ ShipCharacteristics script is updated version with attitude methods

**Physics Checklist:**
- ✓ Ship Rigidbody does NOT have FreezeRotation constraint
- ✓ Ship movement scripts use velocity, not transform.forward
- ✓ Engines apply thrust to rb.velocity, not transform-relative forces

**Visual Checklist:**
- ✓ Lever handles rotate around correct pivot (bottom-center)
- ✓ Colors change appropriately with lever position
- ✓ Ship visually rotates when levers moved
- ✓ Ship velocity remains constant when attitude changes

**Console Checklist:**
- ✓ No error messages about missing methods
- ✓ Debug logs show lever angles and ship attitudes
- ✓ Lever drag events firing correctly

---

## COMMON QUESTIONS

**Q: Why doesn't pitch affect climb rate?**
A: By design—attitude is purely visual. Use lift levers for climb/descent rate. This creates the "drift" effect where ship can look nose-down while climbing.

**Q: Can I couple attitude to velocity (realistic mode)?**
A: Yes, but requires modifying engine thrust to be transform.forward-relative instead of velocity-based. This is a significant design change.

**Q: Why is pitch asymmetric but roll symmetric?**
A: Real aircraft/airships typically have more nose-up range than nose-down. Roll is naturally symmetric. Adjust to your preference.

**Q: Should levers auto-center when released?**
A: Preference-based. Auto-center is arcade-style and easier. Manual control is more engaging and realistic for airships.

**Q: How do I add autopilot leveling?**
A: Create a script that monitors ship attitude and gradually calls `SetLeverAngle()` on both levers to return to 0°/90°.

**Q: Can I use these levers for camera controls instead?**
A: Yes—modify scripts to control a Camera transform instead of ship transform. Perfect for cinematic camera controls.

---

## ADDITIONAL RESOURCES

**Related Scripts:**
- `ShipCharacteristics.cs` - Attitude control methods
- `RollLeverController.cs` - Roll lever implementation
- `PitchLeverController.cs` - Pitch lever implementation
- `ChadburnController.cs` - Similar telegraph-style control reference

**Related Setup Guides:**
- `Shipwheel_setup_guide.md` - Yaw control setup
- `instrument_panel_setup_guide.md` - Instrument panel (shows attitude)
- `CHADBURN_SETUP_GUIDE.md` - Engine telegraph setup

**Design Documentation:**
- `Design.md` - Overall game design philosophy
- `DEV_JOURNAL.md` - Development history and decisions

---

**End of Setup Guide**
