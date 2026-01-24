using UnityEngine;
using DG.Tweening;

public class EnemyShooter : EnemyBase
{
    [Header("Shooter Movement")]
    public float padding = 2.0f;
    public float cornerRadius = 3.0f;
    public float enterSpeed = 5f;      // 入场速度

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireInterval = 1.5f;
    public int burstCount = 1;
    public float shootSpeed = 10;

    private float shootTimer;
    private bool isOrbiting = false;   // 是否已经进入轨道
    private int direction = 1;         // 1为顺时针，-1为逆时针 (随机)
    private float currentPathDist = 0f;

    private float rectW, rectH;
    private float totalLength;
    // 缓存摄像机边界
    private float xMax, yMax;

    public override void OnSpawn()
    {
        base.OnSpawn();
        isOrbiting = false;

        firePoint = transform.Find("FirePoint");

        shootTimer = fireInterval;
        direction = Random.value > 0.5f ? 1 : -1;

        Camera cam = Camera.main;
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        rectW = camWidth - 2 * padding;
        rectH = camHeight - 2 * padding;

        xMax = rectW / 2f;
        yMax = rectH / 2f;

        float maxR = Mathf.Min(rectW, rectH) / 2f;
        float actualR = Mathf.Min(cornerRadius, maxR);

        totalLength = 2 * (rectW - 2 * actualR) + 2 * (rectH - 2 * actualR) + 2 * Mathf.PI * actualR;

        currentPathDist = Random.Range(0, totalLength);
    }

    protected override void MoveBehavior()
    {
        if (playerTransform == null) return;

        // --- A. 瞄准逻辑 (始终朝向玩家) ---
        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        float angle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
        // 假设 Sprite 头朝右，直接赋值；如果头朝上，angle - 90
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // --- B. 移动逻辑 ---

        // 1. 计算这一帧在轨道上的目标点
        currentPathDist = (currentPathDist + moveSpeed * Time.deltaTime) % totalLength;
        Vector3 targetPosOnTrack = CalculateRectPosition(currentPathDist);

        if (!isOrbiting)
        {
            // 入场阶段：直接飞向计算出的轨道点
            // 使用 MoveTowards 平滑靠近
            transform.position = Vector3.MoveTowards(transform.position, targetPosOnTrack, enterSpeed * Time.deltaTime);

            // 如果距离非常近，视为入场完毕
            if (Vector3.Distance(transform.position, targetPosOnTrack) < 0.1f)
            {
                isOrbiting = true;
            }
        }
        else
        {
            // 环绕阶段：直接吸附在轨道点上 (或者用插值更平滑一点)
            // 这里直接赋值，因为 currentPathDist 已经是连续变化的了
            transform.position = targetPosOnTrack;
        }

        // 3. 射击逻辑
        HandleShooting();
    }

    void HandleShooting()
    {
        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0)
        {
            shootTimer = fireInterval;
            Shoot();
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null) return;

        Vector3 spawnPos = firePoint ? firePoint.position : transform.position;

        // 生成子弹
        GameObject bullet = ObjectPoolManager.Instance.Get(bulletPrefab, spawnPos, transform.rotation);
        bullet.GetComponent<EnemyBullet>().speed = shootSpeed;
    }

    Vector3 CalculateRectPosition(float dist)
    {
        float actualR = Mathf.Min(cornerRadius, Mathf.Min(rectW, rectH) / 2f);

        // 各段长度
        float topLen = rectW - 2 * actualR;    // 上边直线
        float cornerLen = 0.5f * Mathf.PI * actualR; // 1/4圆弧
        float sideLen = rectH - 2 * actualR;   // 侧边直线

        // 定义顺时针顺序：上边 -> 右上角 -> 右边 -> 右下角 -> 下边 -> 左下角 -> 左边 -> 左上角
        // 坐标系：中心(0,0)，上Y正，右X正

        // 1. Top Edge (上边直线)
        // 起点: (-w/2+R, h/2) -> 终点: (w/2-R, h/2)
        if (dist < topLen)
        {
            float t = dist; // 局部距离
            return new Vector3(-xMax + actualR + t, yMax, 0);
        }
        dist -= topLen;

        // 2. Top-Right Corner (右上角)
        // 圆心: (w/2-R, h/2-R)
        if (dist < cornerLen)
        {
            float angle = Mathf.Lerp(90f, 0f, dist / cornerLen) * Mathf.Deg2Rad;
            return new Vector3(xMax - actualR + Mathf.Cos(angle) * actualR, yMax - actualR + Mathf.Sin(angle) * actualR, 0);
        }
        dist -= cornerLen;

        // 3. Right Edge (右边直线)
        // (w/2, h/2-R) -> (w/2, -h/2+R)
        if (dist < sideLen)
        {
            return new Vector3(xMax, yMax - actualR - dist, 0);
        }
        dist -= sideLen;

        // 4. Bottom-Right Corner (右下角)
        if (dist < cornerLen)
        {
            float angle = Mathf.Lerp(0f, -90f, dist / cornerLen) * Mathf.Deg2Rad;
            return new Vector3(xMax - actualR + Mathf.Cos(angle) * actualR, -yMax + actualR + Mathf.Sin(angle) * actualR, 0);
        }
        dist -= cornerLen;

        // 5. Bottom Edge (下边直线)
        // (w/2-R, -h/2) -> (-w/2+R, -h/2)
        if (dist < topLen)
        {
            return new Vector3(xMax - actualR - dist, -yMax, 0);
        }
        dist -= topLen;

        // 6. Bottom-Left Corner (左下角)
        if (dist < cornerLen)
        {
            float angle = Mathf.Lerp(-90f, -180f, dist / cornerLen) * Mathf.Deg2Rad;
            return new Vector3(-xMax + actualR + Mathf.Cos(angle) * actualR, -yMax + actualR + Mathf.Sin(angle) * actualR, 0);
        }
        dist -= cornerLen;

        // 7. Left Edge (左边直线)
        // (-w/2, -h/2+R) -> (-w/2, h/2-R)
        if (dist < sideLen)
        {
            return new Vector3(-xMax, -yMax + actualR + dist, 0);
        }
        dist -= sideLen;

        // 8. Top-Left Corner (左上角)
        // 剩余的距离都在这里
        float finalAngle = Mathf.Lerp(180f, 90f, dist / cornerLen) * Mathf.Deg2Rad;
        return new Vector3(-xMax + actualR + Mathf.Cos(finalAngle) * actualR, yMax - actualR + Mathf.Sin(finalAngle) * actualR, 0);
    }
}