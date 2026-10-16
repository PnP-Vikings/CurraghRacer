using System;
using UnityEngine;

public class Spline : MonoBehaviour
{
   [SerializeField] private Transform splineTransform;
   [SerializeField] private bool playerHasPassed = false;

   void OnEnable()
   {
      splineTransform = GetComponent<Transform>();
   }
   
   public void PlayerHasPassedSpline()
   {
      playerHasPassed = true;
   }

   public void ResetSpline()
   {
      playerHasPassed = false;
   }


   private void OnTriggerEnter2D(Collider2D other)
   {
      if (other.GetComponent<PlayerMap>() != null)
      {
         PlayerHasPassedSpline();
      }
   }


   public bool HasPlayerPassedSpline()
   {
      return playerHasPassed;
   }
   
   public Vector3 GetTransformPosition()
   {
      return transform.position;
   }
   
}
