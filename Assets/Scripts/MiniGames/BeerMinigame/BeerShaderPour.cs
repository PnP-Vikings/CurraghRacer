using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(MeshFilter), typeof(Renderer))]
public class BeerShaderPour : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,Holdable
{
    [Tooltip("Assign the BeerCutoff material asset here")]
    public Material beerMaterialAsset;

    // runtime instance:
    Material beerMatInstance;
    MeshFilter meshFilter;
    Renderer   meshRenderer;
    public Color beerColor = Color.yellow; // color of the beer liquid
    public TMPro.TMP_Text beerTypeText; // TextMeshPro to display beer type
    
    public bool isActive; // can be toggled off to pause pouring
    [Range(0,1)] public float fillLevel;    // normalized 0–1
    public float pourSpeed = 0.5f;       // fill units per second
    public bool beerComplete; // is the beer glass full?
    public bool isPlaced; // is the beer glass placed?
    bool isPouring;
    float meshHeight;
    public BeerType beerType; // Type of beer
    
    [Header("Precision Pouring System")]
    public float targetZoneMin;
    public float targetZoneMax;
    public Color foamColor;
    public PourQuality pourQuality;
    public bool isLocked;
    
    [Header("Particle Systems")]
    public ParticleSystem foamOverflowParticles;
    public ParticleSystem pourStreamParticles;
    
    [Header("Target Zone Visualization")]
    public Canvas targetZoneCanvas;
    public UnityEngine.UI.Image targetZoneImage;
    
    [Header("Foam Appearance")]
    [Range(0.01f, 0.15f)]
    public float foamThickness = 0.05f;
    [Range(0f, 2f)]
    public float desiredFoamHeight = 0.0f;
    
    public void AssignBeerType(BeerType type)
    {
        beerType = type;
        ProcessBeerType();
    }
    
    public void SetBeerTypeText()
    {
        if (beerTypeText != null)
        {
            beerTypeText.text = beerType.ToString();
        }
    }
    
    public void ProcessBeerType()
    {
        switch (beerType)
        {
            case BeerType.Lager:
                pourSpeed = 0.47f; 
                beerColor = Color.yellow;
                foamThickness = 0.04f;
                desiredFoamHeight = 0.7f;
                break;
            case BeerType.Ale:
                pourSpeed = 0.4f; // slower pour speed for Ale
                beerColor = new Color(0.8f, 0.5f, 0.2f); // brownish
                foamThickness = 0.06f;
                desiredFoamHeight = 0.75f;
                break;
            case BeerType.Stout:
                pourSpeed = 0.35f; // even slower pour speed for Stout
                beerColor = new Color(0.1f, 0.1f, 0.1f); // dark
                foamThickness = 0.14f;
                desiredFoamHeight = 1.0f;
                break;
            case BeerType.IPA:
                pourSpeed = 0.45f; // medium pour speed for IPA
                beerColor = new Color(1f, 0.6f, 0.2f); // amber
                foamThickness = 0.05f;
                desiredFoamHeight = 0.8f;
                break;
            case BeerType.Pilsner:
                pourSpeed = 0.5f; // standard pour speed for Pilsner
                beerColor = new Color(1f, 0.9f, 0.5f); // light yellow
                foamThickness = 0.04f;
                desiredFoamHeight = 0.65f;
                break;
            default:
                beerColor = Color.yellow;
                foamThickness = 0.05f;
                desiredFoamHeight = 0.8f;
                break;
        }
        SetBeerTypeText();
    }
    

    void Awake()
    {
        meshFilter    = GetComponent<MeshFilter>();
        meshRenderer  = GetComponent<Renderer>();

        // 1) Instantiate a unique copy of the material:
        beerMatInstance = Instantiate(beerMaterialAsset);
        meshRenderer.material = beerMatInstance;
    }

