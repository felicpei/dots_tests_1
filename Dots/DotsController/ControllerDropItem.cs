using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ControllerDropItem : MonoBehaviour
{
    private bool _bFlash;
    private float _timer;
    private bool _bActive;
    private float _minDelta = 0.25f;
    public void UpdateFlash(bool bFlash)
    {
        if (!_bFlash && bFlash)
        {
            //start flash
            _bFlash = true;
            _timer = 0;
        }
    }

    private void Update()
    {
        if (_bFlash)
        {
            var deltaTime = Time.deltaTime;
            _timer += deltaTime;
            if (_timer >= _minDelta)
            {
                _minDelta *= 0.96f;
                if (_minDelta < 0.05f)
                {
                    _minDelta = 0.05f;
                }
                Renderer.enabled = _bActive;
                _bActive = !_bActive;
                _timer = 0;
            }
        }
    }

    private Renderer _renderer;

    private Renderer Renderer
    {
        get
        {
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<Renderer>();
            }

            return _renderer;
        }
    }
}