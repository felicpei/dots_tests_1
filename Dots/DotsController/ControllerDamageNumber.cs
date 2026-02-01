using UnityEngine;

public class ControllerDamageNumber  : MonoBehaviour
{
    private Material _material;
    private static readonly int Tile = Shader.PropertyToID("_Tile");
    private static readonly int Color1 = Shader.PropertyToID("_Color");

    private void Awake()
    {
        _material = GetComponent<Renderer>().material;
    }

    public void Init(int tile, Color color)
    {
        _material.SetFloat(Tile, tile);
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