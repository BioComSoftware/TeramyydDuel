# SHIP'S WHEEL - SETUP GUIDE
## Step-by-Step Instructions for Creating the Steering Control

The Ship's Wheel is a rotating control that allows the player to steer the ship by rotating it around its central axis (horizontal plane).

---

## OVERVIEW

**What it does:**
- **0° (indicator pointing up)** = No rotation (ship goes straight)
- **1° to 90° clockwise** = Ship rotates right (0.0167 to 1.5 degrees/sec)
- **1° to 90° counter-clockwise** = Ship rotates left (0.0167 to 1.5 degrees/sec)

**How it works:**
- Player drags the wheel with mouse
- Wheel rotates around its center pivot point
- Rotation angle determines ship's rotation speed (degrees/second)
- Ship rotates around its Y-axis in the horizontal plane only
- Dead zone: ±5° = no rotation (buffer for user)
- Wheel color changes: White (center), Cyan (right), Orange (left)

**Example Speeds:**
- 60° clockwise = 1.0°/sec right turn
- 90° clockwise = 1.5°/sec right turn (maximum)
- 60° counter-clockwise = 1.0°/sec left turn
- 90° counter-clockwise = 1.5°/sec left turn (maximum)

---

## PHASE 1: PREPARE YOUR SPRITE

