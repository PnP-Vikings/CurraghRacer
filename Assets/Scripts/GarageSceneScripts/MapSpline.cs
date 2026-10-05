using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSpline : MonoBehaviour
{   
    [SerializeField] private MapController mapController;
    [SerializeField] Transform playerIconTransform;
    [SerializeField] private GameObject splineToHome;
    [SerializeField] private GameObject splineToShop;
    [SerializeField] private List<Spline> splineFromHomeToShop;
    [SerializeField] private List<Spline> splineFromShopToHome;
    
    [SerializeField] private Spline previousSplineTo;
    [SerializeField] private Spline previousSplineFrom;
    
    [SerializeField] private bool isMovingPlayerIcon = false;
    [SerializeField] private bool hasCompletedSpline = false;
    
    [SerializeField] private List<Spline> ActiveSpline;
    Coroutine MovePlayerIconCoroutine;
    [Tooltip("The speed at which the player icon moves along the spline. Lower values result in faster movement.")]
    [SerializeField] private float playerMoveSpeed = 2f;
    private float startTime;
   
    private void Start()
    {
       ResetSpines();
    }

    public void StartMovingPlayerTowardsShop()
    {
        if (isMovingPlayerIcon)
        {
            return;
        }
        ResetSpines();
        
        if(splineToShop != null)
        {
            splineToShop.gameObject.SetActive(true);
        }
        ActiveSpline = splineFromHomeToShop;
        isMovingPlayerIcon = true;
        previousSplineTo = ActiveSpline[0];
        previousSplineFrom = ActiveSpline[0];
        MovePlayerIconCoroutine = StartCoroutine(MovePlayerIconAlongSpline(ActiveSpline[0], ActiveSpline[1]));
        mapController.UpdatePlayerLocation(MapController.PlayerLocation.Shop);
    }
    
    public void StartMovingPlayerTowardsHome()
    {
        if (isMovingPlayerIcon)
        {
            return;
        }
        ResetSpines();
        if(splineToHome != null)
        {
            splineToHome.gameObject.SetActive(true);
        }
        ActiveSpline = splineFromShopToHome;
        isMovingPlayerIcon = true;
        previousSplineTo = ActiveSpline[0];
        previousSplineFrom = ActiveSpline[0];
        MovePlayerIconCoroutine = StartCoroutine(MovePlayerIconAlongSpline(ActiveSpline[0], ActiveSpline[1]));
        mapController.UpdatePlayerLocation(MapController.PlayerLocation.Home);
    }
    
    private void ResetSpines()
    {
        for(int i = 0; i < splineFromHomeToShop.Count; i++)
        {
            splineFromHomeToShop[i].ResetSpline();
        }
        for(int i = 0; i < splineFromShopToHome.Count; i++)
        {
            splineFromShopToHome[i].ResetSpline();
        }
        
        if(splineToHome != null)
        {
            splineToHome.gameObject.SetActive(false);
        }
        if(splineToShop != null)
        {
            splineToShop.gameObject.SetActive(false);
        }
        
        previousSplineTo = null;
        previousSplineFrom = null;
        hasCompletedSpline = false;
        ActiveSpline = null;
        isMovingPlayerIcon = false;
    }
    
    private void StartMovingPlayerIcon()
    {
   
        print("StartMovingPlayerIcon called");
        if (ActiveSpline == null || ActiveSpline.Count < 2)
        {
            print("ActiveSpline is null or has less than 2 splines");
            return;
        }
        Spline currentSplineFrom = ActiveSpline[0];
        Spline currentSplineTo = ActiveSpline[1];

       
        if (previousSplineTo.HasPlayerPassedSpline() && previousSplineFrom.HasPlayerPassedSpline())
        {
            for (int i = 0; i < ActiveSpline.Count - 1; i++)
            {
                if (ActiveSpline[i].HasPlayerPassedSpline())
                {
                    currentSplineFrom = ActiveSpline[i];
                    currentSplineTo = ActiveSpline[i +1];
                }
                
               


                if (ActiveSpline[i] == null || currentSplineTo == null )
                {
                    hasCompletedSpline = true;
                    isMovingPlayerIcon = false;
                    break;
                }
            }
        }
        
        if(currentSplineTo == previousSplineTo && currentSplineFrom == previousSplineFrom && currentSplineTo.HasPlayerPassedSpline() && currentSplineFrom.HasPlayerPassedSpline() || hasCompletedSpline)
        {
            hasCompletedSpline = true;
            if (playerIconTransform != null && previousSplineTo != null)
            {
                playerIconTransform.position = previousSplineTo.GetTransformPosition();
            }
            isMovingPlayerIcon = false;
            ActiveSpline = null;
        }
        else
        {
            StopCoroutine(MovePlayerIconCoroutine);
            print("Moving player icon from " + currentSplineFrom.name + " to " + currentSplineTo.name);
            MovePlayerIconCoroutine = StartCoroutine(MovePlayerIconAlongSpline(currentSplineFrom, currentSplineTo));
        }
        
        
    }


    
    IEnumerator MovePlayerIconAlongSpline(Spline splineFrom,Spline splineTo)
    {
       
        isMovingPlayerIcon = true;
        Vector3 startPosition = playerIconTransform.position;
        startTime = Time.time;
        float completion = Mathf.Clamp01((Time.time - startTime) / playerMoveSpeed); // Adjust the duration of the movement here
        print($"Completion: {completion}");
        float t = 0f;
        while (completion <1)
        {
            completion = Mathf.Clamp01((Time.time - startTime) / playerMoveSpeed); // Adjust the duration of the movement here
            print($"Completion: {completion}");
            t += Time.deltaTime * playerMoveSpeed; // Adjust the speed of movement here
            playerIconTransform.position = Vector3.Lerp(startPosition, splineTo.GetTransformPosition(), completion);
            if (splineFrom.HasPlayerPassedSpline() && splineTo.HasPlayerPassedSpline())
            {
                previousSplineFrom = splineFrom;
                previousSplineTo = splineTo;
                print("Player has passed spline from " + splineFrom.name + " to " + splineTo.name);
                StartMovingPlayerIcon();
            }
            yield return null;
        }
    }
    
    public bool IsPlayerMoving()
    {
        return isMovingPlayerIcon;
    }
    
    private void OnEnable()
    {
        ResetSpines();

    }

}
