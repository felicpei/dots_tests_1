using System;
using System.Collections.Generic;
using DG.Tweening;
using Lobby;
using Unity.Mathematics;
using UnityEngine;

public abstract class ControllerBase : MonoBehaviour
{
    protected Animator Animator;
    protected List<Material> Materials = new();
    public GameObject Muzzle;
    public List<GameObject> Weapons;
    
    private bool _bUseAnimMoveForward;
    private void Awake()
    {
        Animator = GetComponent<Animator>();
        hasMuzzle = Muzzle != null;
        var skinRenderers = gameObject.GetComponentsInChildren<Renderer>();
        foreach (var render in skinRenderers)
        {
            Materials.Add(render.material);
        }

        if (Animator != null)
        {
            var bVert = false;
            var bHoriz = false;
            foreach (var param in Animator.parameters)
            {
                if (param.nameHash == InputVertical)
                {
                    bVert = true;
                }

                if (param.nameHash == InputHorizontal)
                {
                    bHoriz = true;
                }
            }
            _bUseAnimMoveForward = bVert && bHoriz;
        }
      
    }

    private bool _bActive = true;
    
    public void SetActive(bool bActive)
    {
        if (_bActive == bActive)
        {
            return;
        }

        _bActive = bActive;
        var renderers = gameObject.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r != null)
            {
                r.enabled = bActive;
            }
        }
    }

    public void SetWeaponActive(bool bWeaponActive)
    {
        if (Weapons != null && Weapons.Count > 0)
        {
            foreach (var weapon in Weapons)
            {
                weapon.SetActiveSafe(bWeaponActive);
            }
        }
    }

    public bool hasMuzzle;

    public float3 getMuzzlePos()
    {
        if (hasMuzzle)
        {
            return Muzzle.transform.position - transform.position;
        }
        return float3.zero;
    }

    public List<float3> getSlotPos(int slotId)
    {
        return new List<float3>();
    }
    
    public void PlayAnimation(string aniName)
    {
        if (Animator != null && gameObject.activeSelf)
        {
            if (!Animator.HasState(0, Animator.StringToHash(aniName)))
            {
                //Debug.LogError("播放Prefab怪物动作错误，动作在状态机中不存在，动作名称:" + aniName + " name:" + gameObject.name);
            }
            else
            {
                Animator.Play(aniName);
            }
        }
    }

    private float _prevBlendValue;
    private Color _prevColor;
    private static readonly int BlendColor = Shader.PropertyToID("_BlendColor");
    private static readonly int BlendOpacity = Shader.PropertyToID("_BlendOpacity");
    private static readonly int InMove = Animator.StringToHash("InMove");
    private static readonly int Atk = Animator.StringToHash("Atk");
    private static readonly int Dead = Animator.StringToHash("Dead");
    private static readonly int Revive = Animator.StringToHash("Revive");
    private static readonly int Spell = Animator.StringToHash("Spell");
    private static readonly int InputHorizontal = Animator.StringToHash("InputHorizontal");
    private static readonly int InputVertical = Animator.StringToHash("InputVertical");

    public void UpdateBlend(float value, Color color)
    {
        //低配不走
        if (Math.Abs(_prevBlendValue - value) > 0.001f)
        {
            _prevBlendValue = value;
            foreach (var mat in Materials)
            {
                mat.SetFloat(BlendOpacity, value);
            }
        }
        
        if (_prevColor != color)
        {
            _prevBlendValue = value;
            foreach (var mat in Materials)
            {
                mat.SetColor(BlendColor, color);
            }
        }
    }

    private bool _prevInMove;
    public void UpdateMove(bool inMove)
    {
        if (_bDead)
        {
            return;
        }
        if (_prevInMove != inMove)
        {
            _prevInMove = inMove;
            Animator?.SetBool(InMove, inMove);
        }
    }

    public void UpdateMoveForward(float3 moveForward, float3 faceForward)
    {
        if (Animator != null &&　_bUseAnimMoveForward)
        {
            Vector3 moveDir = moveForward;
            Vector3 faceDir = faceForward;

// 角色的本地坐标系轴
            Vector3 right = Vector3.Cross(Vector3.up, faceDir); // 右方向
            Vector3 forward = faceDir;                          // 前方向

// 计算Blend Tree所需的输入
            float inputHorizontal = Vector3.Dot(moveDir, right);
            float inputVertical = Vector3.Dot(moveDir, forward);
            
            
            Animator.SetFloat(InputHorizontal, inputHorizontal);
            Animator.SetFloat(InputVertical, inputVertical);
        }
    }

    public void PlayAtk()
    {
        if (_bDead)
        {
            return;
        }
        Animator?.ResetTrigger(Atk);
        Animator?.SetTrigger(Atk);
    }

    private bool _bDead;
    public void PlayDead()
    {
        _bDead = true;
        Animator?.ResetTrigger(Dead);
        Animator?.SetTrigger(Dead);
        Animator?.SetBool(InMove, false);
    }
    
    public void PlayRevive()
    {
        if (_bDead)
        {
            Animator?.ResetTrigger(Revive);
            Animator?.SetTrigger(Revive);
            _bDead = false;
        }
    }

    public void SetAttacking(bool bAttacking)
    {
        Animator?.SetBool(Spell, bAttacking);
    }

    private GameObject _muzzleEffect;
    private Tween _playTween;
    private Tween _stopTween;
    
    public void PlayMuzzleEffect(int id, float delay)
    {
        if (Muzzle == null)
        {
            Debug.LogError("PlayMuzzleEffect error, muzzle is null");
            return;
        }

        if (_muzzleEffect != null)
        {
            Destroy(_muzzleEffect);
            _muzzleEffect = null;
        }

        _stopTween?.Kill();
        _playTween?.Kill();
        _playTween = DOVirtual.DelayedCall(delay, async () =>
        {
            var resDp = Table.GetResourceDeploy(id);
            _muzzleEffect = await G.LoadGameObject(resDp.Url);
            _muzzleEffect.transform.SetParent(Muzzle.transform, false);
            _muzzleEffect.transform.localScale = resDp.Scale * Vector3.one;
            _muzzleEffect.transform.localPosition = Vector3.zero;
        });
    }

    public void StopMuzzleEffect(float delay)
    {
        _stopTween?.Kill();
        _stopTween = DOVirtual.DelayedCall(delay, () =>
        {
            if (_muzzleEffect != null)
            {
                Destroy(_muzzleEffect);
                _muzzleEffect = null;
            }
        });
    }
    
    private void OnDestroy()
    {
        for (var i = 0; i < Materials.Count; i++)
        {
            Destroy(Materials[i]);
        }

        if (_muzzleEffect != null)
        {
            Destroy(_muzzleEffect);
            _muzzleEffect = null;
        }
        
        Materials.Clear();
        
        _stopTween?.Kill();
        _stopTween = null;
        
        _playTween?.Kill();
        _playTween = null;
    }
}