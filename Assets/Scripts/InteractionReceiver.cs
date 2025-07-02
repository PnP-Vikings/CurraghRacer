using UnityEngine;
using UnityEngine.Events;

public class InteractionReceiver : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private string[] acceptedItemNames;
    [SerializeField] private bool acceptAnyItem = true;
    
    [Header("Interaction Effects")]
    [SerializeField] private bool destroyItemOnInteraction = false;
    [SerializeField] private bool disableItemOnInteraction = false;
    [SerializeField] private float interactionCooldown = 0.5f;
    
    [Header("Visual Feedback")]
    [SerializeField] private ParticleSystem interactionEffect;
    [SerializeField] private AudioClip interactionSound;
    
    [Header("Events")]
    public UnityEvent<HoldableItem> OnValidInteraction;
    public UnityEvent<HoldableItem> OnInvalidInteraction;
    
    private AudioSource audioSource;
    private float lastInteractionTime;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    
    public void OnInteractWithHeldItem(HoldableItem heldItem)
    {
        // Check cooldown
        if (Time.time - lastInteractionTime < interactionCooldown)
            return;
        
        lastInteractionTime = Time.time;
        
        // Check if this item is accepted
        bool isValidItem = acceptAnyItem || IsItemAccepted(heldItem.name);
        
        if (isValidItem)
        {
            HandleValidInteraction(heldItem);
        }
        else
        {
            HandleInvalidInteraction(heldItem);
        }
    }
    
    private bool IsItemAccepted(string itemName)
    {
        foreach (string acceptedName in acceptedItemNames)
        {
            if (itemName.Contains(acceptedName))
                return true;
        }
        return false;
    }
    
    private void HandleValidInteraction(HoldableItem heldItem)
    {
        Debug.Log($"Valid interaction: {heldItem.name} with {gameObject.name}");
        
        // Play effects
        PlayInteractionEffects();
        
        // Handle item state
        if (destroyItemOnInteraction)
        {
            Destroy(heldItem.gameObject);
        }
        else if (disableItemOnInteraction)
        {
            heldItem.gameObject.SetActive(false);
        }
        
        // Trigger event
        OnValidInteraction.Invoke(heldItem);
    }
    
    private void HandleInvalidInteraction(HoldableItem heldItem)
    {
        Debug.Log($"Invalid interaction: {heldItem.name} with {gameObject.name}");
        OnInvalidInteraction.Invoke(heldItem);
    }
    
    private void PlayInteractionEffects()
    {
        // Play particle effect
        if (interactionEffect != null)
        {
            interactionEffect.Play();
        }
        
        // Play sound effect
        if (audioSource != null && interactionSound != null)
        {
            audioSource.PlayOneShot(interactionSound);
        }
    }
    
    public void AddAcceptedItem(string itemName)
    {
        System.Array.Resize(ref acceptedItemNames, acceptedItemNames.Length + 1);
        acceptedItemNames[acceptedItemNames.Length - 1] = itemName;
    }
    
    public void SetAcceptAnyItem(bool accept)
    {
        acceptAnyItem = accept;
    }
}