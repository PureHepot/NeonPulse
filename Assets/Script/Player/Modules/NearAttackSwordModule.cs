using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NearAttackSwordModule : PlayerModule
{
    public GameObject SwordPrefab;
    public Transform orbitCenter;          // 玩家中心
    private TrailRenderer trail;
    
    [Header("Attack Settings")]
    public float swordRadius = 2f;         // 刀尖到圆心的距离（检测半径）
    public float attackAngle = 180f;       // 扫过的总角度
    public int maxTargets = int.MaxValue;
    public float attackDuration = 0.2f;
    public float attackCooldown = 0.2f;    // 攻击冷却时间
    public LayerMask enemyLayer;
    
    [Header("Weapon Visual Adjustment")]
    public float radiusOffset = 0f;        // 视觉补偿
    public bool weaponUpIsRadius = true;   // 竖着的刀选true（up指向外），横着的刀选false（right指向外）
    
    private bool isAttacking = false;
    private float baseDamage;
    private GameObject currentSword;
    private float startAngle;
    private float endAngle;

    protected override void OnInitialize()
    {
        baseDamage = GetStat("weapon.damage", 10f);
        
        currentSword = Instantiate(SwordPrefab, transform);
        currentSword.SetActive(false);
        
        trail = currentSword.GetComponent<TrailRenderer>();
        if (trail != null) trail.enabled = false;

        if (orbitCenter == null) orbitCenter = GameObject.FindGameObjectWithTag("Player").transform;
    }
    
    public override void OnModuleUpdate()
    {
        if (InputManager.Instance.Mouse0Down() && !isAttacking)
        {
            StartCoroutine(Attack());
        }
    }
    
    private IEnumerator Attack()
    {
        isAttacking = true;
        currentSword.SetActive(true);
        
        if (trail != null)
        {
            trail.Clear();
            trail.enabled = true;
        }
        
        Vector2 mouseDir = GetAttackDirection();
        float centerAngle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
        float halfAngle = attackAngle / 2f;
        startAngle = centerAngle + halfAngle;
        endAngle = centerAngle - halfAngle;
        
        float elapsedTime = 0f;
        bool damageDealt = false;
        
        while (elapsedTime < attackDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / attackDuration;
            float currentAngle = Mathf.Lerp(startAngle, endAngle, t);
            Vector2 direction = GetDirectionFromAngle(currentAngle);
            
            Vector3 visualPos = orbitCenter.position + (Vector3)(direction * (swordRadius + radiusOffset));
            currentSword.transform.position = visualPos;
            
            if (weaponUpIsRadius)
                currentSword.transform.up = -direction;
            else
                currentSword.transform.right = direction;

            if (!damageDealt && t >= attackDuration * 0.2f)
            {
                PerformSectorAttack(centerAngle, halfAngle);
                damageDealt = true;
            }
            
            yield return null;
        }
        
        // 攻击动作结束，立刻隐藏武器和拖尾
        if (trail != null) trail.enabled = false;
        currentSword.SetActive(false);
        
        // 等待攻击冷却，期间 isAttacking 仍为 true，无法再次攻击
        yield return new WaitForSeconds(attackCooldown);
        
        isAttacking = false;
    }
    
    private void PerformSectorAttack(float centerAngle, float halfAngle)
    {
        Vector2 attackDirection = GetDirectionFromAngle(centerAngle);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(orbitCenter.position, swordRadius, enemyLayer);
        List<IDamageable> hitTargets = new List<IDamageable>();
        
        foreach (var collider in hitColliders)
        {
            Vector2 toTarget = (collider.transform.position - orbitCenter.position).normalized;
            float angleToTarget = Vector2.Angle(attackDirection, toTarget);
            if (angleToTarget <= halfAngle)
            {
                IDamageable target = collider.GetComponent<IDamageable>();
                if (target != null) hitTargets.Add(target);
            }
        }
        
        int hitCount = 0;
        foreach (var target in hitTargets)
        {
            if (hitCount >= maxTargets) break;
            target.TakeDamage((int)baseDamage, orbitCenter.position, attackDirection);
            hitCount++;
        }
        
        Debug.Log($"扇形攻击击中 {hitCount} 个敌人");
    }
    
    private Vector2 GetDirectionFromAngle(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
    
    private Vector2 GetAttackDirection()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return (mousePos - orbitCenter.position).normalized;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (orbitCenter == null) return;
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(orbitCenter.position, swordRadius);
        
        if (Application.isPlaying && isAttacking)
        {
            Gizmos.color = Color.red;
            float half = attackAngle / 2f;
            Vector2 mouseDir = GetAttackDirection();
            float center = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
            DrawArc(orbitCenter.position, swordRadius, center - half, center + half);
        }
    }
    
    private void DrawArc(Vector3 center, float radius, float fromAngle, float toAngle)
    {
        int segments = 30;
        Vector3 prev = center + (Vector3)GetDirectionFromAngle(fromAngle) * radius;
        for (int i = 1; i <= segments; i++)
        {
            float ang = Mathf.Lerp(fromAngle, toAngle, i / (float)segments);
            Vector3 cur = center + (Vector3)GetDirectionFromAngle(ang) * radius;
            Gizmos.DrawLine(prev, cur);
            prev = cur;
        }
    }
}