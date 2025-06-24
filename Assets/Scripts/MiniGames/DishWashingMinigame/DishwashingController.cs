//using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DishwashingController : MonoBehaviour
{
    [Tooltip("Drag your Sponge prefab here")]
    public GameObject spongePrefab;

    [Tooltip("Which layers count as 'plates'")]
    public LayerMask plateLayerMask;

    [Tooltip("Maximum distance (in world units) the sponge will follow the cursor")]
    public float maxDistance = 5f;

    [Tooltip("How high above the plate surface the sponge should hover")]
    public float hoverHeight = 0.01f;

    public GameObject PlatePrefab; // Reference to the Plate prefab
    
    public List<PlateLogic> plates = new List<PlateLogic>(); // List to hold spawned plates
    
    public List<PlateLogic> platesCleaned = new List<PlateLogic>(); // List to hold plates that need cleaning
    
    GameObject spongeInstance;

    public PlateCleanPosition plateCleanPosition; // Reference to the PlateCleanPosition script
    
    public Transform spawnPoint,cleanPosition,finishClean; // Point where the sponge is spawned

    [SerializeField] private int spawnCount = 5;

    //Coroutine SpongeAudioCoroutine;
    
    void OnEnable()
    {
        if (spongePrefab == null)
        {
            Debug.LogError("Please assign a spongePrefab!");
            enabled = false;
            return;
        }

        // Only create one sponge
        if (spongeInstance == null)
            spongeInstance = Instantiate(spongePrefab);
        
        
        SpawnPlates();
        plateCleanPosition.onPlateCleaned.AddListener(PlateCleaned); // Subscribe to the BeerDone event
        MovePlateToCleanPosition();
        
    }

    void Update()
    {
        if (spongeInstance == null) return;

        // Ray from camera through cursor/touch
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Raycast against plate colliders, up to maxDistance
        if (Physics.Raycast(ray, out hit, maxDistance, plateLayerMask))
        {
            // Place sponge at the hit point + hoverHeight
            Vector3 targetPos = hit.point;
            targetPos.y += hoverHeight;
            spongeInstance.transform.position = targetPos;

            //PlaySpongeAudio();
            //SpongeAudioCoroutine = StartCoroutine(PlaySpongeAudio());
            //PlaykaSpongeAudio();
        }
        else
        {
            // Optional: clamp to the farthest point along the ray
            Vector3 fallback = ray.GetPoint(maxDistance);
            fallback.y = Mathf.Max(fallback.y, hoverHeight);
            spongeInstance.transform.position = fallback;
        }
    }

    //public void PlaykaSpongeAudio()
    //{
    //    FMOD.Studio.EventInstance kaSponge;
    //    kaSponge = FMODUnity.RuntimeManager.CreateInstance("event:/Mini Games/kaSponge");
    //    kaSponge.start();
    //}
    //IEnumerator PlaySpongeAudio()
    //{
    //    FMOD.Studio.EventInstance kaSponge;
    //    kaSponge = FMODUnity.RuntimeManager.CreateInstance("event:/Mini Games/kaSponge");
    //    kaSponge.start();
    //    yield return new WaitForSeconds(1f);
    //}

    public void MovePlateToCleanPosition()
    {
        if (plateCleanPosition.plateLogic == null  && plates.Count > 0)
        {
            plates[0].transform.position = cleanPosition.position; // Move the plate to the clean position
            plates[0].transform.rotation = cleanPosition.rotation; // Set the rotation to match the clean position
        }
    }
    
    public void SpawnPlates()
    {
        for (int i = 0; i < spawnCount; i++) // Spawn 5 plates
        {
            GameObject plateObject = Instantiate(PlatePrefab, spawnPoint.position, Quaternion.identity);
            PlateLogic plateLogic = plateObject.GetComponent<PlateLogic>();
            if (plateLogic != null)
            {
                plates.Add(plateLogic); // Add the plate to the list
                Debug.Log("Spawned plate: " + plateObject.name);
            }
            else
            {
                Debug.LogError("PlateLogic component not found on the spawned plate prefab.");
            }
        }
    }
    public void PlateCleaned()
    {
        plateCleanPosition.plateLogic.transform.position = finishClean.position; // Move the plate to the finish clean position
        platesCleaned.Add(plateCleanPosition.plateLogic); // Add the cleaned plate to the list
        plates.Remove(plateCleanPosition.plateLogic); // Remove the cleaned plate from the plates list
        plateCleanPosition.plateLogic = null; // Clear the plateLogic reference in PlateCleanPosition
        MovePlateToCleanPosition(); // Move the next plate to the clean position

        FMOD.Studio.EventInstance MovePlateAudio;
        MovePlateAudio = FMODUnity.RuntimeManager.CreateInstance("event:/Kitchen/MovePlate");
        MovePlateAudio.start();

        MinigameFinished();
    }
    
    public void MinigameFinished()
    {
       if(platesCleaned.Count == spawnCount)
       {
           SceneManager.LoadScene("RaceScene"); 
           if(GameManager.Instance != null)
           {
               GameManager.Instance.PlayerWorked();
           }
       }
      
    }
}