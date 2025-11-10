using UnityEngine;
using UnityEngine.Events;
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
    
    public bool isActive = false; // can be toggled off to pause pouring
    [Range(0,1)] public float fillLevel;    // normalized 0–1
    public float   pourSpeed = 0.5f;       // fill units per second
    public bool beerComplete = false; // is the beer glass full?
    public bool isPlaced = false; // is the beer glass placed?
    bool isPouring;
    float meshHeight;
    public BeerType beerType; // Type of beer
    
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
        // Implement logic based on beer type
        switch (beerType)
        {
            case BeerType.Lager:
                pourSpeed = 0.47f; 
                beerColor = Color.yellow;
                break;
            case BeerType.Ale:
                pourSpeed = 0.4f; // slower pour speed for Ale
                beerColor = new Color(0.8f, 0.5f, 0.2f); // brownish
                break;
            case BeerType.Stout:
                pourSpeed = 0.35f; // even slower pour speed for Stout
                beerColor = new Color(0.1f, 0.1f, 0.1f); // dark
                break;
            case BeerType.IPA:
                pourSpeed = 0.45f; // medium pour speed for IPA
                beerColor = new Color(1f, 0.6f, 0.2f); // amber
                break;
            case BeerType.Pilsner:
                pourSpeed = 0.5f; // standard pour speed for Pilsner
                beerColor = new Color(1f, 0.9f, 0.5f); // light yellow
                break;
            default:
                beerColor = Color.yellow;
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
        beerMatInstance.SetColor("_Color", beerColor);

        // read the mesh-height once (bounds in object space)
        meshHeight = meshFilter.sharedMesh.bounds.size.y;
    }
    
    public void PourAuto()
    {
        if (!beerComplete)
        {
            fillLevel += pourSpeed/4f;
            Debug.Log("Auto Pouring... Fill Level: " + fillLevel);
        }
    }

    void Update()
    {
        /*// Simple raw-input pour (anywhere on screen)
        isPouring = Input.GetMouseButton(0) || Input.touchCount > 0;*/

        if (isPouring && fillLevel < 1f && isActive)
            fillLevel += Time.deltaTime * pourSpeed;
        beerMatInstance.SetColor("_Color", beerColor);
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
    
    
    
    public void StopPouring()
    {
        isPouring = false;
        isActive = false;
    }
    

    public void StartPouring()
    {
        isPouring = true;
        isActive = true;
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


}

public enum BeerType
{
    Lager,
    Ale,
    Stout,
    IPA,
    Pilsner
}