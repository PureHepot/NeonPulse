using UnityEngine;

public class EnemyElectric : EnemyBase
{
    [Header("Electric Settings")]
    public Vector3 enemyScale = new Vector3(0.8f, 0.8f, 1f);

    [Header("移动参数")]
    public float moveSpeedToCenter = 2f;
    public float reachDistance = 0.3f;
    public float centerStayOffset = 8f;

    [Header("电流场")]
    public GameObject electricAuraObj; // 电流范围特效+触发器

    private float camHalfWidth;
    private float camHalfHeight;

    private Vector3 targetCenterPos;
    private bool isReachCenter = false;

    public override void OnSpawn()
    {
        base.OnSpawn();

        transform.localScale = enemyScale;
        StopMovementDrive(true);
        isReachCenter = false;

        InitCameraBounds();
        CalculateTargetCenterPos();

        if (electricAuraObj != null)
            electricAuraObj.SetActive(false);
    }
    private void Update()
    {
        if (!isDead)
        {
            transform.localScale = enemyScale;
        }
    }

    protected override void MoveBehavior()
    {
        if (isDead)
        {
            StopMovementDrive();
            return;
        }

        if (!isReachCenter)
        {
            MoveToCenter();
            return;
        }

        StopMovementDrive(true);
    }

    private void MoveToCenter()
    {
        Vector2 direction = (targetCenterPos - transform.position).normalized;
        DriveVelocity(direction * moveSpeedToCenter, 1.2f);

        float distance = Vector2.Distance(transform.position, targetCenterPos);
        if (distance < reachDistance)
        {
            isReachCenter = true;
            StopMovementDrive(true);
            transform.position = targetCenterPos;

            DeployElectricField();
        }
    }

    private void DeployElectricField()
    {
        if (electricAuraObj != null)
        {
            electricAuraObj.SetActive(true);
        }
    }

    private void CalculateTargetCenterPos()
    {
        float safeX = camHalfWidth - centerStayOffset;
        float safeY = camHalfHeight - centerStayOffset;

        float x = Random.Range(-safeX, safeX);
        float y = Random.Range(-safeY, safeY);

        targetCenterPos = new Vector3(x, y, 0);
    }

    private void InitCameraBounds()
    {
        Camera cam = Camera.main;
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        camHalfWidth = camWidth / 2f;
        camHalfHeight = camHeight / 2f;
    }

    public override void OnDespawn()
    {
        base.OnDespawn();

        StopMovementDrive(true);
        isReachCenter = false;

        if (electricAuraObj != null)
            electricAuraObj.SetActive(false);
    }
}
