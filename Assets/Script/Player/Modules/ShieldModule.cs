using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldModule : PlayerModule
{
    [Header("Shield References")]
    public GameObject shieldObject;
    public ShieldController shieldScript;

    [Header("Settings")]
    public float rechargeRate = 1f;
    public float rotateSpeed = 200f;

    public override void Initialize(PlayerController _player)
    {
        base.Initialize(_player);

        if (shieldObject) shieldObject.SetActive(false);
    }

    public override void OnActivate()
    {
        base.OnActivate();
        // 激活时，打开护盾物体
        if (shieldObject) shieldObject.SetActive(true);
        // 重置护盾状态
        if (shieldScript) shieldScript.SetDefend(false); // 假设你之前的脚本有这个方法
    }

    public override void OnModuleUpdate()
    {
        HandleRotation();

        if (InputManager.Instance.Mouse1())
        {
            shieldScript.SetDefend(true);
        }
        else
        {
            shieldScript.SetDefend(false);
        }
    }

    void HandleRotation()
    {
        if (shieldObject == null) return;

        Vector3 mousePos = MUtils.GetMouseWorldPosition();

        Vector2 direction = mousePos - shieldObject.transform.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);

        shieldObject.transform.rotation = Quaternion.Slerp(shieldObject.transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();
        if (shieldObject) shieldObject.SetActive(false);
    }
}
