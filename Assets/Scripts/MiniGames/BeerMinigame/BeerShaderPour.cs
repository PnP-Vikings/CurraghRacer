using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(MeshFilter), typeof(Renderer))]
public class BeerShaderPour : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("Assign the BeerCutoff material asset here")]
    public Material beerMaterialAsset;

    // runtime instance:
    Material beerMatInstance;
    MeshFilter meshFilter;
    Renderer   meshRenderer;
    
    public bool isActive = false; // can be toggled off to pause pouring
    [Range(0,1)] public float fillLevel;    // normalized 0–1
    public float   pourSpeed = 0.5f;       // fill units per second
    public bool beerComplete = false; // is the beer glass full?

    bool isPouring;
    float meshHeight;

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

        // read the mesh-height once (bounds in object space)
        meshHeight = meshFilter.sharedMesh.bounds.size.y;
    }

    void Update()
    {
        // Simple raw-input pour (anywhere on screen)
        isPouring = Input.GetMouseButton(0) || Input.touchCount > 0;

        if (isPouring && fillLevel < 1f && isActive)
            fillLevel += Time.deltaTime * pourSpeed;

        // Always push the latest cutoff
        float cutoff = Mathf.Clamp01(fillLevel) * meshHeight;
        beerMatInstance.SetFloat("_CutoffHeight", cutoff);
        beerComplete = BeerComplete(); // Update the beer complete status
    }
    
    public bool BeerComplete()
    {
        if(fillLevel >= 1f)
        {
           return true; // Beer is full
        }
        else
        {
            return false; // Beer is not full yet
        }

    }

    public void OnPointerDown(PointerEventData e) => isPouring = true;
    public void OnPointerUp  (PointerEventData e) => isPouring = false;
}