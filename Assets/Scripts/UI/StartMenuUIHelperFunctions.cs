using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class StartMenuUIHelperFunctions : MonoBehaviour
{
    public List<Transform> uiElementsToAnimate;
    public Vector3 offscreenOffset = new Vector3(20f, 0f, 0f);
    public float moveDuration = 0.6f;
    public float rotateDuration = 0.4f;
    public float stagger = 0.08f;
    public Ease moveEase = Ease.OutBounce;

    private List<Vector3> targetLocalPositions;

    private void Awake()
    {
        if (uiElementsToAnimate == null) uiElementsToAnimate = new List<Transform>();
        targetLocalPositions = new List<Vector3>(uiElementsToAnimate.Count);

        // record targets and push elements off-screen (local positions)
        foreach (var t in uiElementsToAnimate)
        {
            if (t == null) continue;
            targetLocalPositions.Add(t.localPosition);
            t.localPosition = t.localPosition + offscreenOffset;
            t.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }

    private async void Start()
    {
        try
        {
            for (int i = 0; i < uiElementsToAnimate.Count; i++)
            {
                var t = uiElementsToAnimate[i];
                if (t == null) continue;

                // create move and rotate tweens
                var moveTween = t.DOLocalMove(targetLocalPositions[i], moveDuration).SetEase(moveEase);
                var rotateTween = t.DOLocalRotate(Vector3.zero, rotateDuration).SetEase(moveEase);

                // play both in a sequence so they run together, then await completion
                var seq = DOTween.Sequence();
                seq.Join(moveTween);
                seq.Join(rotateTween);

                await seq.AsyncWaitForCompletion();
            }
        }
        catch( Exception e )
        {
            Debug.LogError($"Error during UI animation: {e.Message}");
        }
    }
    
    public void QuitGame()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}
