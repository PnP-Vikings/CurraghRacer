using UnityEngine;

public class CloudMovement : MonoBehaviour
{
    public float Speed;
        
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.Translate(Speed, 0, 0);
    }
}
