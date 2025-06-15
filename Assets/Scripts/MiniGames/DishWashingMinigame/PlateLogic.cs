using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PlateLogic : MonoBehaviour
{
    public List<DirtShaderLogic> dirt; // List of dirt shaders on the plate

    private void OnEnable()
    {
      foreach (var dirtShader in this.gameObject.GetComponentsInChildren<DirtShaderLogic>())
      {
          dirt.Add(dirtShader); // Add each dirt shader to the list
      }
      Debug.Log("Plate Logic Enabled: " + dirt.Count + " dirt shaders found.");
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
    
    

}
