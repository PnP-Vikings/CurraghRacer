using System.Collections;
using UnityEngine;

public class BlinkTxt : MonoBehaviour
{
  TMPro.TMP_Text txt;
  
  
  public float blinkInterval = 8f; // Time in seconds for one blink cycle
  public float opacityMin = 0.2f; // Minimum opacity
  public float opacityMax = .6f; // Maximum opacity
  public float currentOpacity;
  private float timer;
  private bool isVisible = true;
    
  void Start()
  {
       
    if (txt == null)
    {
      txt = GetComponent<TMPro.TMP_Text>();
    }

    // Ensure the image starts fully visible
    StartCoroutine(Blink());
  }
    
    
  void OnEnable()
  {
    if (txt == null)
    {
      txt = GetComponent<TMPro.TMP_Text>();
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
      var color = txt.color;
      color.a = currentOpacity;
      txt.color = color;
      yield return null;
    }
  }

    

  void OnDisable()
  {
    StopAllCoroutines();
    if (txt != null)
    {
      txt.enabled = true; // Ensure the image is visible when disabled
    }
  }
}
