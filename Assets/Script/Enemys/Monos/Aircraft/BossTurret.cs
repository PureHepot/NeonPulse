using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class BossTurret : MonoBehaviour
{
    [Header("References")]
    [Tooltip("拖入挂载了 EnemyProjectile 的子弹预制件")]
    public GameObject bulletPrefab;

    [Tooltip("拖入 towerPoint (子弹生成点)")]
    public Transform firePoint;

    [Header("Fire Settings")]
    [Tooltip("两波弹幕之间的大间隔 (秒)")]
    public float fireInterval = 3.0f;

    [Tooltip("首次射击延迟 (秒)")]
    public float startDelay = 1.0f;

    [Header("Burst Settings (子弹链)")]
    [Tooltip("每次连射发射的子弹数量")]
    public int burstCount = 10;

    [Tooltip("连射时相邻子弹的微小间隔 (秒)，越小连得越紧")]
    public float burstRate = 0.1f;

    // 内部变量
    private float timer;
    private bool isShooting = false; // 防止在大间隔倒计时中重复触发

    private void Start()
    {
        timer = startDelay;
    }

    private void Update()
    {
        // 1. 检查玩家是否存在且存活
        if (PlayerManager.Instance == null || !PlayerManager.Instance.IsPlayerAlive)
            return;

        // 2. 如果正在连射中，暂停大间隔倒计时
        if (isShooting) return;

        // 3. 倒计时逻辑
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            // 启动连射协程
            StartCoroutine(ShootBurstRoutine());
            timer = fireInterval;
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
            // 在连射过程中，再次检查玩家是否存活（防止玩家死后鞭尸）
            if (PlayerManager.Instance != null && PlayerManager.Instance.IsPlayerAlive)
            {
                FireOneBullet();
            }

            // 等待极短的时间，形成链条感
            yield return new WaitForSeconds(burstRate);
        }

        isShooting = false;
    }

    private void FireOneBullet()
    {
        if (bulletPrefab == null || firePoint == null) return;

        // 获取玩家当前位置
        // 注意：每次发射都重新获取位置，这样子弹链会“扫射”跟随玩家
        // 如果想做“死板的一条线”，就把这行代码移到 for 循环外面去
        Vector3 targetPos = PlayerManager.Instance.PlayerPosition;

        // 计算方向
        Vector2 direction = (targetPos - firePoint.position).normalized;

        // 生成子弹
        GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        // 初始化子弹
        EnemyProjectile projectile = bulletObj.GetComponent<EnemyProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(direction);
        }
    }
}