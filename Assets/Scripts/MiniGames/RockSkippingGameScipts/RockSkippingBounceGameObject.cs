using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class RockSkippingBounceGameObject : MonoBehaviour ,Bounceable
{
   public float bounceForce = 5f;
   public float movementSpeed = 10f;
   public Collider col;
   

   public void Awake()
   {
       col = GetComponent<Collider>();
   }

   public void OnTriggerEnter(Collider other)
   {
       Debug.Log("Collision detected with " + other.gameObject.name);
       if (other.gameObject.GetComponent<Rock>() != null)
       {
           Debug.Log("Bounce applied to rock");
           ApplyBounceToTarget(other.gameObject);
       }
   }
   
   
   
    
    public void ApplyBounceToTarget(GameObject gobject)
    {
        Rigidbody rb = gobject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(Vector3.up * bounceForce, ForceMode.Impulse);
        }
    }
}
