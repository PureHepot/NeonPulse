using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class BossTurret : MonoBehaviour
{
    [Header("References")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Standard Fire Settings")]
    public float fireInterval = 3.0f;//射击间隔
    public int burstCount = 10;//子弹数量
    public float burstRate = 0.1f;//连续射击的最小时间

    [Header("Wild Mode Settings")]
    public float wildFireRate = 0.1f;
    public float wildSpreadRadius = 3.0f;

    private bool isWildMode = false;
    private Coroutine currentRoutine;


    // 内部变量
    private float timer;
    private bool isShooting = false; // 防止在大间隔倒计时中重复触发

    public void FireBurst()
    {
        if (!gameObject.activeInHierarchy || isWildMode) return;
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(ShootBurstRoutine());
    }

    public void SetWildMode(bool active)
    {
        if (!gameObject.activeInHierarchy) return;
        isWildMode = active;

        if (currentRoutine != null) StopCoroutine(currentRoutine);

        if (isWildMode)
        {
            currentRoutine = StartCoroutine(WildFireRoutine());
        }
    }


    IEnumerator WildFireRoutine()
    {
        while (true)
        {
            if (PlayerManager.Instance != null && PlayerManager.Instance.IsPlayerAlive)
            {
                // 在玩家周围随机选点
                Vector3 targetPos = PlayerManager.Instance.PlayerPosition;
                Vector2 randomOffset = Random.insideUnitCircle * wildSpreadRadius;
                targetPos += (Vector3)randomOffset;

                FireBullet(targetPos);
            }
            yield return new WaitForSeconds(wildFireRate); // 极快射速
        }
    }

    /// <summary>
    /// 连射协程：像机枪一样突突突
    /// </summary>
    IEnumerator ShootBurstRoutine()
    {
        isShooting = true;

        for (int i = 0; i < burstCount; i++)
        {
            if (PlayerManager.Instance != null && PlayerManager.Instance.IsPlayerAlive)
            {
                FireBullet(PlayerManager.Instance.PlayerPosition);
            }
            yield return new WaitForSeconds(burstRate);
        }

        isShooting = false;
    }

    private void FireBullet(Vector3 targetPos)
    {
        if (bulletPrefab == null || firePoint == null) return;
        Vector2 direction = (targetPos - firePoint.position).normalized;
        GameObject bulletObj = ObjectPoolManager.Instance.Get(bulletPrefab, firePoint.position, Quaternion.identity);
        bulletObj.GetComponent<EnemyProjectile>()?.Initialize(direction);
    }
}