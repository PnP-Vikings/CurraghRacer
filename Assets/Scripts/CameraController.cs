using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
   public List<Transform> cameraPositions;
   public int currentPositionIndex = 0;
   public Transform ship;
   public bool followPlayer = false;
   public Transform followOffset;
   [SerializeField] private bool isMiniGame = false; // Flag to check if in mini-game mode

   public void Start()
   {
      if (cameraPositions == null || cameraPositions.Count == 0)
      {
         Debug.LogError("Camera positions not set or empty");
         return;
      }
      if (isMiniGame)
      {
         // If in mini-game, do not allow camera movement
         return;
      }
      if(GameManager.Instance == null || RaceManager.Instance == null)
      {
        Debug.LogError("GameManager or RaceManager instance is null!");
         return;
      }
      
     RaceManager.Instance.mainCamera =this.GetComponent<Camera>();
     RaceManager.Instance.startRace.AddListener(SetFollowPlayer);
   }
   
   private void Update()
   {
         if(GameManager.Instance == null || RaceManager.Instance == null)
         {
            return;
         }
      
         
         if (isMiniGame)
         {
            // If in mini-game, do not allow camera movement
            return;
         }
         
         
         if(!GameManager.Instance.GameStarted)
         {
            // If the game has not started or the player is busy, do not allow camera movement
            return;
         }
     
         if (followPlayer) 
         {
            if (ship != null && followOffset != null)
            {
               // Get the current transform position
               Vector3 currentPosition = transform.position;
        
               // Calculate the target position (follow ship's X and Z, keep current Y)
               Vector3 targetPosition = new Vector3(
                  ship.position.x +10 ,  // Follow ship's X with offset
                  currentPosition.y,                 // Keep current height (Y)
                  ship.position.z +5    // Follow ship's Z with offset
               );
               
              // transform.rotation = Quaternion.LookRotation(ship.position - transform.position, Vector3.up);
               // Smoothly move to target position
               float followSpeed = 5f; // Adjust this value for faster/slower following
               transform.position = Vector3.Lerp(currentPosition, targetPosition, followSpeed * Time.deltaTime);
            }
            return;
         }
         if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
         {
            if(currentPositionIndex >= cameraPositions.Count - 1)
            {
               currentPositionIndex = 0;
            }
            else
            {
               currentPositionIndex++;
            }
            MoveCameraToPosition(currentPositionIndex);
         }
   }
   
   public void MoveCameraToPosition(int index)
   {
      if (index < 0 || index >= cameraPositions.Count)
      {
         Debug.LogError("Invalid camera position index");
         return;
      }

      Transform targetPosition = cameraPositions[index];
      transform.position = targetPosition.position;
      transform.rotation = targetPosition.rotation;
      
   }
   
   private void SetFollowPlayer()
   {
      foreach (Transform position in cameraPositions)
      {
         GameObject cameraObject = position.gameObject;
         if (cameraObject.GetComponent<FollowPlayer>() !=null)
         {
            // Check if ships exist before trying to access them
            if (RaceManager.Instance.ships != null && RaceManager.Instance.ships.Count > 0)
            {
                ship = RaceManager.Instance.ships[RaceManager.Instance.ships.Count - 1].transform;
                followOffset = cameraObject.transform;
                followPlayer = true;
            }
            else
            {
                Debug.LogWarning("CameraController: No ships available to follow!");
                followPlayer = false;
            }
         }
        
      }
   }

}
