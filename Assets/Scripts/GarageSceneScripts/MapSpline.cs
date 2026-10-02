using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSpline : MonoBehaviour
{
    [SerializeField] Transform playerIconTransform;
    [SerializeField] private List<Spline> splineFromHomeToShop;
    [SerializeField] private List<Spline> splineFromShopToHome;
    
    [SerializeField] private Spline previousSplineTo;
    [SerializeField] private Spline previousSplineFrom;
    
    [SerializeField] private bool hasCompletedSpline = false;
    
    Coroutine MovePlayerIconCoroutine;

    [SerializeField] private float playerMoveSpeed = 2f;
    
   
    private void Start()
    {
        for(int i = 0; i < splineFromHomeToShop.Count; i++)
        {
            splineFromHomeToShop[i].ResetSpline();
        }
        for(int i = 0; i < splineFromShopToHome.Count; i++)
        {
            splineFromShopToHome[i].ResetSpline();
        }
        
        /*
        previousSplineTo = splineFromHomeToShop[1];
        previousSplineFrom = splineFromHomeToShop[0];
        */
        
        MovePlayerIconCoroutine = StartCoroutine(MovePlayerIconAlongSpline(splineFromHomeToShop[0], splineFromHomeToShop[1]));
       
        
       
    }

    private void StartMovingPlayerIcon()
    {
       
        print("StartMovingPlayerIcon called");
        Spline currentSplineFrom = splineFromHomeToShop[0];
        Spline currentSplineTo = splineFromHomeToShop[1];

       
        if (previousSplineTo.HasPlayerPassedSpline() && previousSplineFrom.HasPlayerPassedSpline())
        {
            for (int i = 0; i < splineFromHomeToShop.Count - 1; i++)
            {
                if (splineFromHomeToShop[i].HasPlayerPassedSpline())
                {
                    currentSplineFrom = splineFromHomeToShop[i];
                    currentSplineTo = splineFromHomeToShop[i +1];
                }
                
               


                if (splineFromHomeToShop[i] == null || currentSplineTo == null )
                {
                    hasCompletedSpline = true;
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
        Vector3 startPosition = playerIconTransform.position;
        float t = 0f;
        while (t < 1.5f)
        {
            t += Time.deltaTime * playerMoveSpeed; // Adjust the speed of movement here
            playerIconTransform.position = Vector3.Lerp(startPosition, splineTo.GetTransformPosition(), t);
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
    private void OnEnable()
    {
        for(int i = 0; i < splineFromHomeToShop.Count; i++)
        {
            splineFromHomeToShop[i].ResetSpline();
        }
        for(int i = 0; i < splineFromShopToHome.Count; i++)
        {
            splineFromShopToHome[i].ResetSpline();
        }
        
        previousSplineTo = null;
        previousSplineFrom = null;
        hasCompletedSpline = false;
        

    }

}
