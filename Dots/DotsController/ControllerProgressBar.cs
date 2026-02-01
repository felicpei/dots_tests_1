using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ControllerProgressBar : MonoBehaviour
{
    private Material _material;
    private static readonly int Fill = Shader.PropertyToID("_FillRate");

    private void Awake()
    {
        _material = GetComponent<Renderer>().material;
    }

    private float _lastFill;
    public void UpdateProgress(float fill)
    {
        if (!Mathf.Approximately(_lastFill, fill))
        {
            _lastFill = fill;
            _material.SetFloat(Fill, fill);
        }
    }

    private void OnDestroy()
    {
        if (_material != null)
        {
            Destroy(_material);
        }
    }
}