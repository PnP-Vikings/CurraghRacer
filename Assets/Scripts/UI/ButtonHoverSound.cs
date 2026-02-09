using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Add this component to any button to play a sound when hovering over it.
/// Works with mouse and touch input.
/// </summary>
public class ButtonHoverSound : MonoBehaviour, IPointerEnterHandler
{
    [Header("Sound Settings")]
    [SerializeField] private bool playOnHover = true;
    [SerializeField] private string soundEventName = "UIClick3"; // Default sound
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playOnHover) return;
        
        PlayHoverSound();
    }
    
    private void PlayHoverSound()
    {
        if (AudioManager.instance == null) return;
        
        // Play the sound based on the configured event name
        switch (soundEventName)
        {
            case "UIClick1":
                AudioManager.instance.UIClick1.start();
                break;
            case "UIClick3":
                AudioManager.instance.UIClick3.start();
                break;
            // Add more cases here for other sounds if needed
            default:
                AudioManager.instance.UIClick3.start();
                break;
        }
    }
    
    /// <summary>
    /// Programmatically set which sound to play on hover
    /// </summary>
    public void SetSound(string soundName)
    {
        soundEventName = soundName;
    }
    
    /// <summary>
    /// Enable or disable hover sound
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        playOnHover = enabled;
    }
}
