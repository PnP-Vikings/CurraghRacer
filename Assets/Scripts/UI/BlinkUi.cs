using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BlinkUi : MonoBehaviour
{
    Image image;
    
    public float blinkInterval = 8f; // Time in seconds for one blink cycle
    public float opacityMin = 0.2f; // Minimum opacity
    public float opacityMax = .6f; // Maximum opacity
    public float currentOpacity;
    private float timer;
    //private bool isVisible = true;
    
    void Start()
    {
       
        if (image == null)
        {
            image = GetComponent<Image>();
        }

         // Ensure the image starts fully visible
        StartCoroutine(Blink());
    }
    
    
    void OnEnable()
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }

         // Ensure the image starts fully visible
        StartCoroutine(Blink());
    }
    IEnumerator Blink()
    {
        
        while (true)
        {
            timer += Time.deltaTime;
            float normalizedTime = (Mathf.Sin((timer / blinkInterval) * Mathf.PI * 2) + 1) / 2; // Normalize to [0, 1]
            currentOpacity = Mathf.Lerp(opacityMin, opacityMax, normalizedTime);
            var color = image.color;
            color.a = currentOpacity;
            image.color = color;
            yield return null;
        }
    }

    

    void OnDisable()
    {
        StopAllCoroutines();
        if (image != null)
        {
            image.enabled = true; // Ensure the image is visible when disabled
        }
    }
}
