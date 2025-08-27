using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class WallLogic : MonoBehaviour
{
    public List<DirtShaderLogic> dirt; // List of dirt shaders on the plate

    private void OnEnable()
    {
      foreach (var dirtShader in this.gameObject.GetComponentsInChildren<DirtShaderLogic>())
      {
          dirt.Add(dirtShader); // Add each dirt shader to the list
      }
      Debug.Log("Wall Logic Enabled: " + dirt.Count + " dirt shaders found.");
    }
    
    
    public bool IsWallClean()
    {
        foreach (var dirtShader in dirt)
        {
            if (dirtShader != null && !dirtShader.IsClean())
            {
                return false; // If any dirt shader is not clean, the wall is not clean
            }
        }
        return true; // All dirt shaders are clean
    }
    
    

}
