using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;


public class ServantHud : MonoBehaviour
{
    public Image HpFore;
    public Image Aim;
    public GameObject ShieldRoot;
    public Image ShieldFore;

    private float _aimAngle;
    private Transform _root;
    private bool _inited;
    private bool _isMain;
    private Vector3 _originScale;

    private void Awake()
    {
        ShieldRoot.SetActive(false);
        _originScale = transform.localScale;
    }

    public void Init(Transform root, int servantId, bool isMainServant)
    {
        _root = root;
        _isMain = isMainServant;
        
        if (_isMain)
        {
            _aimAngle = 10;
            Aim.gameObject.SetActiveSafe(_aimAngle > 0);
            Aim.fillAmount = _aimAngle / 360f;
        }
        else
        {
            Aim.gameObject.SetActive(false);
        }
      
        _inited = true;
    }

    public void UpdateAim(float3 faceForward)
    {
        if (_isMain && _aimAngle > 0)
        {
            var worldAngle = MathHelper.Forward2Angle(faceForward);
            worldAngle -= _aimAngle / 2f;
            Aim.transform.localEulerAngles = new Vector3(0, 0, -worldAngle);
        }
    }

    public void SetHp(float curHp, float maxHp)
    {
        var progress = curHp / maxHp;
        HpFore.fillAmount = progress;
    }

    private bool _shieldActive;
    public void SetShield(float curShield, float maxShield)
    {
        var shieldActive = curShield > 0 && maxShield > 0;
        if (shieldActive != _shieldActive)
        {
            _shieldActive = shieldActive;
            ShieldRoot.SetActive(shieldActive);
        }

        if (shieldActive)
        {
            var progress = curShield / maxShield;
            ShieldFore.fillAmount = progress;
        }
    }

    private void LateUpdate()
    {
        if (_inited)
        {
            if (_root == null)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = _root.position;
            var cameraRotation = CameraController.GetCameraRotation(out var cameraPos);
            transform.rotation = cameraRotation;
            
            /*
            var toCameraDist = math.distance(cameraPos, transform.position);
            var scaleFac = toCameraDist / 15f;
            transform.localScale = _originScale * scaleFac;*/
        }
    }
}