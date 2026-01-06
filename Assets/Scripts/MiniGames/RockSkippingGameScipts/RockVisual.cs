using System;
using DG.Tweening;
using UnityEngine;

public class RockVisual : MonoBehaviour
{
    private GameObject rockVisualObject;
    [SerializeField] private bool isHovered = false;
    [SerializeField] private bool isSelected = false;
    [SerializeField] private bool isInteractable = true;
    
    public Rock rockData;
    private Sequence hoverSequence;
    private Vector3 originalScale;
    
    public event Action<RockVisual> OnRockHoverEnter;
    public event Action<RockVisual> OnRockHoverExit;
    public event Action<RockVisual> OnRockClicked;
    
    private Material rockMaterial;
    private Color originalColor;
    private Color hoverColor = Color.yellow;
    private Color selectedColor = Color.green;
    
    private bool isInitialized = false;
    
    public bool IsInteractable => isInteractable && !isSelected;
    
    public void Initialize(Rock Data)
    {
        rockData = Data;
    }
    
    /// <summary>
    /// Call this after the object is instantiated in the scene
    /// </summary>
    public void SetupAfterInstantiation()
    {
        if (isInitialized) return;
        
        rockVisualObject = this.gameObject;
        originalScale = transform.localScale;
        
        // Get material for color changes (only works on instantiated objects)
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rockMaterial = rend.material;
            originalColor = rockMaterial.color;
        }
        else
        {
            Debug.LogWarning($"RockVisual {gameObject.name} has no Renderer component! Color changes won't work.");
        }
        
        // Ensure collider exists for raycast
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
            Debug.Log($"Added BoxCollider to {gameObject.name} for interaction");
        }
        
        isInitialized = true;
    }
    
    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        if (!interactable)
        {
            ResetVisuals();
        }
    }
    
    /// <summary>
    /// Called when pointer enters this rock (from RockSelectionManager)
    /// </summary>
    public void OnPointerEnter()
    {
        if (!isInteractable) return; // Only check base interactability, not selection state
        
        isHovered = true;
        OnRockHoverEnter?.Invoke(this);
        
        // Only play hover animation if not already selected
        if (!isSelected)
        {
            PlayHoverAnimation();
        }
    }
    
    /// <summary>
    /// Called when pointer exits this rock (from RockSelectionManager)
    /// </summary>
    public void OnPointerExit()
    {
        if (!isInteractable) return; // Only check base interactability
        
        isHovered = false;
        OnRockHoverExit?.Invoke(this);
        
        // Only stop hover animation if not selected
        if (!isSelected)
        {
            StopHoverAnimation();
        }
    }
    
    /// <summary>
    /// Called when this rock is clicked (from RockSelectionManager)
    /// </summary>
    public void OnPointerClick()
    {
        if (!isInteractable) return; // Only check base interactability, not selection state

        if (isSelected)
        {
            // Deselect the rock
            isSelected = false;
            OnRockClicked?.Invoke(this);
            ResetVisuals();
            return;
        }
        
        // Select the rock
        isSelected = true;
        OnRockClicked?.Invoke(this);
        PlaySelectionAnimation();
    }
    
    private void PlayHoverAnimation()
    {
        // Kill any existing sequence
        hoverSequence?.Kill();
        
        hoverSequence = DOTween.Sequence();
        hoverSequence.Append(transform.DOScale(originalScale * 1.2f, 0.3f).SetEase(Ease.OutBack));
        
        // Change color to hover color
        if (rockMaterial != null)
        {
            rockMaterial.DOColor(hoverColor, 0.3f);
        }
    }
    
    private void StopHoverAnimation()
    {
        // Kill hover sequence
        hoverSequence?.Kill();
        
        // Return to original scale and color
        transform.DOScale(originalScale, 0.2f).SetEase(Ease.InBack);
        
        if (rockMaterial != null)
        {
            rockMaterial.DOColor(originalColor, 0.2f);
        }
    }
    
    private void PlaySelectionAnimation()
    {
        // Kill any existing sequence
        hoverSequence?.Kill();
        
        // Scale up and change color
        transform.DOScale(originalScale * 1.3f, 0.3f).SetEase(Ease.OutBack);
        
        if (rockMaterial != null)
        {
            rockMaterial.DOColor(selectedColor, 0.3f);
        }
        
        // Add a gentle floating/rotating animation
        hoverSequence = DOTween.Sequence();
        hoverSequence.Append(transform.DOLocalMoveY(transform.localPosition.y + 0.2f, 0.5f).SetEase(Ease.InOutSine))
            .SetLoops(-1, LoopType.Yoyo);
    }
    
    public void ResetVisuals()
    {
        isHovered = false;
        isSelected = false;
        
        hoverSequence?.Kill();
        transform.DOScale(originalScale, 0.2f);
        
        if (rockMaterial != null)
        {
            rockMaterial.DOColor(originalColor, 0.2f);
        }
    }
    
    private void OnDestroy()
    {
        hoverSequence?.Kill();
    }
}
