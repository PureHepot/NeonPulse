using System.Collections;
using DG.Tweening;
using UnityEngine;

public class MovementModule : PlayerModule
{
    private const string MoveSpeedStatId = "move.speed";
    private const string DashCooldownStatId = "move.dashcooldown";
    private const string DashForceStatId = "move.dashforce";

    [Header("Move Settings")]
    public float smoothTime = 0.15f;

    [Header("Dash Settings")]
    public float dashForce = 20f;
    public float dashDuration = 0.22f;
    public AnimationCurve dashSpeedCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));

    [Header("Dash Collision Layers")]
    public string playerLayerName = "Player";
    public string enemyLayerName = "Enemy";
    public string enemyBulletLayerName = "EnemyBullet";

    [Header("Afterimage")]
    public float afterimageInterval = 0.04f;
    public float afterimageLifetime = 0.2f;
    [Range(0f, 1f)] public float afterimageAlpha = 0.55f;
    public float afterimageScaleMultiplier = 1f;

    private float baseMoveSpeed;
    private float speedMultiplier = 1f;
    private Vector2 currentVelocity;
    private Vector2 refVelocity;
    private float dashCooldown;
    private float lastDashTime = -999f;
    private Vector2 dashDirection;
    private int playerLayerId;
    private int enemyLayerId;
    private int enemyBulletLayerId;
    private Coroutine dashRoutine;
    private Coroutine afterimageRoutine;

    protected override void OnInitialize()
    {
        RecalculateStats();
        playerLayerId = LayerMask.NameToLayer(playerLayerName);
        enemyLayerId = LayerMask.NameToLayer(enemyLayerName);
        enemyBulletLayerId = LayerMask.NameToLayer(enemyBulletLayerName);
    }

    protected override void OnActivate()
    {
        RecalculateStats();
    }

    protected override void OnDeactivate()
    {
        StopDashState();
    }

    public override void OnModuleUpdate()
    {
        if (player == null || player.IsDead || player.IsStunned || !HasControl)
            return;

        if (!player.IsDashing && InputManager.Instance.Space() && IsDashReady())
            dashRoutine = StartCoroutine(DashRoutine());

        if (player.IsDashing)
            return;

        float x = InputManager.Instance.GetMoveX();
        float y = InputManager.Instance.GetMoveY();
        Vector2 input = new Vector2(x, y);

        Vector2 targetVelocity = input.normalized * GetFinalSpeed();
        currentVelocity = Vector2.SmoothDamp(currentVelocity, targetVelocity, ref refVelocity, smoothTime);
        player.SetVelocity(currentVelocity);
    }

    private bool IsDashReady()
    {
        return Time.time >= lastDashTime + dashCooldown + dashDuration;
    }

    private void RecalculateStats()
    {
        baseMoveSpeed = GetStat(MoveSpeedStatId, 5f);
        dashCooldown = Mathf.Max(0f, GetStat(DashCooldownStatId, 0.3f));
        dashForce = Mathf.Max(baseMoveSpeed, GetStat(DashForceStatId, dashForce));
        speedMultiplier = 1f;
    }

    private float GetFinalSpeed()
    {
        return baseMoveSpeed * speedMultiplier;
    }

    private IEnumerator DashRoutine()
    {
        BeginDashState();

        dashDirection = new Vector2(InputManager.Instance.GetMoveX(), InputManager.Instance.GetMoveY()).normalized;
        if (dashDirection.sqrMagnitude < 0.01f)
        {
            Vector3 mouseDirection = MUtils.GetMouseWorldPosition() - player.transform.position;
            dashDirection = new Vector2(mouseDirection.x, mouseDirection.y).normalized;
        }

        if (dashDirection.sqrMagnitude < 0.01f)
            dashDirection = Vector2.right;

        player.AddImpulse(dashDirection * dashForce);

        float timer = 0f;
        while (timer < dashDuration)
        {
            timer += DeltaTime;
            float progress = Mathf.Clamp01(timer / dashDuration);
            float speedFactor = dashSpeedCurve != null ? dashSpeedCurve.Evaluate(progress) : 1f;
            float currentDashSpeed = Mathf.Lerp(GetFinalSpeed(), dashForce, speedFactor);
            player.SnapVelocity(dashDirection * currentDashSpeed);
            yield return null;
        }

        dashRoutine = null;
        StopDashState();
    }

    private void BeginDashState()
    {
        lastDashTime = Time.time;
        player.IsDashing = true;
        currentVelocity = Vector2.zero;
        refVelocity = Vector2.zero;

        AudioManager.Instance?.PlayEffect("Dash");

        if (playerLayerId >= 0 && enemyLayerId >= 0)
            Physics2D.IgnoreLayerCollision(playerLayerId, enemyLayerId, true);

        if (playerLayerId >= 0 && enemyBulletLayerId >= 0)
            Physics2D.IgnoreLayerCollision(playerLayerId, enemyBulletLayerId, true);

        if (afterimageRoutine != null)
            StopCoroutine(afterimageRoutine);
        afterimageRoutine = StartCoroutine(SpawnAfterimageRoutine());
    }

    private void StopDashState()
    {
        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
        }

        if (afterimageRoutine != null)
        {
            StopCoroutine(afterimageRoutine);
            afterimageRoutine = null;
        }

        if (player != null)
            player.IsDashing = false;

        if (playerLayerId >= 0 && enemyLayerId >= 0)
            Physics2D.IgnoreLayerCollision(playerLayerId, enemyLayerId, false);

        if (playerLayerId >= 0 && enemyBulletLayerId >= 0)
            Physics2D.IgnoreLayerCollision(playerLayerId, enemyBulletLayerId, false);
    }

    private IEnumerator SpawnAfterimageRoutine()
    {
        while (player != null && player.IsDashing)
        {
            SpawnAfterimage();
            yield return new WaitForSeconds(afterimageInterval);
        }

        afterimageRoutine = null;
    }

    private void SpawnAfterimage()
    {
        var sourceRenderer = player != null ? player.BodyRenderer : null;
        if (sourceRenderer == null || sourceRenderer.sprite == null)
            return;

        var ghostObject = new GameObject("DashAfterimage");
        ghostObject.transform.position = sourceRenderer.transform.position;
        ghostObject.transform.rotation = sourceRenderer.transform.rotation;
        ghostObject.transform.localScale = sourceRenderer.transform.lossyScale * afterimageScaleMultiplier;

        var ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();
        ghostRenderer.sprite = sourceRenderer.sprite;
        ghostRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        ghostRenderer.sortingOrder = sourceRenderer.sortingOrder - 1;
        ghostRenderer.flipX = sourceRenderer.flipX;
        ghostRenderer.flipY = sourceRenderer.flipY;
        ghostRenderer.material = sourceRenderer.material;

        Color ghostColor = sourceRenderer.color;
        ghostColor.a *= afterimageAlpha;
        ghostRenderer.color = ghostColor;

        ghostRenderer.DOFade(0f, afterimageLifetime).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            if (ghostObject != null)
                Destroy(ghostObject);
        });
    }
}
