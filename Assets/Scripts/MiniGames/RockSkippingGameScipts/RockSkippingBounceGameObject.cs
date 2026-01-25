using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class RockSkippingBounceGameObject : MonoBehaviour ,Bounceable
{
   public float bounceForce = 5f;
   public float movementSpeed = 10f;
   public Collider col;
   [SerializeField] private bool bounceAble = true;

   public void Awake()
   {
       col = GetComponent<Collider>();
   }

   public void OnTriggerEnter(Collider other)
   {
       Debug.Log("Collision detected with " + other.gameObject.name);
       if (other.gameObject.GetComponent<Rock>() != null && bounceAble)
       {
           ApplyBounceToTarget(other.gameObject);
       }
   }

   public void OnCollisionEnter(Collision other)
   {
       Debug.Log("Collision detected with " + other.gameObject.name);
       if (other.gameObject.GetComponent<Rock>() != null && bounceAble)
       {
           ApplyBounceToTarget(other.gameObject);
       }
   }
    
  
    public void ApplyBounceToTarget(GameObject gobject)
    {
        Rock rock = gobject.GetComponent<Rock>();
        if (rock != null)
        {
            rock.HandleExternalBounce(bounceForce);
        }
    }
}
