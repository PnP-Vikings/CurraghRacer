using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class AnimateUiBackToOrigin : MonoBehaviour
{
    public List<Transform> uiElementsToAnimate;
    public Vector3 offscreenOffset = new Vector3(20f, 0f, 0f);
    public float moveDuration = 0.6f;
    public float rotateDuration = 0.4f;
    public float stagger = 0.08f;
    public Ease moveEase = Ease.OutBounce;

    private List<Vector3> targetLocalPositions;
    public bool playOnlyOnStart = false;

    private void Awake()
    {
        InitializePositions();
    }

    private void InitializePositions()
    {
        if (targetLocalPositions != null) return;

        if (uiElementsToAnimate == null) uiElementsToAnimate = new List<Transform>();
        targetLocalPositions = new List<Vector3>(uiElementsToAnimate.Count);

        foreach (var t in uiElementsToAnimate)
        {
            if (t == null) continue;
            targetLocalPositions.Add(t.localPosition);
        }
    }

    private void ResetToOffscreen()
    {
        for (int i = 0; i < uiElementsToAnimate.Count; i++)
        {
            var t = uiElementsToAnimate[i];
            if (t == null) continue;
            t.localPosition = targetLocalPositions[i] + offscreenOffset;
            t.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }

    private async void Start()
    {
        try
        {
            if (!playOnlyOnStart) return;

            ResetToOffscreen();
            await AnimateElements();
        }
        catch(Exception e)
        {
            Debug.LogError($"Error during UI animation: {e.Message}");
        }
    }

    private async void OnEnable()
    {
        try
        {
            if (playOnlyOnStart) return;

            InitializePositions();
            ResetToOffscreen();
            await AnimateElements();
        }
        catch(Exception e)
        {
            Debug.LogError($"Error during UI animation: {e.Message}");
        }
    }

    private async System.Threading.Tasks.Task AnimateElements()
    {
        for (int i = 0; i < uiElementsToAnimate.Count; i++)
        {
            var t = uiElementsToAnimate[i];
            if (t == null) continue;

            var moveTween = t.DOLocalMove(targetLocalPositions[i], moveDuration).SetEase(moveEase);
            var rotateTween = t.DOLocalRotate(Vector3.zero, rotateDuration).SetEase(moveEase);

            var seq = DOTween.Sequence();
            seq.Join(moveTween);
            seq.Join(rotateTween);

            if (i < uiElementsToAnimate.Count - 1)
            {
                await System.Threading.Tasks.Task.Delay((int)(stagger * 1000));
            }
        }
    }
}
