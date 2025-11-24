# Beer Pouring Minigame - Customization Guide

## Common Customizations with Code Examples

### 1. Adjusting Difficulty

#### Change Round Count Range
```csharp
// In BeerGameController.StartOrderRoundSystem()
totalRounds = UnityEngine.Random.Range(5, 8); // 5-7 rounds instead of 3-5
```

#### Adjust Batch Size Progression
```csharp
// In BeerGameController.CalculateRoundSize()
private int CalculateRoundSize(int roundIndex)
{
    if (roundIndex <= 2)
        return Mathf.Max(1, UnityEngine.Random.Range(2, 4)); // Always 2-3 beers early
    else
        return 4; // Always 4 beers in later rounds
}
```

#### Tighten Target Zones Faster
```csharp
// In BeerGameController.GenerateNextRoundOrders()
// Change from -0.01 every 10 orders to -0.02 every 5 orders
if (currentOrderIndex + i >= 5)
{
    float reduction = 0.02f * ((currentOrderIndex + i) / 5);
    // ...rest of code
}
```

---

### 2. Scoring Adjustments

#### Change Point Values
```csharp
// In BeerGameController.BeerDone() and GameCompleted()
int basePoints = quality switch
{
    PourQuality.Perfect => 200,     // Increased from 150
    PourQuality.Good => 120,        // Increased from 100
    PourQuality.Acceptable => 60,   // Increased from 50
    PourQuality.Poor => 10,         // Decreased from 20
    _ => 0
};
```

#### Adjust Multiplier Scaling
```csharp
// In BeerGameController.BeerDone()
// Change from 10% per streak to 15% per streak
performanceMultiplier = 1.0f + (perfectStreak * 0.15f);
```

#### Add Multiplier Cap
```csharp
// In BeerGameController.BeerDone()
performanceMultiplier = Mathf.Min(3.0f, 1.0f + (perfectStreak * 0.1f)); // Cap at 3x
```

---

### 3. Timer Adjustments

#### Make All Timers the Same
```csharp
// In BeerGameController.GenerateNextRoundOrders()
// Replace variable times with fixed time
order.customerPatienceTime = 15f; // All orders get 15 seconds
```

#### Add Time Pressure by Round
```csharp
// In BeerGameController.GenerateNextRoundOrders()
float baseTime = 20f - (currentRoundIndex * 2f); // Decrease 2s per round
order.customerPatienceTime = Mathf.Max(8f, baseTime); // Minimum 8 seconds
```

#### Impatient Customer Modifier
```csharp
// In BeerGameController.GenerateNextRoundOrders()
// Add after setting patience time
if (order.customerName == "Murphy" || order.customerName == "O'Brien")
{
    order.customerPatienceTime *= 0.7f; // 30% less time
    // Compensate with wider zone
    float center = (order.targetZoneMin + order.targetZoneMax) / 2f;
    float tolerance = (order.targetZoneMax - order.targetZoneMin) / 2f * 1.3f;
    order.targetZoneMin = center - tolerance;
    order.targetZoneMax = center + tolerance;
}
```

---

### 4. Visual Customizations

#### Change Target Zone Color Based on Difficulty
```csharp
// In BeerShaderPour.ShowTargetZone()
float zoneSize = targetZoneMax - targetZoneMin;
Color zoneColor;
if (zoneSize > 0.15f)
    zoneColor = new Color(0f, 1f, 0f, 0.3f); // Green for easy
else if (zoneSize > 0.08f)
    zoneColor = new Color(1f, 1f, 0f, 0.3f); // Yellow for medium
else
    zoneColor = new Color(1f, 0f, 0f, 0.3f); // Red for hard

targetZoneImage.color = zoneColor;
```

#### Add Fill Percentage Display
```csharp
// In BeerShaderPour.cs, add field:
public TMPro.TMP_Text fillPercentageText;

// In Update() method:
if (fillPercentageText != null && !isLocked)
{
    fillPercentageText.text = $"{Mathf.RoundToInt(fillLevel * 100)}%";
}
```

