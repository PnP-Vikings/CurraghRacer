// Import necessary namespaces
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ensure this script executes in edit mode as well
[ExecuteAlways]
public class LightingManager : MonoBehaviour
{
// Serialized fields
    [SerializeField] private Light DirectionalLight;
    [SerializeField] private LightingPreset Preset;
     public float fogamount = 0f;

// Update the lighting every frame


    private void OnEnable()
    {
        if (Preset != null && Application.isPlaying)
        {
            TimeManager.Instance.timeChangedEvent.AddListener(UpdateLighting);
        }
    }
    
    
    private void OnDisable()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.timeChangedEvent.RemoveListener(UpdateLighting);
        
    }

// Update the lighting based on the time of day
    private void UpdateLighting()
    {
        float timePercent = TimeManager.Instance.TimeOfDay / 24f;
        RenderSettings.ambientLight = Preset.ambientColor.Evaluate(timePercent);
        RenderSettings.fogColor = Preset.fogColor.Evaluate(timePercent);



        if (DirectionalLight != null)
        {
            DirectionalLight.color = Preset.directionalColor.Evaluate(timePercent);
            DirectionalLight.transform.localRotation =
                Quaternion.Euler(new Vector3((timePercent * 360f) - 90f, 170, 0));
        }

        float fogDensity = Preset.GetFogDensity(timePercent);
        fogamount = fogDensity;
        RenderSettings.fogDensity = fogDensity;
    }



// Validate the Directional Light field
    private void OnValidate()
    {
        // If the Directional Light is already set, exit the function
        if (DirectionalLight != null)
        {
            return;
        }

        // If the Directional Light is not set but there is a RenderSettings.sun, set the Directional Light to the sun
        if (RenderSettings.sun != null)
        {
            DirectionalLight = RenderSettings.sun;
        }
        // If the Directional Light is not set and there is no RenderSettings.sun, find the Directional Light in the scene
        else
        {
            Light[] lights = GameObject.FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    DirectionalLight = light;
                    return;
                }
            }
        }
    }
}


