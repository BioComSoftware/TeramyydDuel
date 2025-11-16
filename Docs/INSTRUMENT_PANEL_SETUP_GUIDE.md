# INSTRUMENT PANEL - UNITY EDITOR SETUP GUIDE
## Step-by-Step Instructions for Creating the Ship HUD

This guide will walk you through setting up the instrument panel in Unity using your custom instrument panel sprite and the provided scripts.

---

## PHASE 1: PREPARE YOUR SPRITES

### Step 1: Import Instrument Panel Background
1. Place your instrument panel image (the brass panel with 4 gauges) in `Assets/Sprites/UI/`
2. Select the image in Project window
3. In Inspector:
   - **Texture Type**: Sprite (2D and UI)
   - **Sprite Mode**: Single
   - **Pixels Per Unit**: 100 (adjust based on your image resolution)
   - **Filter Mode**: Bilinear
   - **Compression**: None (for crisp UI)
   - Click **Apply**

### Step 2: Create Needle/Hand Sprites
You'll need to create small sprite images for the clock hands and indicators:

**Required Sprites:**
1. **Needle_Thin.png** - Thin pointer for airspeed, altimeter, vertical speed
   - Simple thin line/arrow pointing upward (12 o'clock)
   - Recommended size: 5px wide × 50px tall
   - Pivot: Bottom-center (for rotation around base)

2. **Needle_Thick.png** - Thicker pointer for altimeter hundreds/thousands
   - Wider line/arrow pointing upward
   - Recommended size: 8px wide × 40px tall
   - Pivot: Bottom-center

3. **Airplane_Silhouette.png** - Airplane icon (top-down view)
   - Simple airplane shape viewed from behind (wings visible)
   - Recommended size: 60px wide × 40px tall
   - Pivot: Center

4. **Yaw_Triangle.png** - Triangle for yaw indicator
   - Simple triangle pointing up
   - Recommended size: 20px wide × 20px tall
   - Pivot: Center

**Import Settings for All Needles/Indicators:**
- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- Pixels Per Unit: 100
- Pivot: **IMPORTANT - Set correctly for each**
- Filter Mode: Bilinear
- Compression: None

---

## PHASE 2: LOCATE OR CREATE UI CANVAS

### Step 3: Use Existing Canvas (Recommended) OR Create New Canvas

**OPTION A: Use Existing HUD Canvas (Recommended)**
1. **Locate your existing HUD Canvas** in Hierarchy (e.g., `HUD_Canvas`)
2. This is the same Canvas that has your ship outline and game controls
3. **Skip to Step 4** below

**Benefits of using existing Canvas:**
- Better performance (single Canvas batch)
- Unified sorting and scaling
- Easier management of all HUD elements
- Consistent behavior across all UI

**OPTION B: Create New Separate Canvas**
1. **Right-click** in Hierarchy → **UI** → **Canvas**
2. Name it: `InstrumentPanelCanvas`
3. In Inspector (Canvas component):
   - **Render Mode**: Screen Space - Overlay
   - **Pixel Perfect**: ✓ (checked)
   - **Sort Order**: 10 (so it renders on top of other UI)

4. **Canvas Scaler** component:
   - **UI Scale Mode**: Scale With Screen Size
   - **Reference Resolution**: 1920 x 1080 (adjust to your target resolution)
   - **Screen Match Mode**: Match Width Or Height
   - **Match**: 0.5 (balance between width and height)

**Note:** For the rest of this guide, `[YourCanvas]` refers to either your existing HUD Canvas or the new InstrumentPanelCanvas you created.

---

## PHASE 3: BUILD INSTRUMENT PANEL STRUCTURE

### Step 4: Create Panel Background
1. **Right-click** `[YourCanvas]` → **UI** → **Image**
2. Name it: `InstrumentPanel_Background`
3. In Inspector (Image component):
   - **Source Image**: Drag your instrument panel sprite here
   - **Color**: White (255, 255, 255, 255)
   - **Preserve Aspect**: ✓ (checked)
4. Position it (RectTransform):
   - **Anchors Min**: (0.5, 0)
   - **Anchors Max**: (0.5, 0)
   - **Pivot**: (0.5, 0)
   - **Pos X**: 0
   - **Pos Y**: 50 (small margin from bottom)
   - **Width**: 800 (adjust to desired panel width)
   - **Height**: 200 (adjust to desired panel height)

**Positioning with Other HUD Elements:**
- Instrument panel at **bottom-center** won't overlap with ship controls (typically top/center)
- Use anchors so elements stay positioned correctly when screen resizes
- Suggested layout: Ship outline/controls (top-center), Instrument panel (bottom-center)
- Adjust Pos Y if needed to prevent overlap (increase from 50 to push panel higher)

### Step 5: Create Manager GameObject
1. **Right-click** `InstrumentPanel_Background` → **Create Empty**
2. Name it: `InstrumentPanel_Manager`
3. **Add Component** → Search for `InstrumentPanelManager`
4. Keep this object selected - we'll configure it later

---

## PHASE 4: BUILD EACH INSTRUMENT

### **INSTRUMENT 1: AIRSPEED INDICATOR**

#### Step 6a: Create Airspeed Container
1. **Right-click** `InstrumentPanel_Background` → **Create Empty**
2. Name it: `Airspeed_Indicator`
3. Position it over the airspeed gauge face (RectTransform):
   - **Anchors Min**: (0, 0.5)
   - **Anchors Max**: (0, 0.5)
   - **Pivot**: (0.5, 0.5)
   - **Pos X**: ~150 (adjust to center of first gauge from left edge)
   - **Pos Y**: 0
   - **Width**: 150
   - **Height**: 150

#### Step 6b: Create Airspeed Needle
1. **Right-click** `Airspeed_Indicator` → **UI** → **Image**
2. Name it: `Airspeed_Needle`
3. In Inspector (Image component):
   - **Source Image**: Your thin needle sprite
   - **Image Type**: Simple (NOT Sliced)
   - **Preserve Aspect**: ✓ (CRITICAL - prevents distortion when rotating)
   - **Color**: Black or dark brown (to match gauge)
   - **Raycast Target**: ✗ (unchecked - not clickable)
4. Position (RectTransform):
   - **Anchors Min**: (0.5, 0.5)
   - **Anchors Max**: (0.5, 0.5)
   - **Pivot**: **(0.5, 0)** ← CRITICAL - bottom center
   - **Pos X**: 0
   - **Pos Y**: 0
   - **Width**: 5
   - **Height**: 60 (should reach from center to edge of gauge)
   - **Rotation Z**: 0 (will be controlled by script)

#### Step 6c: Attach Airspeed Script
1. Select `Airspeed_Indicator` object
2. **Add Component** → Search for `AirspeedIndicator`
3. In Inspector:
   - **Needle Transform**: Drag `Airspeed_Needle` here
   - **Ship Characteristics**: Leave empty (auto-discovered)
   - **Max Airspeed Knots**: 10 (for 0-9 scale; or custom max like 20, 30, etc.)
   - **Zero Rotation Degrees**: 0 (top = 0 position)
   - **Rotate Clockwise**: ✓
   - **Damping Factor**: 5

**Note:** The gauge uses a 0-9 scale (not 1-12 clock). 0 is at the top. If your ship goes faster than 10 knots, increase `maxAirspeedKnots` (e.g., 20 knots = needle makes 2 full rotations from 0-20).

---

### **INSTRUMENT 2: ALTIMETER**

#### Step 7a: Create Altimeter Container
1. **Right-click** `InstrumentPanel_Background` → **Create Empty**
2. Name it: `Altimeter_Indicator`
3. Position it over the altimeter gauge face (RectTransform):
   - **Anchors Min**: (0, 0.5)
   - **Anchors Max**: (0, 0.5)
   - **Pivot**: (0.5, 0.5)
   - **Pos X**: ~350 (second gauge from left edge)
   - **Pos Y**: 0
   - **Width**: 150
   - **Height**: 150

#### Step 7b: Create Three Altimeter Needles
**Create these IN ORDER (for proper layering):**

1. **THOUSANDS HAND** (bottom layer):
   - Right-click `Altimeter_Indicator` → UI → Image
   - Name: `Altimeter_Thousands`
   - Source Image: Thick needle
   - **Image Type**: Simple (NOT Sliced)
   - **Preserve Aspect**: ✓ (CRITICAL - prevents distortion when rotating)
   - Color: Red or distinctive color
   - Anchors Min: (0.5, 0.5), Anchors Max: (0.5, 0.5)
   - Pivot: **(0.5, 0)**
   - Pos: (0, 0)
   - Width: 8, Height: 35

2. **HUNDREDS HAND** (middle layer):
   - Right-click `Altimeter_Indicator` → UI → Image
   - Name: `Altimeter_Hundreds`
   - Source Image: Thick needle
   - **Image Type**: Simple (NOT Sliced)
   - **Preserve Aspect**: ✓ (CRITICAL - prevents distortion when rotating)
   - Color: Blue or distinctive color
   - Anchors Min: (0.5, 0.5), Anchors Max: (0.5, 0.5)
   - Pivot: **(0.5, 0)**
   - Pos: (0, 0)
   - Width: 8, Height: 45

3. **TENS HAND** (top layer):
   - Right-click `Altimeter_Indicator` → UI → Image
   - Name: `Altimeter_Tens`
   - Source Image: Thin needle
   - **Image Type**: Simple (NOT Sliced)
   - **Preserve Aspect**: ✓ (CRITICAL - prevents distortion when rotating)
   - Color: White or light color
   - Anchors Min: (0.5, 0.5), Anchors Max: (0.5, 0.5)
   - Pivot: **(0.5, 0)**
   - Pos: (0, 0)
   - Width: 5, Height: 60

#### Step 7c: Attach Altimeter Script
1. Select `Altimeter_Indicator` object
2. **Add Component** → `AltimeterIndicator`
3. In Inspector:
   - **Tens Hand Transform**: Drag `Altimeter_Tens`
   - **Hundreds Hand Transform**: Drag `Altimeter_Hundreds`
   - **Thousands Hand Transform**: Drag `Altimeter_Thousands`
   - **Ship Characteristics**: Leave empty
   - **Zero Rotation Degrees**: 0 (top = 0 position)
   - **Rotate Clockwise**: ✓
   - **Damping Factor**: 5

**Note:** The altimeter uses a 0-9 scale. Each hand points to digits 0-9:
- At 2,456m: thousands hand between 2-3, hundreds hand between 4-5, tens hand between 5-6
- Each hand rotates 36° per digit (360° ÷ 10 positions)

---

### **INSTRUMENT 3: VERTICAL SPEED INDICATOR**

#### Step 8a: Create VSI Container
1. **Right-click** `InstrumentPanel_Background` → **Create Empty**
2. Name it: `VerticalSpeed_Indicator`
3. Position it over the climb gauge (RectTransform):
   - **Anchors Min**: (0, 0.5)
   - **Anchors Max**: (0, 0.5)
   - **Pivot**: (0.5, 0.5)
   - **Pos X**: ~550 (third gauge from left edge)
   - **Pos Y**: 0
   - **Width**: 150
   - **Height**: 150

#### Step 8b: Create VSI Needle
1. **Right-click** `VerticalSpeed_Indicator` → **UI** → **Image**
2. Name it: `VSI_Needle`
3. Configuration:
   - Source Image: Thin needle
   - **Image Type**: Simple (NOT Sliced)
   - **Preserve Aspect**: ✓ (CRITICAL - prevents distortion when rotating)
   - Color: Dark color
4. RectTransform:
   - Anchors Min: (0.5, 0.5), Anchors Max: (0.5, 0.5)
   - Pivot: **(0.5, 0)**
   - Pos: (0, 0)
   - Width: 5, Height: 60

#### Step 8c: Attach VSI Script
1. Select `VerticalSpeed_Indicator` object
2. **Add Component** → `VerticalSpeedIndicator`
3. In Inspector:
   - **Needle Transform**: Drag `VSI_Needle`
   - **Ship Characteristics**: Leave empty
   - **Max Climb Rate MPS**: 20
   - **Zero Rotation Degrees**: 0
   - **Max Climb Rotation Degrees**: 180 (right side = up)
   - **Max Descent Rotation Degrees**: -180 (left side = down)
   - **Damping Factor**: 3 (more lag like real VSI)

---

### **INSTRUMENT 4: ATTITUDE INDICATOR**

#### Step 9a: Create Attitude Container
1. **Right-click** `InstrumentPanel_Background` → **Create Empty**
2. Name it: `Attitude_Indicator`
3. Position it over the pitch/roll gauge (RectTransform):
   - **Anchors Min**: (0, 0.5)
   - **Anchors Max**: (0, 0.5)
   - **Pivot**: (0.5, 0.5)
   - **Pos X**: ~750 (fourth gauge from left edge)
   - **Pos Y**: 0
   - **Width**: 150
   - **Height**: 150

#### Step 9b: Create Airplane Silhouette
1. **Right-click** `Attitude_Indicator` → **UI** → **Image**
2. Name it: `Attitude_Airplane`
3. Configuration (RectTransform):
   - Source Image: Airplane silhouette sprite
   - Color: White or light color
   - Anchors Min: (0.5, 0.5), Anchors Max: (0.5, 0.5)
   - Pivot: **(0.5, 0.5)** (center)
   - Pos: (0, 0)
   - Width: 60, Height: 40
   - Rotation Z: 0 (will be controlled by script)

#### Step 9c: Create Yaw Triangle
1. **Right-click** `Attitude_Indicator` → **UI** → **Image**
2. Name it: `Attitude_YawTriangle`
3. Configuration (RectTransform):
   - Source Image: Triangle sprite
   - Color: Yellow or distinctive color
   - Anchors Min: (0.5, 0.5), Anchors Max: (0.5, 0.5)
   - Pivot: **(0.5, 0.5)** (center)
   - Pos X: 0
   - Pos Y: -50 (below airplane, near bottom of gauge)
   - Width: 20, Height: 20

#### Step 9d: Attach Attitude Script
1. Select `Attitude_Indicator` object
2. **Add Component** → `AttitudeIndicator`
3. In Inspector:
   - **Airplane Transform**: Drag `Attitude_Airplane`
   - **Yaw Triangle Transform**: Drag `Attitude_YawTriangle`
   - **Ship Characteristics**: Leave empty
   - **Max Pitch Degrees**: 45
   - **Max Pitch Movement Pixels**: 40
   - **Max Yaw Degrees**: 45
   - **Max Yaw Movement Pixels**: 50
   - **Damping Factor**: 8

---

## PHASE 5: CONFIGURE MANAGER

### Step 10: Link Everything to Manager
1. Select `InstrumentPanel_Manager` object
2. In Inspector (InstrumentPanelManager component):
   - **Ship Characteristics**: Leave empty (auto-discovered)
   - **Airspeed Indicator**: Drag `Airspeed_Indicator` component
   - **Altimeter Indicator**: Drag `Altimeter_Indicator` component
   - **Vertical Speed Indicator**: Drag `VerticalSpeed_Indicator` component
   - **Attitude Indicator**: Drag `Attitude_Indicator` component
   - **Instruments Enabled**: ✓
   - **Debug Log**: ✓ (for testing)

### Step 11: Optional - Add Canvas Group for Fading
1. Select `InstrumentPanel_Background`
2. **Add Component** → `Canvas Group`
3. Go back to `InstrumentPanel_Manager`
4. Drag `InstrumentPanel_Background` to **Panel Canvas Group** field

---

## PHASE 6: TESTING

### Step 12: Test in Play Mode
1. Make sure your ship has the `ShipCharacteristics` component
2. **Enter Play Mode**
3. Check Console for: `"=== Instrument Panel Setup ===" ` message
4. All instruments should show checkmarks (✓)
5. Move your ship and watch instruments respond:
   - **Airspeed**: Should rotate as ship moves
   - **Altimeter**: Three hands should rotate at different speeds
   - **VSI**: Should point up when climbing, down when descending
   - **Attitude**: Airplane should roll/pitch, triangle should move for yaw

### Step 13: Troubleshooting
**If needles don't move:**
- Check Console for errors
- Verify `ShipCharacteristics` exists in scene
- Check that needle RectTransforms are assigned in each indicator script
- Verify pivot points are correct (0.5, 0) for needles

**If rotation is wrong direction:**
- Toggle `Rotate Clockwise` setting
- Adjust `Zero Rotation Degrees` (try 0, 90, 180, 270)

**If needles point wrong direction:**
- Check pivot point (should be bottom-center for clock hands)
- Verify sprite is pointing UP in the source image

**If needles distort/stretch when rotating:**
- Select the needle Image object
- In Image component, check **Preserve Aspect**
- Make sure **Image Type** is set to **Simple** (not Sliced)

**If movements are too fast/slow:**
- Adjust `Damping Factor` (higher = smoother/slower, lower = faster)

---

## PHASE 7: FINE-TUNING

### Step 14: Adjust Visual Appearance
- **Needle Colors**: Select each needle Image component, change Color
- **Needle Length**: Adjust Height in RectTransform
- **Needle Thickness**: Adjust Width in RectTransform
- **Panel Size**: Adjust `InstrumentPanel_Background` Width/Height
- **Panel Position**: Adjust Pos Y to move panel up/down on screen

### Step 15: Adjust Instrument Ranges
- **Airspeed Max**: Change `Max Airspeed Knots` (default 10 for 0-9 scale)
  - For faster ships: Use 20 (needle makes 2 rotations), 30 (3 rotations), etc.
  - The needle will continuously rotate through the 0-9 positions
- **VSI Range**: Change `Max Climb Rate MPS` (default 20)
- **Pitch Range**: Change `Max Pitch Degrees` (default 45)
- **Yaw Range**: Change `Max Yaw Degrees` (default 45)

**Understanding the 0-9 Scale:**
- **Airspeed**: 0 at top, needle rotates clockwise through positions 0→1→2...→9→0 (repeats)
  - At 5 knots, needle points to position 5
  - At 15 knots (with max=20), needle points to position 5 (on second rotation)
- **Altimeter**: Each hand has its own 0-9 scale showing different magnitudes
  - Tens hand: Each digit = 10 meters (0=0-9m, 5=50-59m)
  - Hundreds hand: Each digit = 100 meters (0=0-99m, 5=500-599m)
  - Thousands hand: Each digit = 1000 meters (0=0-999m, 5=5000-5999m)

---

## QUICK REFERENCE: HIERARCHY STRUCTURE

**Using Existing Canvas (Recommended):**
```
HUD_Canvas (existing Canvas)
├── [Your existing HUD elements - ship outline, controls, etc.]
└── InstrumentPanel_Background (Image)
    ├── InstrumentPanel_Manager (has InstrumentPanelManager script)
    ├── Airspeed_Indicator (has AirspeedIndicator script)
    │   └── Airspeed_Needle (Image - RectTransform)
    ├── Altimeter_Indicator (has AltimeterIndicator script)
    │   ├── Altimeter_Thousands (Image - RectTransform)
    │   ├── Altimeter_Hundreds (Image - RectTransform)
    │   └── Altimeter_Tens (Image - RectTransform)
    ├── VerticalSpeed_Indicator (has VerticalSpeedIndicator script)
    │   └── VSI_Needle (Image - RectTransform)
    └── Attitude_Indicator (has AttitudeIndicator script)
        ├── Attitude_Airplane (Image - RectTransform)
        └── Attitude_YawTriangle (Image - RectTransform)
```

**Using Separate Canvas:**
```
InstrumentPanelCanvas (new Canvas)
└── InstrumentPanel_Background (Image)
    ├── InstrumentPanel_Manager (has InstrumentPanelManager script)
    ├── Airspeed_Indicator (has AirspeedIndicator script)
    │   └── Airspeed_Needle (Image - RectTransform)
    ├── Altimeter_Indicator (has AltimeterIndicator script)
    │   ├── Altimeter_Thousands (Image - RectTransform)
    │   ├── Altimeter_Hundreds (Image - RectTransform)
    │   └── Altimeter_Tens (Image - RectTransform)
    ├── VerticalSpeed_Indicator (has VerticalSpeedIndicator script)
    │   └── VSI_Needle (Image - RectTransform)
    └── Attitude_Indicator (has AttitudeIndicator script)
        ├── Attitude_Airplane (Image - RectTransform)
        └── Attitude_YawTriangle (Image - RectTransform)
```

---

## TIPS & BEST PRACTICES

1. **Use Existing Canvas**: Share Canvas with other HUD elements for better performance
2. **Layer Order**: Create needles in order (bottom to top) so they stack correctly
3. **Pivot Points**: Always double-check - wrong pivot = wrong rotation center
4. **Anchor Strategy**: Bottom-center for panel, top-center for ship controls = no overlap
5. **Debug Mode**: Enable debug logging on instruments to see current values
6. **Testing Values**: Use `SetAirspeed()`, `SetAltitude()`, etc. methods to test manually
7. **Performance**: Single Canvas = one batch draw call for entire UI

---

## NEXT STEPS

- Add labels/text for numeric readouts
- Create warning lights for overspeed/altitude
- Add engine power indicators
- Create throttle/control input UI
- Implement HUD fade in/out based on game state