#### Pulse Timer When Low
```csharp
// In BeerMinigameCanvasUI.UpdateTapTimer()
if (time <= 5f && isActive)
{
    // Pulse effect when under 5 seconds
    float scale = 1f + Mathf.Sin(Time.time * 10f) * 0.2f;
    tapTimerTexts[tapIndex].transform.localScale = Vector3.one * scale;
    tapTimerTexts[tapIndex].color = Color.Lerp(Color.white, Color.red, 1f - (time / 5f));
}
else
{
    tapTimerTexts[tapIndex].transform.localScale = Vector3.one;
    tapTimerTexts[tapIndex].color = Color.white;
}
```

---

### 5. New Beer Types

#### Add New Beer Type
```csharp
// In BeerShaderPour.cs
public enum BeerType
{
    Lager,
    Ale,
    Stout,
    IPA,
    Pilsner,
    Porter,      // New type
    WheatBeer    // New type
}

// In ProcessBeerType() method:
case BeerType.Porter:
    pourSpeed = 0.38f;
    beerColor = new Color(0.2f, 0.15f, 0.1f); // Dark brown
    break;
case BeerType.WheatBeer:
    pourSpeed = 0.48f;
    beerColor = new Color(1f, 0.95f, 0.7f); // Pale yellow
    break;
```

```csharp
// In BeerGameController.GetFoamColor()
case BeerType.Porter:
    return new Color32(200, 170, 140, 255);
case BeerType.WheatBeer:
    return new Color32(255, 250, 235, 255);
```

```csharp
// In BeerGameController.GenerateNextRoundOrders()
// Add to order pattern logic
else if (orderPattern == 8)
{
    order.beerType = BeerType.Porter;
    order.targetZoneMin = 0.87f - 0.04f;
    order.targetZoneMax = 0.87f + 0.04f;
    order.customerPatienceTime = UnityEngine.Random.Range(16f, 18f);
}
```

---

### 6. Sound Effects Integration

#### Add Pour Sound
```csharp
// In BeerShaderPour.StartPouring()
public void StartPouring()
{
    isPouring = true;
    isActive = true;
    
    // Play pour sound
    if (AudioManager.instance != null)
    {
        AudioManager.instance.pouringPint.start();
    }
    
    // Start particles...
    if (pourStreamParticles != null)
    {
        // ...existing code
    }
}
```

#### Add Quality-Based Feedback Sounds
```csharp
// In BeerGameController.BeerDone()
// After calculating quality, add:
if (AudioManager.instance != null)
{
    switch (quality)
    {
        case PourQuality.Perfect:
            AudioManager.instance.PlaySound("PerfectPour");
            break;
        case PourQuality.Good:
            AudioManager.instance.PlaySound("GoodPour");
            break;
        case PourQuality.Poor:
            AudioManager.instance.PlaySound("PoorPour");
            break;
    }
}
```

---

### 7. Animation Enhancements

#### Add Beer Glass Shake on Poor Pour
```csharp
// In BeerShaderPour.UpdateFoamAppearance()
if (pourQuality == PourQuality.Poor)
{
    StartCoroutine(ShakeGlass());
}

private IEnumerator ShakeGlass()
{
    Vector3 originalPos = transform.localPosition;
    float elapsed = 0f;
    float duration = 0.3f;
    
    while (elapsed < duration)
    {
        float x = Random.Range(-0.05f, 0.05f);
        float z = Random.Range(-0.05f, 0.05f);
        transform.localPosition = originalPos + new Vector3(x, 0, z);
        elapsed += Time.deltaTime;
        yield return null;
    }
    
    transform.localPosition = originalPos;
}
```

