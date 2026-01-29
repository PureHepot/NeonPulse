using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldController : MonoBehaviour
{
    [Header("Settings")]
    public float maxIntegrity = 100f;
    public float recoveryRate = 10f;
    public float recoveryDelay = 1.0f;
    public float bounceForce = 10f;

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

    private Vector3 originalScale;
    private bool isInitialized = false;

    private void Start()
    {
        currentIntegrity = maxIntegrity;
        UpdateShieldState(0f);

        if (visualRoot != null)
        {
            originalScale = visualRoot.localScale;
            isInitialized = true;
        }
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

        if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.layer == LayerMask.NameToLayer("EnemyBullet"))
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

            if (visualRoot != null)
            {
                if (!isInitialized)
                {
                    originalScale = visualRoot.localScale;
                    isInitialized = true;
                }

                visualRoot.DOKill();

                visualRoot.localScale = originalScale;

                visualRoot.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.1f);
            }
        }
    }
}
