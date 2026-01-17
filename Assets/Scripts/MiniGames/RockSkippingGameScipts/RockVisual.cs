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
        if (!isInteractable) return;
        if (AudioManager.instance != null )
        {
            AudioManager.instance.UIClick1.start();
        }
        
        // Just invoke the event - let the controller decide what to do
        OnRockClicked?.Invoke(this);
    }

    public void Select()
    {
        if (isSelected) return;
        
        isSelected = true;
        PlaySelectionAnimation();
    }
    
    public void Deselect()
    {
        if (!isSelected) return;
        
        isSelected = false;
        
        // If still hovering, play hover animation, otherwise reset completely
        if (isHovered)
        {
            PlayHoverAnimation();
        }
        else
        {
            ResetVisuals();
        }
    }

    public void SetSelectedVisuals()
    {
        if (isSelected)
        {
            Deselect();
        }
        else
        {
            Select();
        }
    }
    
    private void PlayHoverAnimation()
    {
        // Kill any existing sequence
        if (hoverSequence != null && hoverSequence.IsActive())
        {
            hoverSequence.Kill(false);
        }
        
        hoverSequence = DOTween.Sequence();
        hoverSequence.Append(transform.DOScale(originalScale * 1.2f, 0.3f).SetEase(Ease.OutBack));
        
        // Change color to hover color
        if (rockMaterial != null)
        {
            hoverSequence.Join(rockMaterial.DOColor(hoverColor, 0.3f));
        }
        
        if (AudioManager.instance != null )
        {
            AudioManager.instance.placeTurf.start();
        }
    }
    
    private void StopHoverAnimation()
    {
        // Kill hover sequence
        if (hoverSequence != null && hoverSequence.IsActive())
        {
            hoverSequence.Kill(false);
        }
        
        // Create new sequence to return to original state
        hoverSequence = DOTween.Sequence();
        hoverSequence.Append(transform.DOScale(originalScale, 0.2f).SetEase(Ease.InBack));
        
        if (rockMaterial != null)
        {
            hoverSequence.Join(rockMaterial.DOColor(originalColor, 0.2f));
        }
    }
    
    private void PlaySelectionAnimation()
    {
        // Kill any existing sequence
        if (hoverSequence != null && hoverSequence.IsActive())
        {
            hoverSequence.Kill(false);
        }
        
        // Change color immediately
        if (rockMaterial != null)
        {
            rockMaterial.DOColor(selectedColor, 0.3f);
        }
        
        // Add a gentle floating/rotating animation
        hoverSequence = DOTween.Sequence();
        hoverSequence.Append(transform.DOScale(originalScale * 1.3f, 0.3f).SetEase(Ease.OutBack))
            .Append(transform.DOLocalMoveY(transform.localPosition.y + 0.2f, 0.5f).SetEase(Ease.InOutSine))
            .SetLoops(-1, LoopType.Yoyo);
    }
    
    public void ResetVisuals()
    {
        isHovered = false;
        isSelected = false;
        
        // Kill any existing sequence
        if (hoverSequence != null && hoverSequence.IsActive())
        {
            hoverSequence.Kill(false);
        }
        
        // Reset scale
        transform.DOScale(originalScale, 0.2f).SetEase(Ease.InBack);
        
        // Reset color
        if (rockMaterial != null)
        {
            rockMaterial.DOColor(originalColor, 0.2f);
        }
        
        // Reset position (in case it was floating from selection animation)
        transform.DOLocalMoveY(0f, 0.2f).SetEase(Ease.InBack);
    }
    
    private void OnDestroy()
    {
        // Safely kill the sequence
        if (hoverSequence != null && hoverSequence.IsActive())
        {
            hoverSequence.Kill(false);
        }
        hoverSequence = null;
        
        // Kill any individual tweens on this transform
        transform.DOKill();
        
        // Kill any material tweens
        if (rockMaterial != null)
        {
            rockMaterial.DOKill();
        }
    }
}
