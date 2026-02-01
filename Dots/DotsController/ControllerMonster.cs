using System;
using System.Collections.Generic;
using Dots;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ControllerMonster : ControllerBase
{
    private static readonly int EmissionPower = Shader.PropertyToID("_EmissionPower");
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int Brightness = Shader.PropertyToID("_Brightness");
    
    public void Init(int paramId)
    {
        
    }

    public void InitShader(ECreatureType type, float brightness = 0f)
    {
        Color color = default;
        var setColor = false;
        if (type == ECreatureType.Boss)
        {
            setColor = true;
            color = new Color(1F, 0.3f, 0, 1);
        }
        else if (type == ECreatureType.Elite)
        {
            setColor = true;
            color = new Color(1F, 0.8f, 0, 1);
        }
        
        foreach (var mat in Materials)
        {
            if (setColor)
            {
                mat.SetFloat(EmissionPower, 5f);
                mat.SetColor(EmissionColor, color);    
            }
            
            /*if (brightness > 0)
            {
                mat.SetFloat(Brightness, brightness);
            }*/
        }
    }
}