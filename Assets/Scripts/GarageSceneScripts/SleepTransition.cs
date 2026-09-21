using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SleepTransition : MonoBehaviour
{
    [SerializeField] private Image transitionImage;
    [SerializeField] private GameObject transitionCanvas;
    
    [SerializeField] private float closeDuration =.4f;
    [SerializeField] private float openDuration = 2f;
    [SerializeField] private AnimationCurve curve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool transitionActive = false;
    private Material material;

    private static readonly int Radius =
        Shader.PropertyToID("_Radius");

    private void Awake()
    {
        // Create an instance so we don't modify the original asset
        material = new Material(transitionImage.material);
        transitionImage.material = material;

        // Start completely open
        material.SetFloat(Radius, 1.5f);
    }
    
    private void OnEnable()
    {
        transitionCanvas.SetActive(false);
        if(GameManager.Instance != null)
        {
           GameManager.Instance.onSleep.AddListener(StartSleepTransition); 
        }
    }
    private void OnDisable()
    {
        if(GameManager.Instance != null)
        {
            GameManager.Instance.onSleep.RemoveListener(StartSleepTransition);
        }
        transitionCanvas.SetActive(false);
    }
    
    public void StartSleepTransition()
    {
        if (transitionActive)
            return;
        transitionActive = true;
        StartCoroutine(Close());
        
        DOVirtual.DelayedCall(closeDuration, () =>
        {
            StartCoroutine(Open());
        });
        
    }

    public IEnumerator Close()
    {
        transitionCanvas.SetActive(true);
        float timer = 0f;

        while (timer < closeDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / closeDuration);
            t = curve.Evaluate(t);

            material.SetFloat(
                Radius,
                Mathf.Lerp(1.5f, 0f, t)
            );

            yield return null;
        }

        material.SetFloat(Radius, 0f);
    }

    public IEnumerator Open()
    {
        float timer = 0f;

        while (timer < openDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / openDuration);
            t = curve.Evaluate(t);

            material.SetFloat(
                Radius,
                Mathf.Lerp(0f, 1.5f, t)
            );

            yield return null;
        }

        material.SetFloat(Radius, 1.5f);
        transitionCanvas.SetActive(false);
        transitionActive = false;
    }
}