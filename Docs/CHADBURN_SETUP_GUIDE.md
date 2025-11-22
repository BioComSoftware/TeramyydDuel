# CHADBURN (ENGINE ORDER TELEGRAPH) - SETUP GUIDE
## Step-by-Step Instructions for Creating the Ship Speed Control

The Chadburn is a rotating handle control that allows the player to set the ship's forward (ahead) and reverse (astern) speed by dragging a handle around a dial.

---

## OVERVIEW

**What it does:**
- **0° (handle pointing up)** = Full Stop
- **1° to 100° clockwise** = 1% to 100% ahead (forward speed)
- **1° to 100° counter-clockwise** = 1% to 100% astern (reverse speed)

**How it works:**
- Player drags the handle with mouse
- Handle rotates around its bottom pivot point
- Rotation angle determines throttle percentage, which is converted to knots using engine power, ship mass, `FORCE_PER_POWER_UNIT`, and `KNOTS_TO_MPS`
- Automatically calls `Engine.SetKnotsAhead()` or `SetKnotsAstern()`
- Handle color changes: White (stop), Green (ahead), Red (astern)

---

## PHASE 1: PREPARE YOUR SPRITES

### Step 1: Create Chadburn Background Sprite
Create or obtain an image of the Chadburn dial face:
- Circular brass dial with markings
- Should show positions for STOP, SLOW, HALF, FULL (ahead and astern)
- Typical size: 200px × 200px or larger
- Save as: `Chadburn_Background.png`

**Import Settings:**
1. Place in `Assets/Sprites/UI/`
2. Select in Project window
3. In Inspector:
   - **Texture Type**: Sprite (2D and UI)
   - **Sprite Mode**: Single
   - **Pixels Per Unit**: 100
   - **Filter Mode**: Bilinear
   - **Compression**: None
   - Click **Apply**

