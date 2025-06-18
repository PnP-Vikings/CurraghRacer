using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
[CreateAssetMenu(fileName = "Lighting Preset",menuName = "Scriptables/Lighting Preset",order = 1)]
public class LightingPreset : ScriptableObject
{
    public Gradient ambientColor;
    public Gradient directionalColor;
    public Gradient fogColor;
    public Gradient skyColor;
    public AnimationCurve fogDensityCurve;

    public float GetFogDensity(float timePercent)
    {
        return fogDensityCurve.Evaluate(timePercent);
    }
}


    

