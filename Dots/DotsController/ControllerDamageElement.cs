using System;
using Deploys;
using UnityEngine;

public class ControllerDamageElement  : MonoBehaviour
{
    private Renderer _renderer;

    public Material ele_charged;
    public Material elz_crystal;
    public Material elz_freeze;
    public Material elz_melt;
    public Material elz_overloaded;
    public Material elz_shatter;
    public Material elz_superconduct;
    public Material elz_vaporize;
    
    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    private static readonly int Color1 = Shader.PropertyToID("_Color");
    private Material _material;
    
    public void Init(EElementReaction reaction, Color color)
    {
        switch (reaction)
        {
            case EElementReaction.Vaporize:
                _renderer.material = elz_vaporize;
                break;
            case EElementReaction.Melt:
                _renderer.material = elz_melt;
                break;
            case EElementReaction.Overloaded:
                _renderer.material = elz_overloaded;
                break;
            case EElementReaction.Charged:
                _renderer.material = ele_charged;
                break;
            case EElementReaction.Freeze:
                _renderer.material = elz_freeze;
                break;
            case EElementReaction.Superconduct:
                _renderer.material = elz_superconduct;
                break;
            case EElementReaction.Shatter:
                _renderer.material = elz_shatter;
                break;
            case EElementReaction.Crystallize:
                _renderer.material = elz_crystal;
                break;
        }

        _material = _renderer.material;
        _material.SetColor(Color1, color);
    }

    public void OnRecycle()
    {
        
    }

    private void OnDestroy()
    {
        if (_material != null)
        {
            Destroy(_material);
        }
    }
}