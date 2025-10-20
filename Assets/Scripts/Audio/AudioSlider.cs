using UnityEngine;
using UnityEngine.UI;

public class AudioSlider : MonoBehaviour
{
    public Slider audioSlider;
    public float audioLevel;
    
    void Start()
    {
        if (audioSlider == null)
        {
            audioSlider = this.GetComponent<Slider>();
        }
        
        if(AudioManager.instance != null)
        {
            audioLevel = AudioManager.instance.GetMasterVolume();
            Debug.Log("Current Master Volume: " + audioLevel);
            
            audioSlider.value = audioLevel;
            
            // Add listener to update volume when slider changes
            audioSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        else
        {
            Debug.LogWarning("AudioManager instance is null!");
        }
    }

    void OnDestroy()
    {
        // Clean up listener
        if (audioSlider != null)
        {
            audioSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        }
    }
    
    private void OnVolumeChanged(float value)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetMasterVolume(value);
            audioLevel = value;
            Debug.Log("Master Volume: " + audioLevel);
        }
    }
}