    void OnEnable()
    {
        // 2) Reset at spawn:
        fillLevel = 0f;
        // ensure the shader shows empty glass immediately
        beerMatInstance.SetFloat("_CutoffHeight", 0f);
        beerMatInstance.SetFloat("_FoamHeight", 0f);
        beerMatInstance.SetFloat("_FoamThickness", foamThickness);
        beerMatInstance.SetColor("_Color", beerColor);
        beerMatInstance.SetColor("_FoamColor", Color.white);

        // read the mesh-height once (bounds in object space)
        meshHeight = meshFilter.sharedMesh.bounds.size.y;
    }
    
    void Update()
    {
        // Skip pouring if beer is locked
        if (isLocked)
            return;

        // Increase fill level while pouring
        if (isPouring && fillLevel < 1f && isActive)
        {
            fillLevel += Time.deltaTime * pourSpeed;
        }
        
        // CRITICAL: Always update shader in real-time (with null check)
        if (beerMatInstance != null)
        {
            beerMatInstance.SetColor("_Color", beerColor);
            
            // This line makes the beer rise in real-time as you pour
            float cutoff = Mathf.Clamp01(fillLevel) * meshHeight;
            beerMatInstance.SetFloat("_CutoffHeight", cutoff);
           // beerMatInstance.SetFloat("_FoamHeight", foamHeight * meshHeight);
            beerMatInstance.SetFloat("_FoamThickness", foamThickness);
        }
        
        beerComplete = BeerComplete(); // Update the beer complete status
    }
    
    public bool BeerComplete()
    {
        if(isLocked)
        {
            return true; // Beer is locked, considered complete
        }
        else
        {
            return false; // Beer is not locked, not complete
        }
    }
    
    
    
    public void StopPouring()
    {
        isPouring = false;
        isActive = false;
        
        // Stop pour stream particles
        if (pourStreamParticles != null && pourStreamParticles.isPlaying)
        {
            pourStreamParticles.Stop();
        }
    }
    

    public void StartPouring()
    {
        isPouring = true;
        isActive = true;
        
        // Start pour stream particles and set color dynamically
        if (pourStreamParticles != null)
        {
            Debug.Log($"Starting pour particles with color {beerColor} for beer at {transform.position}");
            var main = pourStreamParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(beerColor);
            pourStreamParticles.Play();
        }
    }

    public void OnPointerDown(PointerEventData e) => isPouring = true;
    public void OnPointerUp  (PointerEventData e) => isPouring = false;
    public void SetPositionOnHold(Vector3 position)
    {
        transform.parent.position  = position;
    }
    public Vector3 GetPositionOnHold(out Vector3 position)
    {
        position = transform.parent.position;
        return position;
    }

    public void SetOrderTarget(float min, float max, Color foam, Vector3 streamOrigin)
    {
        targetZoneMin = min;
        targetZoneMax = max;
        foamColor = foam;
        
        Debug.Log($"SetOrderTarget called - Zone: {min:F2} to {max:F2}, Stream origin: {streamOrigin}");
        
        // Position pour stream particles at tap spout
        if (pourStreamParticles != null)
        {
            pourStreamParticles.transform.position = streamOrigin;
            
            // Ensure particle system is stopped and ready
            if (pourStreamParticles.isPlaying)
            {
                pourStreamParticles.Stop();
            }
            
            Debug.Log($"Particles positioned at: {streamOrigin}");
        }
        else
        {
            Debug.LogWarning($"pourStreamParticles is NULL for beer at {transform.position}");
        }
        
        // Show target zone visualization
        ShowTargetZone();
    }

    
    public void LockPourAndCalculateQuality()
    {
        isLocked = true;
        isPouring = false;
        isActive = false;
        
        // Stop pour stream
        if (pourStreamParticles != null && pourStreamParticles.isPlaying)
        {
            pourStreamParticles.Stop();
        }
        
        // Calculate quality based on fill level vs target zone
        if (fillLevel >= targetZoneMin && fillLevel <= targetZoneMax)
        {
            pourQuality = PourQuality.Perfect;
        }
        else if (fillLevel >= targetZoneMin - 0.03f && fillLevel <= targetZoneMax + 0.03f)
        {
            pourQuality = PourQuality.Good;
        }
        else if (fillLevel >= targetZoneMin - 0.06f && fillLevel <= targetZoneMax + 0.06f)
        {
            pourQuality = PourQuality.Acceptable;
        }
        else
        {
            pourQuality = PourQuality.Poor;
        }
        
        beerComplete = true;
        Debug.Log($"Pour locked at level {fillLevel:F2}, Quality: {pourQuality}");
        
        // Start foam animation from current fill level
        StartCoroutine(AnimateFoamRise());
    }

