# Beer Pouring Minigame - Quick Setup Guide

## Step-by-Step Unity Setup

### 1. Scene Setup - Tap Configuration

For each of your 4 taps in the scene:

1. Add `TapConfiguration` component to the tap GameObject
2. Fill in Inspector fields:
   ```
   Tap Object: [Drag the visual tap/faucet GameObject]
   Tap Spout Position: [Create empty child Transform at pour spout, drag here]
   Associated Pour Point: [Drag corresponding BeerPourLocation]
   Tap Index: [Set to 0, 1, 2, or 3]
   ```

**Quick Tip**: Create an empty GameObject as child of each tap at the spout position where liquid should stream from.

---

### 2. Update BeerGameController

1. Select your BeerGameController GameObject
2. In Inspector:
   - Change `minigameCanvasUI` field type to `BeerMinigameCanvasUI` (you may need to reassign)
   - Set `Taps` array size to 4
   - Drag all 4 TapConfiguration components into the array
   - Assign `Finish Point` Transform (where completed beers go)
   - Verify `Beer Prefab` is assigned

---

### 3. Create/Update UI Canvas

1. Add `BeerMinigameCanvasUI` component (replace existing MinigameCanvasUI if present)
2. Create UI hierarchy:

```
Canvas (BeerMinigameCanvasUI)
├── TopLeftPanel (for tap timers)
│   ├── TapTimer1 (TextMeshPro)
│   ├── TapTimer2 (TextMeshPro)
│   ├── TapTimer3 (TextMeshPro)
│   └── TapTimer4 (TextMeshPro)
├── TopRightPanel (for streak)
│   └── StreakText (TextMeshPro)
├── TapOrderPanel (for order info)
│   ├── TapOrder1 (TextMeshPro)
│   ├── TapOrder2 (TextMeshPro)
│   ├── TapOrder3 (TextMeshPro)
│   └── TapOrder4 (TextMeshPro)
├── RoundFeedbackText (TextMeshPro - centered, large)
└── SummaryPanel (hidden by default)
    └── SummaryText (TextMeshPro)
```

3. Assign in Inspector:
   - `Tap Timer Texts`: Array of 4 timer texts
   - `Tap Order Texts`: Array of 4 order info texts
   - `Perfect Streak Text`: The streak display text
   - `Round Feedback Text`: Center screen feedback
   - `Summary Panel`: Final results panel
   - `Summary Container`: Parent of summary content

**Position Guidelines**:
- Tap timers: Top-left corner, vertical stack
- Streak display: Top-right corner
- Order info: Near/above each tap (use WorldSpace or ScreenSpace)

---

### 4. Update Beer Prefab

Your beer prefab needs these additions:

#### A. Add Fields to BeerShaderPour Component
In Inspector, you'll see new public fields:
- `Foam Overflow Particles`: Assign foam particle system
- `Pour Stream Particles`: Assign stream particle system
- `Target Zone Canvas`: Assign WorldSpace canvas (create as child)
- `Target Zone Image`: Assign UI Image on the canvas

#### B. Create Target Zone Overlay (Child of Beer Glass)
1. Add WorldSpace Canvas as child of beer glass mesh
   - Render Mode: World Space
   - Position: Center on glass
   - Scale: Adjust to glass size
2. Add UI Image as child of canvas
   - Stretch to fill
   - Color: Green with alpha ~0.3
3. Assign canvas and image to BeerShaderPour component

#### C. Update Material
- Ensure beer material uses `Custom/BeerCutoff` shader
- Material will have new properties: `_FoamColor` and `_FoamHeight` (set via code)

---

### 5. Create Particle System Prefabs

#### Pour Stream Particles
Create new GameObject with Particle System:
```
Main:
  - Start Lifetime: 0.3
  - Start Speed: 5
  - Start Size: 0.1
  - Start Color: White (will be set by code)
  - Gravity Modifier: 2

Emission:
  - Rate over Time: 75

Shape:
  - Shape: Cone
  - Angle: 5
  - Radius: 0.1

Renderer:
  - Material: Use liquid/water texture
```
Save as prefab, assign to beer prefab's `pourStreamParticles` field.

