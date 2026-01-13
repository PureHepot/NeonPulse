using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldController : MonoBehaviour
{
    [Header("Settings")]
    public float maxIntegrity = 100f;
    public float recoveryRate = 10f;       // 每秒恢复量
    public float recoveryDelay = 1.0f;     // 受击后多久开始恢复
    public float bounceForce = 10f;        // 反弹力度

    [Header("Visual & Physics")]
    public Transform visualRoot;  
    public Vector2 maxColliderSize;       
    public Vector2 minColliderSize;       

    //运行时状态
    private float currentIntegrity;
    private float lastHitTime;
    private bool isDefending = false;

    // 用于平滑动画的变量
    private float targetDeployFactor = 0f; // 0 = 收起, 1 = 展开
    private float currentDeployFactor = 0f;

    public DynamicArcShield dynamicShield; 

    private void Start()
    {
        currentIntegrity = maxIntegrity;
        UpdateShieldState(0f);
    }

    void UpdateShieldState(float factor)
    {
        if (dynamicShield != null)
        {
            dynamicShield.deployFactor = factor;
        }
    }

    public void SetDefend(bool defend)
    {
        isDefending = defend;
    }

    void Update()
    {
        if (Time.time > lastHitTime + recoveryDelay)
        {
            currentIntegrity = Mathf.MoveTowards(currentIntegrity, maxIntegrity, recoveryRate * Time.deltaTime);
        }

        float intendedFactor = isDefending ? (currentIntegrity / maxIntegrity) : 0f;

        currentDeployFactor = Mathf.Lerp(currentDeployFactor, intendedFactor, Time.deltaTime * 15f);

        UpdateShieldState(currentDeployFactor);
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDefending) return;

        if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("Bullet"))
        {
            float damage = 20f;
            currentIntegrity = Mathf.Max(0, currentIntegrity - damage);
            lastHitTime = Time.time;

            Rigidbody2D otherRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (otherRb != null)
            {
                Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
                otherRb.AddForce(knockbackDir * bounceForce, ForceMode2D.Impulse);
            }

            visualRoot.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.1f);
        }
    }
}