   // csharp
private IEnumerator AnimateFoamRise()
{
    float foamStartHeight = fillLevel;
    float foamDuration = 0.5f;
    float elapsed = 0f;
    
    // Calculate how much foam can actually rise based on quality
    float maxFoamRise = pourQuality switch
    {
        PourQuality.Perfect => desiredFoamHeight, // Full foam rise
        PourQuality.Good => desiredFoamHeight * 0.7f,
        PourQuality.Acceptable => desiredFoamHeight * 0.4f,
        PourQuality.Poor => desiredFoamHeight * 0.2f,
        _ => 0f
    };
    
    // Cap foam to top of the mesh to avoid sending values beyond shader capacity
    float finalFoamHeight = foamStartHeight + maxFoamRise;
    bool cappedToTop = false;
    if (finalFoamHeight > 1.0f)
    {
        finalFoamHeight = 1.0f;
        cappedToTop = true;
    }
    
    // Animate foam rising
    while (elapsed < foamDuration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / foamDuration;
        
        if (cappedToTop)
        {
            // If capped, use ease-out for a softer finish
            t = Mathf.Sin(t * Mathf.PI * 0.5f);
        }
        else
        {
            // Standard ease-in-out
            t = t * t * (3f - 2f * t);
        }
        
        float currentFoamHeight = Mathf.Lerp(foamStartHeight, finalFoamHeight, t);
        
        if (currentFoamHeight > 1.0f)
            currentFoamHeight = 1.0f;
        
        if (beerMatInstance != null)
        {
            // Ensure shader gets a clamped height in world/object units
            beerMatInstance.SetFloat("_FoamHeight", Mathf.Clamp01(currentFoamHeight) * meshHeight);
            beerMatInstance.SetColor("_FoamColor", foamColor);
            
        }
        
        yield return null;
    }
    
    // Trigger overflow particles if foam exceeded the target or reached the top
    if ((finalFoamHeight > targetZoneMax || finalFoamHeight >= 1.0f) && foamOverflowParticles != null)
    {
        var main = foamOverflowParticles.main;
        main.startColor = new ParticleSystem.MinMaxGradient(foamColor);
        foamOverflowParticles.Play();
    }
}


    public void ShowTargetZone()
    {
        if (targetZoneCanvas != null && targetZoneImage != null)
        {
            // Calculate positions based on mesh height
            float minHeight = targetZoneMin * meshHeight;
            float maxHeight = targetZoneMax * meshHeight;
            float zoneHeight = maxHeight - minHeight;
            
            // Position and scale the zone overlay
            RectTransform rectTransform = targetZoneCanvas.transform as RectTransform;
            rectTransform.anchoredPosition = new Vector2(0, minHeight);
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, zoneHeight);
            
            // Set color to green with transparency
            targetZoneImage.color = new Color(0f, 1f, 0f, 0.3f);
            targetZoneCanvas.gameObject.SetActive(true);
        }
    }


}

public enum BeerType
{
    Lager,
    Ale,
    Stout,
    IPA,
    Pilsner
}

public enum PourQuality
{
    Perfect,
    Good,
    Acceptable,
    Poor
}
