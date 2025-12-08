using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PlateLogic : MonoBehaviour
{
    public List<DirtShaderLogic> dirt; // List of dirt shaders on the plate
    public int numofTimesPlateDippedInWater = 0;
    public int timesPlateNeedsToBeDippedInWaterToClean = 3;
    [SerializeField] private ParticleSystem waterSplashEffect;
    
    private void OnEnable()
    {
      foreach (var dirtShader in this.gameObject.GetComponentsInChildren<DirtShaderLogic>())
      {
          dirt.Add(dirtShader); // Add each dirt shader to the list
      }
      Debug.Log("Plate Logic Enabled: " + dirt.Count + " dirt shaders found.");
      numofTimesPlateDippedInWater = 0;
    }
    
    public void PlayWaterSplashEffect()
    {
        if (waterSplashEffect != null)
        {
            waterSplashEffect.Play();
        }
    }
    
    public void StopWaterSplashEffect()
    {
        if (waterSplashEffect != null)
        {
            waterSplashEffect.Stop();
        }
    }
    
    public void IncrementWaterDipCount()
    {
        numofTimesPlateDippedInWater++;
        Debug.Log("Plate dipped in water " + numofTimesPlateDippedInWater + " times.");
    }
    
    public bool HasBeenDippedInWaterEnough()
    {
        return numofTimesPlateDippedInWater >= timesPlateNeedsToBeDippedInWaterToClean;
    }
    
    public bool IsPlateClean()
    {
        foreach (var dirtShader in dirt)
        {
            if (dirtShader != null && !dirtShader.IsClean())
            {
                return false; // If any dirt shader is not clean, the plate is not clean
            }
        }
        return true; // All dirt shaders are clean
    }

    public void SetAllDirtShaderstoCleaning()
    {
        foreach (var dirtShader in dirt)
        {
            if (dirtShader != null)
            {
                dirtShader.SetIsCleaning(true); // Start cleaning each dirt shader
            }
        }
    }


}
