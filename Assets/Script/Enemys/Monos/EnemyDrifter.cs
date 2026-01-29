using UnityEngine;

public class EnemyDrifter : EnemyBase
{
    [Header("Movement Stats")]
    public float initialSpread = 3.0f;  // 初始瞄准的偏差半径 (值越大，一开始越歪)
    public float steerStrength = 0.5f;

    [Header("Screen Wrap")]
    public float edgePadding = -0.5f;

    private Vector2 moveDirection;
    private Vector2 screenBounds;

    private float limitTime = 5f;

    public override void OnSpawn()
    {
        base.OnSpawn();
        limitTime = 5f;
        Camera cam = Camera.main;
        float height = cam.orthographicSize;
        float width = height * cam.aspect;
        screenBounds = new Vector2(width, height);

        Vector2 targetPos;
        if (playerTransform != null)
        {
            Vector2 randomOffset = Random.insideUnitCircle * initialSpread;
            targetPos = (Vector2)playerTransform.position + randomOffset;
        }
        else
        {
            targetPos = (Vector2)transform.position + Vector2.left;
        }

        moveDirection = (targetPos - (Vector2)transform.position).normalized;
    }

    protected override void MoveBehavior()
    {
        if (playerTransform != null)
        {
            Vector2 idealDir = (playerTransform.position - transform.position).normalized;

            Vector3 newDir = Vector3.RotateTowards(moveDirection, idealDir, steerStrength * Time.deltaTime, 0f);

            moveDirection = newDir.normalized;
        }

        rb.velocity = moveDirection * moveSpeed;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        if(limitTime < 0) HandleScreenWrap();
        else limitTime -= Time.deltaTime;
    }

    void HandleScreenWrap()
    {
        Vector3 pos = transform.position;
        bool wrapped = false;

        if (pos.x > screenBounds.x + edgePadding)
        {
            pos.x = -screenBounds.x - edgePadding;
            wrapped = true;
        }
        else if (pos.x < -screenBounds.x - edgePadding)
        {
            pos.x = screenBounds.x + edgePadding;
            wrapped = true;
        }

        if (pos.y > screenBounds.y + edgePadding)
        {
            pos.y = -screenBounds.y - edgePadding;
            wrapped = true;
        }
        else if (pos.y < -screenBounds.y - edgePadding)
        {
            pos.y = screenBounds.y + edgePadding;
            wrapped = true;
        }

        if (wrapped)
        {
            transform.position = pos;
        }
    }
}