#### Animate Streak Counter Growth
```csharp
// In BeerMinigameCanvasUI.UpdatePerfectStreak()
public void UpdatePerfectStreak(int streak, float multiplier)
{
    if (perfectStreakText != null)
    {
        if (streak > 0)
        {
            perfectStreakText.text = $"Perfect Streak: {streak} ({multiplier:F1}x)";
            perfectStreakText.gameObject.SetActive(true);
            
            // Scale pulse on update
            StopCoroutine("PulseStreak");
            StartCoroutine(PulseStreak());
        }
        else
        {
            perfectStreakText.text = "";
            perfectStreakText.gameObject.SetActive(false);
        }
    }
}

private IEnumerator PulseStreak()
{
    float elapsed = 0f;
    while (elapsed < 0.3f)
    {
        float scale = 1f + Mathf.Sin(elapsed * 10f) * 0.3f;
        perfectStreakText.transform.localScale = Vector3.one * scale;
        elapsed += Time.deltaTime;
        yield return null;
    }
    perfectStreakText.transform.localScale = Vector3.one;
}
```

---

### 8. Advanced Features

#### Add Combo Bonus
```csharp
// In BeerGameController, add field:
private int consecutivePerfects;

// In BeerDone(), after updating perfectStreak:
if (quality == PourQuality.Perfect)
{
    consecutivePerfects++;
    
    // Award combo bonus every 5 perfect pours
    if (consecutivePerfects % 5 == 0)
    {
        int comboBonus = 500;
        finalPoints += comboBonus;
        Debug.Log($"COMBO BONUS! +{comboBonus} points!");
        // Display combo bonus in UI
    }
}
else if (quality != PourQuality.Good)
{
    consecutivePerfects = 0;
}
```

#### Track Best Performance
```csharp
// In BeerGameController, add fields:
private int maxStreakThisGame;
private int perfectCount;

// In BeerDone():
maxStreakThisGame = Mathf.Max(maxStreakThisGame, perfectStreak);
if (quality == PourQuality.Perfect)
    perfectCount++;

// In GameCompleted(), display stats:
Debug.Log($"Perfect Pours: {perfectCount}/{orderResults.Count}");
Debug.Log($"Best Streak: {maxStreakThisGame}");
```

#### Save High Score
```csharp
// In BeerGameController.GameCompleted()
int highScore = PlayerPrefs.GetInt("BeerMinigameHighScore", 0);
if (finalScore > highScore)
{
    PlayerPrefs.SetInt("BeerMinigameHighScore", finalScore);
    PlayerPrefs.Save();
    Debug.Log("NEW HIGH SCORE!");
    // Display high score notification
}
```

---

## Performance Optimizations

### Cache Shader Property IDs
```csharp
// In BeerShaderPour.cs, add static fields:
private static readonly int CutoffHeightID = Shader.PropertyToID("_CutoffHeight");
private static readonly int ColorID = Shader.PropertyToID("_Color");
private static readonly int FoamHeightID = Shader.PropertyToID("_FoamHeight");
private static readonly int FoamColorID = Shader.PropertyToID("_FoamColor");

// Replace all SetFloat/SetColor calls:
beerMatInstance.SetFloat(CutoffHeightID, cutoff);
beerMatInstance.SetColor(ColorID, beerColor);
beerMatInstance.SetFloat(FoamHeightID, foamHeight * meshHeight);
beerMatInstance.SetColor(FoamColorID, foamColor);
```

### Object Pooling for Beers
```csharp
// Create a simple pool manager
public class BeerPool : MonoBehaviour
{
    public GameObject beerPrefab;
    public int poolSize = 4;
    private Queue<GameObject> pool = new Queue<GameObject>();
    
    void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject beer = Instantiate(beerPrefab);
            beer.SetActive(false);
            pool.Enqueue(beer);
        }
    }
    
    public GameObject GetBeer(Vector3 position)
    {
        GameObject beer = pool.Count > 0 ? pool.Dequeue() : Instantiate(beerPrefab);
        beer.transform.position = position;
        beer.SetActive(true);
        return beer;
    }
    
    public void ReturnBeer(GameObject beer)
    {
        beer.SetActive(false);
        pool.Enqueue(beer);
    }
}
```

---

**Happy Customizing!** These examples should cover most common modifications you might want to make to the beer pouring minigame.

