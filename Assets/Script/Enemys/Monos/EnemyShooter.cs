using UnityEngine;
using DG.Tweening;

public class EnemyShooter : EnemyBase
{
    [Header("Shooter Movement")]
    public float padding = 2.0f;
    public float cornerRadius = 3.0f;
    public float enterSpeed = 5f;      // 鍏ュ満閫熷害

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireInterval = 1.5f;
    public int burstCount = 1;
    public float shootSpeed = 10;

    private float shootTimer;
    private bool isOrbiting = false;   // 鏄惁宸茬粡杩涘叆杞ㄩ亾
    private int direction = 1;         // 1涓洪『鏃堕拡锛?1涓洪€嗘椂閽?(闅忔満)
    private float currentPathDist = 0f;

    private float rectW, rectH;
    private float totalLength;
    // 缂撳瓨鎽勫儚鏈鸿竟鐣?
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

        // --- A. 鐬勫噯閫昏緫 (濮嬬粓鏈濆悜鐜╁) ---
        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        float angle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
        // 鍋囪 Sprite 澶存湞鍙筹紝鐩存帴璧嬪€硷紱濡傛灉澶存湞涓婏紝angle - 90
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // --- B. 绉诲姩閫昏緫 ---

        // 1. 璁＄畻杩欎竴甯у湪杞ㄩ亾涓婄殑鐩爣鐐?
        currentPathDist = (currentPathDist + moveSpeed * Time.deltaTime) % totalLength;
        Vector3 targetPosOnTrack = CalculateRectPosition(currentPathDist);

        if (!isOrbiting)
        {
            // 鍏ュ満闃舵锛氱洿鎺ラ鍚戣绠楀嚭鐨勮建閬撶偣
            // 浣跨敤 MoveTowards 骞虫粦闈犺繎
            Vector2 toTrack = targetPosOnTrack - transform.position;
            Vector2 targetVelocity = toTrack.sqrMagnitude > 0.0001f
                ? toTrack.normalized * enterSpeed
                : Vector2.zero;
            DriveVelocity(targetVelocity, 1.6f);

            // 濡傛灉璺濈闈炲父杩戯紝瑙嗕负鍏ュ満瀹屾瘯
            if (Vector3.Distance(transform.position, targetPosOnTrack) < 0.1f)
            {
                isOrbiting = true;
                StopMovementDrive();
            }
        }
        else
        {
            // 鐜粫闃舵锛氱洿鎺ュ惛闄勫湪杞ㄩ亾鐐逛笂 (鎴栬€呯敤鎻掑€兼洿骞虫粦涓€鐐?
            // 杩欓噷鐩存帴璧嬪€硷紝鍥犱负 currentPathDist 宸茬粡鏄繛缁彉鍖栫殑浜?
            Vector2 toTrack = targetPosOnTrack - transform.position;
            float orbitalSpeed = Mathf.Max(moveSpeed, enterSpeed);
            Vector2 targetVelocity = toTrack.sqrMagnitude > 0.0001f
                ? toTrack.normalized * orbitalSpeed
                : Vector2.zero;
            DriveVelocity(targetVelocity, 2f);
        }

        // 3. 灏勫嚮閫昏緫
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

        // 鐢熸垚瀛愬脊
        GameObject bullet = ObjectPoolManager.Instance.Get(bulletPrefab, spawnPos, transform.rotation);
        bullet.GetComponent<EnemyBullet>().speed = shootSpeed;
    }

    Vector3 CalculateRectPosition(float dist)
    {
        float actualR = Mathf.Min(cornerRadius, Mathf.Min(rectW, rectH) / 2f);

        // 鍚勬闀垮害
        float topLen = rectW - 2 * actualR;    // 涓婅竟鐩寸嚎
        float cornerLen = 0.5f * Mathf.PI * actualR; // 1/4鍦嗗姬
        float sideLen = rectH - 2 * actualR;   // 渚ц竟鐩寸嚎

        // 瀹氫箟椤烘椂閽堥『搴忥細涓婅竟 -> 鍙充笂瑙?-> 鍙宠竟 -> 鍙充笅瑙?-> 涓嬭竟 -> 宸︿笅瑙?-> 宸﹁竟 -> 宸︿笂瑙?
        // 鍧愭爣绯伙細涓績(0,0)锛屼笂Y姝ｏ紝鍙砐姝?

        // 1. Top Edge (涓婅竟鐩寸嚎)
        // 璧风偣: (-w/2+R, h/2) -> 缁堢偣: (w/2-R, h/2)
        if (dist < topLen)
        {
            float t = dist; // 灞€閮ㄨ窛绂?
            return new Vector3(-xMax + actualR + t, yMax, 0);
        }
        dist -= topLen;

        // 2. Top-Right Corner (鍙充笂瑙?
        // 鍦嗗績: (w/2-R, h/2-R)
        if (dist < cornerLen)
        {
            float angle = Mathf.Lerp(90f, 0f, dist / cornerLen) * Mathf.Deg2Rad;
            return new Vector3(xMax - actualR + Mathf.Cos(angle) * actualR, yMax - actualR + Mathf.Sin(angle) * actualR, 0);
        }
        dist -= cornerLen;

        // 3. Right Edge (鍙宠竟鐩寸嚎)
        // (w/2, h/2-R) -> (w/2, -h/2+R)
        if (dist < sideLen)
        {
            return new Vector3(xMax, yMax - actualR - dist, 0);
        }
        dist -= sideLen;

        // 4. Bottom-Right Corner (鍙充笅瑙?
        if (dist < cornerLen)
        {
            float angle = Mathf.Lerp(0f, -90f, dist / cornerLen) * Mathf.Deg2Rad;
            return new Vector3(xMax - actualR + Mathf.Cos(angle) * actualR, -yMax + actualR + Mathf.Sin(angle) * actualR, 0);
        }
        dist -= cornerLen;

        // 5. Bottom Edge (涓嬭竟鐩寸嚎)
        // (w/2-R, -h/2) -> (-w/2+R, -h/2)
        if (dist < topLen)
        {
            return new Vector3(xMax - actualR - dist, -yMax, 0);
        }
        dist -= topLen;

        // 6. Bottom-Left Corner (宸︿笅瑙?
        if (dist < cornerLen)
        {
            float angle = Mathf.Lerp(-90f, -180f, dist / cornerLen) * Mathf.Deg2Rad;
            return new Vector3(-xMax + actualR + Mathf.Cos(angle) * actualR, -yMax + actualR + Mathf.Sin(angle) * actualR, 0);
        }
        dist -= cornerLen;

        // 7. Left Edge (宸﹁竟鐩寸嚎)
        // (-w/2, -h/2+R) -> (-w/2, h/2-R)
        if (dist < sideLen)
        {
            return new Vector3(-xMax, -yMax + actualR + dist, 0);
        }
        dist -= sideLen;

        // 8. Top-Left Corner (宸︿笂瑙?
        // 鍓╀綑鐨勮窛绂婚兘鍦ㄨ繖閲?
        float finalAngle = Mathf.Lerp(180f, 90f, dist / cornerLen) * Mathf.Deg2Rad;
        return new Vector3(-xMax + actualR + Mathf.Cos(finalAngle) * actualR, yMax - actualR + Mathf.Sin(finalAngle) * actualR, 0);
    }
}