### Step 2: Create Chadburn Handle Sprite
Create the handle/pointer image:
- Vertical handle or arrow pointing UP (12 o'clock position)
- Should be tall enough to reach from center to edge of dial
- Typical size: 20px wide × 80px tall
- Make sure it points UPWARD in the image
- Save as: `Chadburn_Handle.png`

**Import Settings:**
1. Place in `Assets/Sprites/UI/`
2. Select in Project window
3. In Inspector:
   - **Texture Type**: Sprite (2D and UI)
   - **Sprite Mode**: Single
   - **Pixels Per Unit**: 100
   - **Pivot**: **CRITICAL - Set to (0.5, 0)** (bottom-center)
     - Click "Custom" in Pivot dropdown
     - Set X: 0.5, Y: 0
   - **Filter Mode**: Bilinear
   - **Compression**: None
   - Click **Apply**

---

## PHASE 2: ADD TO CANVAS

### Step 3: Locate Your HUD Canvas
1. Find your existing `HUD_Canvas` in the Hierarchy
2. This should be the same Canvas with your instrument panel and ship controls

### Step 4: Create Chadburn Container
1. **Right-click** `HUD_Canvas` → **Create Empty**
2. Name it: `Chadburn_Container`
3. In Inspector (RectTransform):
   - **Anchors**: Left-bottom corner
     - **Anchors Min**: (0, 0)
     - **Anchors Max**: (0, 0)
   - **Pivot**: (0.5, 0.5)
   - **Pos X**: 150 (distance from left edge)
   - **Pos Y**: 300 (distance from bottom edge)
   - **Width**: 200
   - **Height**: 200

**Positioning Tips:**
- Place it where it won't overlap instrument panel (bottom-center)
- Typically good on left or right side of screen
- Adjust Pos X and Pos Y to your preferred location

---

## PHASE 3: BUILD THE CHADBURN

### Step 5: Create Background Dial
1. **Right-click** `Chadburn_Container` → **UI** → **Image**
2. Name it: `Chadburn_Background`
3. In Inspector (Image component):
   - **Source Image**: Drag `Chadburn_Background` sprite here
   - **Color**: White (255, 255, 255, 255)
   - **Preserve Aspect**: ✓ (checked)
4. In Inspector (RectTransform):
   - **Anchors Min**: (0.5, 0.5)
   - **Anchors Max**: (0.5, 0.5)
   - **Pivot**: (0.5, 0.5)
   - **Pos X**: 0
   - **Pos Y**: 0
   - **Width**: 200
   - **Height**: 200

### Step 6: Create Draggable Handle
1. **Right-click** `Chadburn_Container` → **UI** → **Image**
2. Name it: `Chadburn_Handle`
3. In Inspector (Image component):
   - **Source Image**: Drag `Chadburn_Handle` sprite here
   - **Color**: White (255, 255, 255, 255)
   - **Preserve Aspect**: ✓ (CRITICAL)
   - **Image Type**: Simple (NOT Sliced)
4. In Inspector (RectTransform):
   - **Anchors Min**: (0.5, 0.5)
   - **Anchors Max**: (0.5, 0.5)
   - **Pivot**: **(0.5, 0)** ← CRITICAL - bottom center for rotation
   - **Pos X**: 0
   - **Pos Y**: 0
   - **Width**: 20 (adjust to your handle sprite)
   - **Height**: 80 (should reach from center to edge of dial)
   - **Rotation Z**: 0 (will be controlled by script)

**IMPORTANT:** The handle's pivot point MUST be at (0.5, 0) - bottom center. This is where it rotates around.

---

## PHASE 4: ADD SCRIPT AND CONFIGURE

### Step 7: Add ChadburnController Script
1. Select `Chadburn_Handle` object (the handle, not background)
2. **Add Component** → Search for `ChadburnController`
3. In Inspector (ChadburnController component):

**References:**
- **Handle Transform**: Drag `Chadburn_Handle` (itself) here
- **Target Engine**: Leave empty (auto-discovers first Engine)

**Rotation Settings:**
- **Max Rotation Degrees**: 100
   - This is the maximum angle the handle can rotate
   - 100° clockwise = full ahead, 100° counter-clockwise = full astern
- **Snap Increment**: 0
   - Set to 0 for smooth rotation
   - Set to 10 for snapping every 10 degrees (like a real telegraph)
   - Set to 25 for SLOW/HALF/FULL positions

**Throttle Mapping:**
- **Throttle Response Seconds**: Default 30
   - Represents how many seconds of sustained thrust the telegraph assumes when converting percentage to knots
   - Higher values yield larger commanded speeds for the same power; lower values keep commands nearer to current speed
   - Uses `Engine.FORCE_PER_POWER_UNIT` and ship mass to remain physically consistent

**Visual Feedback:**
- **Stop Color**: White (255, 255, 255)
- **Ahead Color**: Green (0, 255, 0) - handle turns green going forward
- **Astern Color**: Red (255, 0, 0) - handle turns red going reverse

**Audio (Optional):**
- **Handle Move Sound**: Drag audio clip for handle movement
- **Stop Bell Sound**: Drag audio clip for bell when reaching stop

**Debug:**
- **Debug Log**: ✓ (check for testing, uncheck for release)

## PHASE 5: MAKE HANDLE DRAGGABLE

### Step 9: Ensure Raycast Target is Enabled
1. Select `Chadburn_Handle` object
2. In Inspector (Image component):
   - **Raycast Target**: ✓ (MUST be checked for dragging to work)

### Step 10: Optional - Add Visual Hover Effect
1. Select `Chadburn_Handle` object
2. **Add Component** → **UI** → **Button** (optional)
3. In Inspector (Button component):
   - **Interactable**: ✓
   - **Transition**: Color Tint
   - **Highlighted Color**: Slight brightness increase
   - **Pressed Color**: Darker shade
   - **Selected Color**: Same as Normal

**Note:** The Button component is optional - it just adds hover/press visual feedback. The `ChadburnController` script handles all the actual functionality.

---

## PHASE 6: TESTING

### Step 11: Test in Play Mode
1. **Enter Play Mode**
2. **Click and drag** the Chadburn handle
3. Watch it rotate smoothly (or snap if snap increment is set)
4. Check Console for debug messages:
   - "Chadburn: 45.0° → AHEAD 45.0kt (45%)"
   - "Chadburn: -30.0° → ASTERN 30.0kt (30%)"
   - "Chadburn: 0.0° → STOP (0%)"
5. **Verify handle color changes:**
   - White at 0°
   - Gradually green rotating clockwise
   - Gradually red rotating counter-clockwise
6. **Verify ship movement:**
   - Ship should accelerate forward when handle is clockwise
   - Ship should accelerate backward when handle is counter-clockwise
   - Ship should slow to stop when handle is at 0°

### Step 12: Test Rotation Limits
1. Try to drag handle beyond 100° clockwise
   - Should stop at maximum rotation
2. Try to drag handle beyond 100° counter-clockwise
   - Should stop at maximum rotation
3. Verify handle cannot be dragged past limits

### Step 13: Troubleshooting

**Handle won't drag:**
- Check `Chadburn_Handle` has `Image` component with **Raycast Target** checked
- Verify `ChadburnController` script is on `Chadburn_Handle` object
- Check **Handle Transform** field is assigned in inspector
- Make sure Canvas has a **Graphic Raycaster** component

**Handle rotates from wrong point:**
- Check `Chadburn_Handle` RectTransform **Pivot** is set to (0.5, 0)
- Verify sprite's import settings have Pivot set to bottom-center

**Handle rotates wrong direction:**
- This is normal - the script uses negative rotation for visual correctness
- Clockwise (ahead) appears as positive world rotation
- If it's backwards, flip the sprite or adjust in script

**Ship doesn't move:**
- Check Console for error messages
- Verify `Target Engine` is assigned (or engine exists in scene)
- Verify `Throttle Response Seconds` matches ship class expectations
- Verify engine is enabled and has power

**Handle stretches/distorts when rotating:**
- Select `Chadburn_Handle` Image component
- Check **Preserve Aspect** is enabled
- Make sure **Image Type** is set to **Simple**

**Colors don't change:**
- Verify `Stop Color`, `Ahead Color`, `Astern Color` are set in inspector
- Check `Chadburn_Handle` has an `Image` component (not just RectTransform)

---

## PHASE 7: POLISH AND CUSTOMIZATION

### Step 14: Add Text Labels (Optional)
You can add text labels around the dial for visual reference:

1. **Right-click** `Chadburn_Background` → **UI** → **Text - TextMeshPro**
2. Name it: `Label_FullAhead`
3. Position it at top-right of dial (where 100° points)
4. Text: "FULL AHEAD"
5. Repeat for other positions:
   - "SLOW AHEAD" at ~30°
   - "HALF AHEAD" at ~50°
   - "STOP" at 0° (top)
   - "SLOW ASTERN" at ~-30°
   - "HALF ASTERN" at ~-50°
   - "FULL ASTERN" at -100° (bottom-left)

### Step 15: Add Speed Readout (Optional)
Create a text display showing current ordered speed:

1. **Right-click** `Chadburn_Container` → **UI** → **Text - TextMeshPro**
2. Name it: `Chadburn_SpeedDisplay`
3. Position it below the dial
4. Initial text: "0 kts"
5. You'll need to create a simple script to update this from `ChadburnController.RequestedSpeedKnots`

### Step 16: Adjust Snap Positions
For authentic telegraph feel, set snap positions:

**Traditional Telegraph Positions:**
- **Snap Increment**: 25
- This gives you: 0°, ±25°, ±50°, ±75°, ±100°
- Matches: STOP, SLOW, HALF, FULL positions

**Percentage-Based:**
- **Snap Increment**: 10
- Gives you 10% increments (0%, 10%, 20%, ... 100%)

**Smooth Control:**
- **Snap Increment**: 0
- Allows precise analog control of speed

### Step 17: Add Audio
1. Find or create audio clips:
   - **Handle Move Sound**: Mechanical ratchet or click sound
   - **Stop Bell Sound**: Ship's bell or brass bell sound
2. Drag clips into `ChadburnController` component
3. Adjust volume in AudioSource component if needed

---

## HIERARCHY STRUCTURE

```
HUD_Canvas
├── [Other HUD elements...]
├── InstrumentPanel_Background
│   └── [Instruments...]
└── Chadburn_Container (Empty GameObject)
    ├── Chadburn_Background (Image)
    │   ├── Label_FullAhead (Text - Optional)
    │   ├── Label_HalfAhead (Text - Optional)
    │   ├── Label_Stop (Text - Optional)
    │   ├── Label_HalfAstern (Text - Optional)
    │   └── Label_FullAstern (Text - Optional)
    ├── Chadburn_Handle (Image + ChadburnController script)
    └── Chadburn_SpeedDisplay (Text - Optional)
```

---

## QUICK REFERENCE: KEY SETTINGS

**Chadburn_Handle RectTransform:**
- Pivot: **(0.5, 0)** ← Bottom-center, CRITICAL for rotation
- Anchors: Center of parent (0.5, 0.5)
- Position: (0, 0) - centered on dial

**Chadburn_Handle Image:**
- Preserve Aspect: ✓ MUST be checked
- Image Type: Simple
- Raycast Target: ✓ MUST be checked for dragging

**ChadburnController:**
- Handle Transform: Self-reference
- Max Rotation Degrees: 100 (±100° range)
- Snap Increment: 0 (smooth) or 10/25 (snapped positions)

**Engine:**
- Max Speed Knots: 100 (or your ship's actual max speed)

---

## INTEGRATION WITH OTHER SYSTEMS

**With Instrument Panel:**
- Place Chadburn on left or right side of screen
- Instrument panel stays at bottom-center
- Both use same HUD Canvas for performance

**With Throttle Controller:**
- If you have separate throttle controls, decide which is primary
- Chadburn sets speed in knots (SetKnotsAhead/Astern)
- Throttle might set power percentage
- They can coexist but player should use one at a time

**With Keyboard Controls:**
- Chadburn works alongside keyboard ship controls
- Both modify same engine speed settings
- Last input wins (either Chadburn or keyboard)

---

## TIPS & BEST PRACTICES

1. **Realistic Snapping**: Use snap increment of 25° for authentic telegraph feel (STOP, SLOW, HALF, FULL)
2. **Speed Scaling**: Tune `Throttle Response Seconds` to suit ship class (small: 20s, medium: 30s, large: 45s)
3. **Audio Feedback**: Add bell sound at stop position for immersive feel
4. **Visual Polish**: Add glow or highlight effect on handle when dragging
5. **Lerp Movement**: Handle snaps instantly - add lerp in script if you want smooth transitions
6. **Multi-Engine**: If ship has multiple engines, Chadburn can control all by targeting each

---

## ADVANCED: PROGRAMMATIC CONTROL

You can control the Chadburn from other scripts:

```csharp
// Get reference to Chadburn
ChadburnController chadburn = FindFirstObjectByType<ChadburnController>();

// Set to full ahead
chadburn.SetFullAhead();

// Set to full astern
chadburn.SetFullAstern();

// Set to stop
chadburn.ResetToStop();

// Set to specific percentage ahead (0-100%)
chadburn.SetPercentageAhead(50f); // 50% ahead

// Set to specific percentage astern (0-100%)
chadburn.SetPercentageAstern(75f); // 75% astern

// Read current state
float rotation = chadburn.CurrentRotation; // -100 to +100 degrees
float percentage = chadburn.CurrentPercentage; // 0 to 100%
float speedKnots = chadburn.RequestedSpeedKnots; // Actual knots requested
bool isAhead = chadburn.IsAhead; // True if going forward
bool isAstern = chadburn.IsAstern; // True if going reverse
```

---

## NEXT STEPS

- Add engine RPM gauge showing actual engine power
- Create rudder control for steering
- Add "All Stop" emergency button
- Implement engine room response delay (realistic time before engine obeys)
- Add telegraph confirmation bell (engine room acknowledges order)
- Create multi-engine Chadburn for ships with port/starboard engines
