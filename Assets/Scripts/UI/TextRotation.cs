using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class TextRotation : MonoBehaviour
{
    public float rotationSpeed = 50f;
    public bool rotateClockwise = true;
    public Vector3 initialRotation;
    
    private void OnEnable()
    {
        initialRotation = transform.eulerAngles;
         int r =  Random.Range(0, 1); 
         rotateClockwise = r == 0 ? true : false;
    }


    void Update()
    {
        transform.Rotate( Vector3.back, rotationSpeed * Time.deltaTime);
    }
    
    private void OnDisable()
    {
        ResetRotation();
    }
    
    public void ResetRotation()
    {
        transform.eulerAngles = initialRotation;
    }
}
