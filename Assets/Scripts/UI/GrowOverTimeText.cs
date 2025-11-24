using UnityEngine;

public class GrowOverTimeText : MonoBehaviour
{
    public float growthRate = 1.0f; // Rate at which the text grows
    public float maxScale = 2.0f;   // Maximum scale limit

    private Vector3 initialScale;

    private void Start()
    {
        initialScale = transform.localScale;
    }

    private void Update()
    {
        if (transform.localScale.x < maxScale)
        {
            float scaleIncrease = growthRate * Time.deltaTime;
            transform.localScale += new Vector3(scaleIncrease, scaleIncrease, scaleIncrease);
            if (transform.localScale.x > maxScale)
            {
                transform.localScale = new Vector3(maxScale, maxScale, maxScale);
            }
        }
    }

    public void OnDisable()
    {
        ResetScale();
    }
    
    public void ResetScale()
    {
        transform.localScale = initialScale;
    }
}