### Step 1: Create Ship's Wheel Sprite
Create or obtain a ship's wheel image:
- Classic wooden ship's wheel with spokes
- **MUST have transparent background** (no background plate)
- Should have an "up" indicator (one spoke pointing to 12 o'clock)
- Typical size: 200px × 200px or larger
- Save as: `ShipWheel.png` (with transparency)

**Import Settings:**
1. Place in `Assets/Sprites/UI/`
2. Select in Project window
3. In Inspector (Texture Import Settings):
   - **Texture Type**: Sprite (2D and UI)
   - **Sprite Mode**: Single
   - **Pixels Per Unit**: 100
   - **Pivot**: **CRITICAL - Set to (0.5, 0.5)** (center)
     - Click "Custom" in Pivot dropdown
     - Set X: 0.5, Y: 0.5
   - **Filter Mode**: Bilinear
   - **Compression**: None
   - Click **Apply**

**Note:** If your wheel sprite has a transparent background (PNG with alpha channel), Unity will automatically handle the transparency. The sprite will display correctly without any additional settings needed.

---

## PHASE 2: ADD TO CANVAS

### Step 2: Locate Your HUD Canvas
1. Find your existing `HUD_Canvas` in the Hierarchy
2. This should be the same Canvas with your instrument panel and Chadburn

### Step 3: Create Ship Wheel Container
1. **Right-click** `HUD_Canvas` → **Create Empty**
2. Name it: `ShipWheel_Container`
3. In Inspector (RectTransform):
   - **Anchors**: Right-bottom corner (opposite of Chadburn)
     - **Anchors Min**: (1, 0)
     - **Anchors Max**: (1, 0)
   - **Pivot**: (0.5, 0.5)
   - **Pos X**: -150 (distance from right edge, negative for left of anchor)
   - **Pos Y**: 300 (distance from bottom edge)
   - **Width**: 200
   - **Height**: 200

**Positioning Tips:**
- Place on opposite side from Chadburn (Chadburn left, Wheel right)
- Won't overlap instrument panel (bottom-center)
- Adjust Pos X and Pos Y to your preferred location

---

## PHASE 3: BUILD THE WHEEL

### Step 4: Create Draggable Wheel
1. **Right-click** `ShipWheel_Container` → **UI** → **Image**
2. Name it: `ShipWheel`
3. In Inspector (Image component):
   - **Source Image**: Drag `ShipWheel` sprite here
   - **Color**: White (255, 255, 255, 255)
   - **Preserve Aspect**: ✓ (CRITICAL)
   - **Image Type**: Simple (NOT Sliced)
   - **Raycast Target**: ✓ (MUST be checked for dragging)
4. In Inspector (RectTransform):
   - **Anchors Min**: (0.5, 0.5)
   - **Anchors Max**: (0.5, 0.5)
   - **Pivot**: **(0.5, 0.5)** ← CRITICAL - center for rotation
   - **Pos X**: 0
   - **Pos Y**: 0
   - **Width**: 200 (adjust to desired wheel size)
   - **Height**: 200 (should be square for circular wheel)
   - **Rotation Z**: 0 (will be controlled by script)

**IMPORTANT:** The wheel's pivot point MUST be at (0.5, 0.5) - center. This is where it rotates around.

---

## PHASE 4: ADD SCRIPT AND CONFIGURE

### Step 5: Add ShipWheelController Script
1. Select `ShipWheel` object
2. **Add Component** → Search for `ShipWheelController`
3. In Inspector (ShipWheelController component):

**References:**
- **Wheel Transform**: Drag `ShipWheel` (itself) here
- **Target Ship**: Leave empty (auto-discovers ship with ShipCharacteristics)

**Rotation Settings:**
- **Max Wheel Rotation**: 90
  - Maximum angle the wheel can rotate (±90°)
  - 90° = full right/left turn
- **Dead Zone Degrees**: 5
  - ±5° around center = no rotation (buffer zone)
  - Prevents tiny accidental movements
- **Max Rotation Speed**: 1.5
  - Ship rotation speed (degrees/sec) at maximum wheel turn
  - 90° wheel = 1.5°/sec ship rotation
  - 60° wheel = 1.0°/sec ship rotation
- **Snap Increment**: 0
  - Set to 0 for smooth rotation
  - Set to 15 for snapping every 15 degrees
  - Set to 30 for coarse control (30°, 60°, 90°)

**Visual Feedback:**
- **Center Color**: White (255, 255, 255) - wheel at center
- **Right Turn Color**: Cyan (0, 204, 255) - turning right
- **Left Turn Color**: Orange (255, 204, 0) - turning left

**Audio (Optional):**
- **Wheel Turn Sound**: Drag audio clip for wheel rotation
- **Center Click Sound**: Drag audio clip for returning to center

**Debug:**
- **Debug Log**: ✓ (check for testing, uncheck for release)

---

## PHASE 5: TESTING

### Step 6: Test in Play Mode
1. **Enter Play Mode**
2. **Click and drag** the ship's wheel
3. Watch it rotate smoothly (or snap if snap increment is set)
4. Check Console for debug messages:
   - "Ship Wheel: 45.0° → RIGHT 0.75°/s"
   - "Ship Wheel: -60.0° → LEFT 1.00°/s"
   - "Ship Wheel: 0.0° → CENTER"
5. **Verify wheel color changes:**
   - White at 0°
   - Gradually cyan rotating clockwise
   - Gradually orange rotating counter-clockwise
6. **Verify ship rotation:**
   - Ship should rotate right when wheel is clockwise
   - Ship should rotate left when wheel is counter-clockwise
   - Ship should stop rotating when wheel is at center

### Step 7: Test Dead Zone
1. Move wheel slightly (less than 5°) from center
   - Ship should NOT rotate
   - Wheel should stay white
2. Move wheel beyond 5° in either direction
   - Ship should start rotating
   - Wheel should change color

### Step 8: Test Rotation Limits
1. Try to drag wheel beyond 90° clockwise
   - Should stop at maximum rotation
2. Try to drag wheel beyond 90° counter-clockwise
   - Should stop at maximum rotation

### Step 9: Troubleshooting

**Wheel won't drag:**
- Check `ShipWheel` has `Image` component with **Raycast Target** checked
- Verify `ShipWheelController` script is on `ShipWheel` object
- Check **Wheel Transform** field is assigned in inspector
- Make sure Canvas has a **Graphic Raycaster** component

**Wheel rotates from wrong point:**
- Check `ShipWheel` RectTransform **Pivot** is set to (0.5, 0.5)
- Verify sprite's import settings have Pivot set to center

**Ship doesn't rotate:**
- Check Console for error messages
- Verify `Target Ship` is assigned (or ShipCharacteristics exists in scene)
- Make sure ship has a Transform component (it always does)
- Check that ship is not frozen (RigidbodyConstraints)

**Wheel stretches/distorts when rotating:**
- Select `ShipWheel` Image component
- Check **Preserve Aspect** is enabled
- Make sure **Image Type** is set to **Simple**

**Ship rotates wrong direction:**
- Right turn should be clockwise (positive Y-axis rotation)
- Left turn should be counter-clockwise (negative Y-axis rotation)
- If backwards, check ship's forward direction (should be +Z)

**Colors don't change:**
- Verify `Center Color`, `Right Turn Color`, `Left Turn Color` are set
- Check `ShipWheel` has an `Image` component

**Rotation is too fast/slow:**
- Adjust **Max Rotation Speed** (default 1.5°/s)
- Higher value = faster turning
- Lower value = slower, more realistic turning

---

## PHASE 6: POLISH AND CUSTOMIZATION

### Step 10: Add Optional Center Indicator
Create a small indicator to show the "up" position:

1. **Right-click** `ShipWheel_Container` → **UI** → **Image**
2. Name it: `Wheel_CenterMark`
3. Use a small triangle or arrow sprite pointing up
4. Position it just above the wheel (fixed, doesn't rotate)
5. This shows the player where "center" is

### Step 11: Adjust Rotation Speeds
Customize the rotation behavior:

**Slow/Realistic Turning (Large Ship):**
- **Max Rotation Speed**: 0.5 to 1.0
- Ships turn very slowly, more realistic
- Requires planning ahead

**Medium Turning (Standard):**
- **Max Rotation Speed**: 1.5 (default)
- Balanced between responsiveness and realism

**Fast Turning (Small Ship/Arcade):**
- **Max Rotation Speed**: 3.0 to 5.0
- Quick, responsive controls
- More arcade-like feel

### Step 12: Adjust Dead Zone
**Tight Control (Skilled Players):**
- **Dead Zone Degrees**: 2
- Smaller buffer, more precise

**Standard (Default):**
- **Dead Zone Degrees**: 5
- Good balance

**Forgiving (Casual Players):**
- **Dead Zone Degrees**: 10
- Larger buffer, easier to stay centered

### Step 13: Add Snap Positions
For discrete control positions:

**Smooth (Default):**
- **Snap Increment**: 0
- Fully analog control

**Coarse Steps:**
- **Snap Increment**: 30
- Positions: 0°, ±30°, ±60°, ±90°
- "Center, Light, Medium, Hard" turns

**Fine Steps:**
- **Snap Increment**: 15
- Positions: 0°, ±15°, ±30°, ±45°, ±60°, ±75°, ±90°
- More granular control

### Step 14: Add Audio
1. Find or create audio clips:
   - **Wheel Turn Sound**: Wood creaking or ratchet sound
   - **Center Click Sound**: Mechanical click or thunk
2. Drag clips into `ShipWheelController` component
3. Adjust volume if needed

---

## HIERARCHY STRUCTURE

```
HUD_Canvas
├── [Other HUD elements...]
├── InstrumentPanel_Background (bottom-center)
├── Chadburn_Container (bottom-left)
│   └── [Chadburn components...]
└── ShipWheel_Container (bottom-right)
    ├── ShipWheel (Image + ShipWheelController script)
    └── Wheel_CenterMark (Image - Optional, fixed position indicator)
```

---

## QUICK REFERENCE: KEY SETTINGS

**ShipWheel RectTransform:**
- Pivot: **(0.5, 0.5)** ← Center, CRITICAL for rotation
- Anchors: Center of parent (0.5, 0.5)
- Position: (0, 0) - centered in container

**ShipWheel Image:**
- Preserve Aspect: ✓ MUST be checked
- Image Type: Simple
- Raycast Target: ✓ MUST be checked for dragging

**ShipWheelController:**
- Wheel Transform: Self-reference
- Max Wheel Rotation: 90 (±90° range)
- Dead Zone Degrees: 5 (±5° buffer)
- Max Rotation Speed: 1.5 (degrees/sec at full turn)
- Snap Increment: 0 (smooth) or 15/30 (snapped)

**Rotation Speed Formula:**
```
Ship Rotation Speed = (Wheel Angle / Max Wheel Rotation) × Max Rotation Speed

Examples (with Max Rotation Speed = 1.5):
- 30° wheel = (30/90) × 1.5 = 0.5°/sec
- 60° wheel = (60/90) × 1.5 = 1.0°/sec
- 90° wheel = (90/90) × 1.5 = 1.5°/sec (maximum)
```

---

## INTEGRATION WITH OTHER SYSTEMS

**With Chadburn (Speed Control):**
- Chadburn on left controls forward/reverse speed
- Wheel on right controls turning
- Both work independently and simultaneously
- Ship can move forward while turning

**With Instrument Panel:**
- Attitude Indicator shows ship's roll/pitch
- Can add heading/compass indicator for direction
- Airspeed shows forward movement while turning

**With Keyboard Controls:**
- Wheel works alongside keyboard steering (A/D keys)
- Both modify ship rotation
- Last input wins

---

## TIPS & BEST PRACTICES

1. **Realistic Pivot**: Ship rotates around its center, not the bow (front)
2. **Speed Tuning**: Larger ships should have lower `maxRotationSpeed` (0.5-1.0)
3. **Visual Feedback**: Color change helps player know wheel is active
4. **Dead Zone**: 5° is a good default, prevents micro-adjustments
5. **Combine with Speed**: Turn faster when moving slower (future enhancement)
6. **Snap for Precision**: Use 15° or 30° snap for tactical positioning

---

## ADVANCED: PROGRAMMATIC CONTROL

You can control the wheel from other scripts:

```csharp
// Get reference to wheel
ShipWheelController wheel = FindFirstObjectByType<ShipWheelController>();

// Set to full right
wheel.SetFullRight();

// Set to full left
wheel.SetFullLeft();

// Reset to center
wheel.ResetToCenter();

// Set to specific angle
wheel.SetAngle(45f); // 45° right turn
wheel.SetAngle(-30f); // 30° left turn

// Read current state
float wheelAngle = wheel.CurrentWheelRotation; // -90 to +90 degrees
float rotSpeed = wheel.ShipRotationSpeed; // degrees/sec (can be negative)
bool turningRight = wheel.TurningRight; // True if turning right
bool turningLeft = wheel.TurningLeft; // True if turning left
```

---

## PHYSICS CONSIDERATIONS

**Current Implementation:**
- Rotates ship directly via `Transform.Rotate()`
- Instant rotation response
- No momentum or inertia

**Future Enhancements:**
- Add turn acceleration (takes time to reach max rotation speed)
- Add turn deceleration (ship continues turning slightly after centering wheel)
- Tie rotation speed to ship's forward velocity (faster when moving)
- Add rudder resistance (harder to turn at high speeds)
- Implement banking/rolling during turns

---

## EXAMPLE CONFIGURATIONS

**Small Patrol Boat:**
- Max Wheel Rotation: 90°
- Dead Zone: 5°
- Max Rotation Speed: 3.0°/s
- Snap: 0 (smooth)

**Medium Cargo Ship:**
- Max Wheel Rotation: 90°
- Dead Zone: 5°
- Max Rotation Speed: 1.0°/s
- Snap: 0 (smooth)

**Large Battleship:**
- Max Wheel Rotation: 60°
- Dead Zone: 3°
- Max Rotation Speed: 0.5°/s
- Snap: 15 (positions at 0°, ±15°, ±30°, ±45°, ±60°)

**Arcade Speedboat:**
- Max Wheel Rotation: 120°
- Dead Zone: 10°
- Max Rotation Speed: 5.0°/s
- Snap: 0 (smooth)

---

## NEXT STEPS

- Add heading/compass indicator showing ship's current direction
- Create rudder position indicator (shows actual rudder angle)
- Implement turn rate indicator (shows current rotation speed)
- Add auto-center feature (wheel returns to 0° when released)
- Create wheel momentum (wheel continues spinning after release)
- Implement velocity-based turning (turn faster when stopped, slower when moving fast)
