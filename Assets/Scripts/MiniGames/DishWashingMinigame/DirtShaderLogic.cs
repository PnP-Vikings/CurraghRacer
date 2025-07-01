using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(MeshFilter), typeof(Renderer))]
public class DirtShaderLogic : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Tooltip("Drag in your DirtCutoff shader material here")]
    public Material dirtMaterialAsset;

    Material   matInstance;
    MeshFilter mf;
    Renderer   rend;

    [Range(0f, 1f)]
    public float dirtLevel = 1f;      // 1 = fully dirty, 0 = fully clean
    public float cleanSpeed = 1f;     // how much dirtLevel falls per second

    bool isClean = false; // true while mouse/touch is down
    bool  isCleaning = false;
    float meshHeight;

    void Awake()
    {
        mf   = GetComponent<MeshFilter>();
        rend = GetComponent<Renderer>();

        // Unique copy of the material so each plate is independent
        matInstance      = Instantiate(dirtMaterialAsset);
        rend.material    = matInstance;
    }

    void OnEnable()
    {
        // Reset on spawn
        dirtLevel        = 1f;
        meshHeight       = mf.sharedMesh.bounds.size.y;
        // Show all dirt immediately
        matInstance.SetFloat("_CutoffHeight", meshHeight);
    }

    void Update()
    {

        if (dirtLevel <= 0f)
        {
            if (!isClean)
            {
                isClean = true;
                // Hide all dirt
                matInstance.SetFloat("_CutoffHeight", 0f);
            }
            return; // No need to update further
        }
       

        // Compute a world-space cutoff from 0→meshHeight
        float cutoff = dirtLevel * meshHeight;
        matInstance.SetFloat("_CutoffHeight", cutoff);
    }
    
    public void CleanDirt()
    {
        dirtLevel -= .15f;
    }

    
    public bool IsClean()
    {
        return isClean;
    }
    
    public void OnPointerDown(PointerEventData e) => isCleaning = true;
    public void OnPointerUp  (PointerEventData e) => isCleaning = false;
}