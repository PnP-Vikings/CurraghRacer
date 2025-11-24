# 🎨 Cartoony Beer Particle Settings Guide

Complete settings for cartoony, fun beer pouring effects!

---

## 🌊 Pour Stream Particles (Tap to Glass)

### Main Module
```
Duration: 5.00
Looping: ✅ Checked
Start Lifetime: 0.4 - 0.6 (Random Between Two Constants)
Start Speed: 3 - 5 (Random Between Two Constants)
Start Size: 0.15 - 0.25 (Random Between Two Constants)
Start Rotation: 0 - 360 (Random)
Start Color: White (Will be set dynamically by code)
Gravity Modifier: 3 (Strong downward pull for liquid feel)
Simulation Space: World
Play On Awake: ❌ Unchecked (controlled by code)
Max Particles: 200
```

### Emission
```
Rate over Time: 80
Bursts: None
```

### Shape
```
Shape: Cone
Angle: 3° (Tight stream)
Radius: 0.05
Arc: 360°
Emit from: Base
```

### Size over Lifetime
```
✅ Enabled
Curve: Start at 1.0, gradually shrink to 0.7 at end
(Creates tapering stream effect)
```

### Color over Lifetime (Optional for extra cartooniness)
```
✅ Enabled
Gradient:
- Start: White (alpha 1.0)
- Middle (50%): Slight brightness boost (alpha 1.0)
- End (100%): Slightly transparent (alpha 0.8)
```

### Velocity over Lifetime
```
✅ Enabled
Linear: (0, -2, 0) (Slight extra downward push)
```

### Renderer
```
Render Mode: Billboard
Material: Default-Particle (or create custom with soft circle texture)
Cast Shadows: Off
Receive Shadows: Off
Trail Material: None
Sorting Layer: Default
Order in Layer: 0
```

### Recommended Texture
- **Built-in**: Use Unity's "Default-Particle" material
- **Custom**: Soft white circle with feathered edges (like a water droplet)
- **Color**: Pure white (code will tint it to beer color)

---

## 💨 Foam Overflow Particles (Glass Rim)

### Main Module
```
Duration: 2.00
Looping: ❌ Unchecked (one-time burst)
Start Lifetime: 0.8 - 1.5 (Random Between Two Constants)
Start Speed: 1.5 - 3.0 (Random Between Two Constants)
Start Size: 0.2 - 0.5 (Random Between Two Constants)
Start Rotation: 0 - 360 (Random)
Start Color: White (Will be set dynamically by code)
Gravity Modifier: -0.5 (Slight upward float for foam lightness)
Simulation Space: World
Play On Awake: ❌ Unchecked (triggered on overpour)
Max Particles: 50
```

### Emission
```
Rate over Time: 0
Bursts: 
  - Time: 0.00
  - Count: 20 - 25 (Random Between Two Constants)
  - Cycles: 1
  - Interval: 0.01
```

### Shape
```
Shape: Cone
Angle: 25° (Wider spray for explosive foam)
Radius: 0.15
Arc: 360°
Emit from: Base
Randomize Direction: 0.3 (Slight randomness)
```

### Size over Lifetime
```
✅ Enabled
Curve: 
- Start: 0.5 (small)
- Peak (30%): 1.2 (expand)
- Middle (60%): 1.0 (maintain)
- End: 0.3 (shrink and pop)

Creates a "bubble pop" effect!
```

### Color over Lifetime
```
✅ Enabled
Gradient:
- Start (0%): Full color, alpha 1.0
- Middle (50%): Slightly brighter, alpha 0.9
- End (100%): Same color, alpha 0.0 (fade out)
```

### Velocity over Lifetime
```
✅ Enabled
Linear: (0, 0.5, 0) (Gentle upward drift)
Orbital: 
  - X: 0.5 (slight swirl)
  - Y: 0.3
  - Z: 0.5
Speed Modifier: 0.8
```

### Rotation over Lifetime
```
✅ Enabled
Angular Velocity: 45 - 90 (Random spin for cartoony effect)
```

### Noise (Optional - adds wobble)
```
✅ Enabled
Strength: 0.3
Frequency: 1.0
Scroll Speed: 0.5
Damping: ✅ Checked
Quality: Medium
```

### Renderer
```
Render Mode: Billboard
Material: Default-Particle (or custom foam texture)
Cast Shadows: Off
Receive Shadows: Off
Sorting Layer: Default
Order in Layer: 1 (Above pour stream)
```

### Recommended Texture
- **Style**: Soft bubble or foam texture
- **Look**: Circular with slight irregularity
- **Options**:
  - Unity's "Default-Particle" (works great!)
  - Soft white circle with slight noise
  - Cartoon bubble sprite

---

## 🎨 Material Settings

