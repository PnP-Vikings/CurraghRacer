using UnityEngine;

public class Rock : MonoBehaviour
{
    
    public float acceleration = 5f;
    public float speed = 0f;
    public float bounceForce = 5f;
    public int maxBounces = 3;
    private int currentBounces = 0;
    private Rigidbody rb;
    public bool isThrown = false;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = transform.forward * speed;
    }
    
    public void Initialize(RockType rockType)
    {
        switch (rockType)
        {
            case RockType.Small:
                acceleration = Random.Range(8f, 15f);
                bounceForce = Random.Range(4f, 6f);
                maxBounces = Random.Range(2,3);
                break;
            case RockType.Medium:
                acceleration = Random.Range(8f, 15f);
                bounceForce = Random.Range(8f, 15f);
                maxBounces = Random.Range(2, 4);
                break;
            case RockType.Large:
                acceleration = Random.Range(7f, 10f);
                bounceForce = Random.Range(4f, 6f);
                maxBounces = Random.Range(4, 8);
                break;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Water"))
        {
            if (currentBounces < maxBounces)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, bounceForce, rb.linearVelocity.z);
                currentBounces++;
            }
            else
            {
                Destroy(gameObject, 2f); // Destroy rock after 2 seconds
            }
        }
        else
        {
            Destroy(gameObject, 2f); // Destroy rock after 2 seconds
        }
    }
    
    void Update()
    {
        if (!isThrown) return;
        speed += acceleration * Time.deltaTime;
        rb.linearVelocity = rb.linearVelocity.normalized * speed;
    }
    
    public void ThrowRock()
    {
        isThrown = true;
        rb.linearVelocity = transform.forward * speed;
    }
    
   
    
    public enum RockType
    {
        Small,
        Medium,
        Large
    }
}
