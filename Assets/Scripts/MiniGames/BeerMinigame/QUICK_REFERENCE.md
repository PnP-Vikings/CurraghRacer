# Beer Pouring Minigame - Quick Reference Card

## 📊 Default Settings at a Glance

### Game Structure
| Setting | Value |
|---------|-------|
| Total Rounds | 3-5 (random) |
| Beers per Round (Early) | 1-2 (rounds 1-2) |
| Beers per Round (Mid) | 1-3 (rounds 3-4) |
| Beers per Round (Late) | 1-4 (rounds 5+) |
| Round Feedback Duration | 3 seconds |

### Scoring
| Quality | Base Points | Multiplier Applied? |
|---------|-------------|---------------------|
| Perfect | 150 | ✅ Yes |
| Good | 100 | ✅ Yes |
| Acceptable | 50 | ✅ Yes |
| Poor | 20 | ❌ No (flat) |
| Timeout | 0 | ❌ No |

**Performance Multiplier**: `1.0 + (perfectStreak × 0.1)`  
**Scaling**: Unlimited (no cap)

### Target Zones by Beer Type

| Beer Type | Center | Base Tolerance | Patience Time |
|-----------|--------|----------------|---------------|
| Pilsner | 0.875 | ±0.08 | 10-12s |
| Lager | 0.875 | ±0.08 | 10-12s |
| IPA | 0.88 | ±0.05 | 14-16s |
| Ale | 0.88 | ±0.05 | 14-16s |
| Stout | 0.89 | ±0.03 | 18-20s |

**Zone Tightening**: -0.01 tolerance every 10 orders

### Quality Thresholds
| Quality | Condition |
|---------|-----------|
| Perfect | `fillLevel` in [targetMin, targetMax] |
| Good | Within ±0.03 of target zone |
| Acceptable | Within ±0.06 of target zone |
| Poor | Beyond ±0.06 |

### Foam Heights
| Quality | Foam Height Above Liquid |
|---------|-------------------------|
| Perfect | +0.05 |
| Good | +0.08 |
| Acceptable | +0.12 |
| Poor/Overflow | +0.20 |

### Foam Colors
| Beer Type | Hex Color | RGB |
|-----------|-----------|-----|
| Lager | #FFFFFF | (255, 255, 255) |
| Stout | #D2B48C | (210, 180, 140) |
| Ale | #FFFDD0 | (255, 253, 208) |
| IPA | #FAF0E6 | (250, 240, 230) |
| Pilsner | #FFFFF0 | (255, 255, 240) |

## 🎮 Key Component References

### BeerGameController
```
Required Fields:
- beerPrefab: GameObject
- taps: TapConfiguration[4]
- finishPoint: Transform
- minigameCanvasUI: BeerMinigameCanvasUI
```

### TapConfiguration
```
Required Fields:
- tapObject: GameObject
- tapSpoutPosition: Transform
- associatedPourPoint: BeerPourLocation
- tapIndex: int (0-3)
```

### BeerShaderPour
```
Required Fields:
- foamOverflowParticles: ParticleSystem
- pourStreamParticles: ParticleSystem
- targetZoneCanvas: Canvas (WorldSpace)
- targetZoneImage: UI Image
- beerMaterialAsset: Material (BeerCutoff shader)
```

### BeerMinigameCanvasUI
```
Required Arrays:
- tapOrderTexts: TMP_Text[4]
- tapTimerTexts: TMP_Text[4]

Required Fields:
- perfectStreakText: TMP_Text
- roundFeedbackText: TMP_Text
- summaryPanel: GameObject
```

## 🔧 Common File Locations

```
Scripts:
└── MiniGames/BeerMinigame/
    ├── BeerGameController.cs
    ├── BeerShaderPour.cs
    ├── BeerPourLocation.cs
    ├── BeerPourButton.cs
    ├── BeerEnterBoxCollider.cs
    ├── TapConfiguration.cs
    └── BeerMinigameCanvasUI.cs

Shaders:
└── Prefabs/
    └── BeerCutoff.shader

Documentation:
└── MiniGames/BeerMinigame/
    ├── IMPLEMENTATION_SUMMARY.md
    ├── SETUP_GUIDE.md
    └── CUSTOMIZATION_GUIDE.md
```

## 🎯 Testing Quick Checks

**✅ Basic Functionality**
- [ ] Beers spawn at start of round
- [ ] Pour button starts/stops liquid stream
- [ ] Timers count down
- [ ] Beer locks when button released
- [ ] Foam appears after locking

**✅ Scoring System**
- [ ] Perfect pours increase streak
- [ ] Multiplier displays correctly
- [ ] Non-perfect pours reset streak
- [ ] Points calculated with multiplier

**✅ Round Progression**
- [ ] Round feedback shows for 3 seconds
- [ ] Next round starts after feedback
- [ ] Game ends after all rounds
- [ ] Final summary displays

**✅ Visual Effects**
- [ ] Pour stream matches beer color
- [ ] Target zone is visible
- [ ] Foam color matches beer type
- [ ] Overflow particles on poor pours

## 📝 Important Code Locations

### To change round count:
`BeerGameController.StartOrderRoundSystem()` line ~81
```csharp
totalRounds = UnityEngine.Random.Range(3, 6);
```

### To adjust scoring:
`BeerGameController.BeerDone()` line ~242
```csharp
int basePoints = quality switch { ... };
```

### To modify target zones:
`BeerGameController.GenerateNextRoundOrders()` lines ~154-188

### To change foam behavior:
`BeerShaderPour.UpdateFoamAppearance()` line ~208

### To adjust timers:
`BeerGameController.GenerateNextRoundOrders()` lines ~170, 178, 186

## 🚀 Performance Tips

1. **Cache shader property IDs** - Use `Shader.PropertyToID()` instead of strings
2. **Pool particle systems** - Reuse instead of instantiate/destroy
3. **Batch UI updates** - Update timers every 0.1s instead of every frame
4. **Optimize target zone** - Only update when necessary, not every frame

## 📞 Troubleshooting

| Issue | Solution Location |
|-------|------------------|
| Timers not showing | `BeerMinigameCanvasUI.UpdateTapTimer()` |
| Beer doesn't lock | `BeerShaderPour.LockPourAndCalculateQuality()` |
| Particles missing | Check assignments in beer prefab Inspector |
| Round doesn't progress | `BeerGameController.CheckRoundComplete()` |
| Foam not appearing | `BeerShaderPour.UpdateFoamAppearance()` |
| Zone not visible | Check WorldSpace canvas on beer prefab |

---

**Quick Version**: v1.0  
**Last Updated**: November 18, 2025  
**Compatible with**: Unity 2020.3+

