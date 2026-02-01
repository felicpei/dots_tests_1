using UnityEngine;

public class ControllerServant : ControllerBase
{
    private int m_servantId;
    public ERarity m_rarity;
    
    public void Init(int servantId, ERarity rarity)
    {
        m_servantId = servantId;
        m_rarity = rarity;
        
        InitShader(rarity);
    }

    private float _curAlpha = 1f;
    public void SetAlpha(float alpha)
    {
        if (Mathf.Approximately(_curAlpha, alpha))
        {
            return;
        }
        _curAlpha = alpha;
    }

    private static readonly int EmissionPower = Shader.PropertyToID("_EmissionPower");
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    public void InitShader(ERarity type)
    {
        Color color = default;
        var setColor = false;

        switch (type)
        {
            case ERarity.R2:
            {
                //green
                setColor = true;
                color = new Color(0F, 0.8f, 0, 1);
                break;
            }
            case ERarity.R3:
            {
                //blue
                setColor = true;
                color = new Color(0F, 0.68f, 1, 1);
                break;
            }
            case ERarity.R4:
            {
                //purple
                setColor = true;
                color = new Color(1F, 0, 1, 1);
                break;
            }
            case ERarity.R5:
            {
                //golden
                setColor = true;
                color = new Color(1F, 0.8f, 0, 1);
                break;
            }
        }

        foreach (var mat in Materials)
        {
            if (setColor)
            {
                mat.SetFloat(EmissionPower, 2.5f);
                mat.SetColor(EmissionColor, color);
            }
        }
    }
}