### Pour Stream Material
```
Shader: Particles/Standard Unlit
Rendering Mode: Fade
Color Mode: Multiply
Main Maps:
  - Albedo: White circle texture
  - Alpha: Same texture
Color: White (tinted by particle system)
```

### Foam Overflow Material
```
Shader: Particles/Standard Unlit
Rendering Mode: Additive (for glowy foam effect)
OR: Fade (for softer foam)
Color Mode: Additive (makes it pop!)
Main Maps:
  - Albedo: Foam bubble texture
  - Alpha: Soft feathered edges
Color: White (tinted by particle system)
```

---

## 🌟 Extra Cartoony Polish

### Pour Stream Enhancements
1. **Sub Emitters** (Optional):
   - Birth: Small splash particles when stream hits glass
   - Rate: 5 particles per birth
   - Size: 0.1 - 0.15
   - Lifetime: 0.3s

2. **Trails** (Optional for liquid streaks):
   - Ratio: 0.5
   - Lifetime: 0.2
   - Minimum Vertex Distance: 0.1
   - Width: Curve from 1.0 to 0.0
   - Color: Match stream color

### Foam Overflow Enhancements
1. **Light Component** (Optional):
   - Add soft white point light
   - Intensity: 0.5
   - Range: 1.5
   - Gives magical foam glow!

2. **Sub Emitters** (Pop effect):
   - Death: Tiny sparkle particles on foam pop
   - Rate: 2-3 particles per death
   - Size: 0.05
   - Color: Bright white
   - Lifetime: 0.2s

---

## 🎯 Quick Setup Checklist

### Pour Stream
- [ ] Set Gravity Modifier to 3
- [ ] Cone angle to 3° (tight stream)
- [ ] Rate over Time: 80
- [ ] Start Speed: 3-5
- [ ] Start Size: 0.15-0.25
- [ ] Duration: 5 seconds
- [ ] Play On Awake: OFF
- [ ] Position at tap spout

### Foam Overflow
- [ ] Set Gravity Modifier to -0.5 (float up)
- [ ] Cone angle to 25° (wide burst)
- [ ] Burst: 20-25 particles at time 0
- [ ] Start Speed: 1.5-3.0
- [ ] Start Size: 0.2-0.5
- [ ] Size over Lifetime: Bubble curve
- [ ] Rotation over Lifetime: 45-90°
- [ ] Play On Awake: OFF
- [ ] Position at glass rim

---

## 🎮 Testing Tips

### Pour Stream
1. **Too thin?** → Increase Start Size to 0.3-0.4
2. **Too slow?** → Increase Start Speed to 6-8
3. **Not enough particles?** → Increase Rate to 100-120
4. **Falls too fast?** → Reduce Gravity Modifier to 2
5. **Too scattered?** → Reduce Cone Angle to 1-2°

### Foam Overflow
1. **Not bubbly enough?** → Increase burst count to 30-35
2. **Falls down instead of up?** → Increase negative Gravity Modifier to -1.0
3. **Too fast?** → Reduce Start Speed to 1.0-2.0
4. **Doesn't feel cartoony?** → Enable Rotation over Lifetime and Noise
5. **Disappears too quick?** → Increase Start Lifetime to 1.5-2.0

---

## 📸 Visual Reference Description

### Pour Stream Should Look Like:
- Smooth, continuous liquid flow
- Slight taper from wide at top to narrow at bottom
- Golden/amber color (beer liquid)
- Gentle sparkle/shine
- Straight down with slight wiggle

### Foam Overflow Should Look Like:
- Explosive burst of bubbles
- Floats upward then fades
- Spins and wobbles
- Pops and disappears
- White/cream colored foam
- Cartoony "bloosh!" effect

---

## 🔧 Optimization Notes

**Performance Impact**: Light
- Pour Stream: ~80 particles active during pouring
- Foam Overflow: ~20-25 particles per burst (short duration)
- Total: ~100-120 particles max per tap
- 4 taps = ~400-500 particles max (very manageable!)

**Mobile Considerations**:
- Reduce Rate to 60 for pour stream
- Reduce Burst to 15 for foam
- Disable Sub Emitters
- Disable Noise module
- Use simpler textures

---

## 🎨 Color Reference (Automatically Set by Code)

The code dynamically sets these colors per beer type:

**Pour Stream Colors** (Beer Liquid):
- Lager: Bright Yellow
- Stout: Dark Brown/Black
- Ale: Amber Brown
- IPA: Orange Amber
- Pilsner: Light Yellow

**Foam Colors** (Beer Foam):
- Lager: Pure White (#FFFFFF)
- Stout: Tan (#D2B48C)
- Ale: Cream (#FFFDD0)
- IPA: Off-White (#FAF0E6)
- Pilsner: Yellow-White (#FFFFF0)

---

**Ready to Create!** 🍺

Follow these settings in Unity's Particle System component for perfect cartoony beer effects!