#### Foam Overflow Particles
Create new GameObject with Particle System:
```
Main:
  - Start Lifetime: 1.0
  - Start Speed: 3
  - Start Size: 0.2 (random between two constants: 0.2-0.5)
  - Start Color: White (will be set by code)
  - Gravity Modifier: 0.5

Emission:
  - Bursts: 1 burst, 15 particles at time 0

Shape:
  - Shape: Cone
  - Angle: 30
  - Radius: 0.2

Size over Lifetime:
  - Enable with animation curve (starts small, grows, then shrinks)

Renderer:
  - Material: Use foam/bubble texture
```
Save as prefab, assign to beer prefab's `foamOverflowParticles` field.
Position at rim of glass.

---

### 6. Testing Checklist

Start Play Mode and verify:

**Round 1 Start**:
- [ ] 1-2 beers spawn at taps
- [ ] Green target zones visible on glasses
- [ ] Timers appear at top-left for active taps only
- [ ] Order info shows beer type and customer name
- [ ] Streak display at top-right shows "0"

**During Pouring**:
- [ ] Hold pour button → liquid stream particles appear with beer color
- [ ] Release pour button → stream stops, beer locks
- [ ] Foam appears on glass after release
- [ ] Quality feedback displays briefly
- [ ] Score updates

**Timer Timeout**:
- [ ] If timer reaches 0, beer auto-submits as "Poor"
- [ ] Streak resets to 0

**Round Complete**:
- [ ] After all beers done, 3-second summary appears
- [ ] Shows "Round X/Y Complete!" with points breakdown
- [ ] Next round starts with new beers

**Perfect Streak**:
- [ ] Perfect pours increment streak counter
- [ ] Multiplier displays correctly (1.1x, 1.2x, etc.)
- [ ] Non-perfect pours reset streak to 0

**Game End**:
- [ ] After all rounds, final summary displays
- [ ] Shows total score and quality breakdown
- [ ] Returns to main menu/scene

---

## Common Issues & Solutions

### Issue: Target zone not visible
**Solution**: Check WorldSpace canvas is enabled and positioned correctly on beer glass. Verify Image component has green color with alpha > 0.

### Issue: Particles don't appear
**Solution**: Ensure particle systems are assigned in Inspector. Check that `Start Color` is not black. Verify particles are not culled (increase Max Particles if needed).

### Issue: Timers don't show
**Solution**: Verify `tapTimerTexts` array has 4 elements assigned. Check that TextMeshPro components are active. Ensure UpdateTapTimer is being called.

### Issue: Beer doesn't lock when releasing button
**Solution**: Check BeerPourButton is properly triggering `StopPouringBeer()`. Verify `isLocked` field in BeerShaderPour. Check console for errors in `LockPourAndCalculateQuality()`.

### Issue: Foam doesn't appear
**Solution**: Verify shader has `_FoamColor` and `_FoamHeight` properties. Check material is using updated shader. Verify `UpdateFoamAppearance()` is being called.

### Issue: Round doesn't progress
**Solution**: Check `CheckRoundComplete()` logic. Verify `completedInRound` increments correctly. Check console for errors in `ShowRoundFeedback()`.

---

## Performance Optimization Tips

1. **Shader Properties**: Replace string lookups with `Shader.PropertyToID()`:
   ```csharp
   private static readonly int CutoffHeightID = Shader.PropertyToID("_CutoffHeight");
   beerMatInstance.SetFloat(CutoffHeightID, cutoff);
   ```

2. **Particle Pooling**: For repeated games, pool particle systems instead of creating new ones.

3. **UI Updates**: Cache transform/component references to avoid `GetComponent()` calls.

---

## Customization Ideas

- **Variable Round Count**: Change `Random.Range(3, 6)` in `StartOrderRoundSystem()` for more/fewer rounds
- **Difficulty Scaling**: Adjust zone tolerances in `GenerateNextRoundOrders()`
- **Custom Beer Types**: Add more entries to `BeerType` enum and update color mappings
- **Customer Personality**: Add patience modifier based on customer name patterns
- **Combo Bonuses**: Award extra points for multiple perfect pours in a row

---

**Setup Complete!** Your beer pouring minigame should now be fully functional with precision pouring, performance streaks, and round-based progression.

