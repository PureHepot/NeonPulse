using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SlimePhysicsRuntime))]
public class SlimeJumpAI : MonoBehaviour
{
    private SlimePhysicsRuntime physicsRuntime;
    private Rigidbody2D centerRB;

    [Header("移动设置")]
    public float moveSpeed = 400f;

    [Header("弹射跳跃参数")]
    public float jumpForce = 25f;
    public float jumpInterval = 3f;

    [Header("蓄力表现")]
    public float chargeTime = 1f;     // 稍微增加一点蓄力时间，让渐变过程更明显
    public float maxSquishForce = 120f; // 最大下压力（终点值）
    public AnimationCurve chargeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 蓄力曲线，让力变化更自然

    [Range(0f, 1f)]
    public float edgeAssist = 0.8f;

    [Header("环境检测")]
    public LayerMask groundLayer;
    public float groundCheckDist = 3.5f;

    private float timer;
    private bool isGrounded;
    private bool isJumping = false;

    void Start()
    {
        physicsRuntime = GetComponent<SlimePhysicsRuntime>();
        centerRB = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        CheckGrounded();

        if (isJumping) return;

        timer += Time.fixedDeltaTime;
        if (timer > jumpInterval && isGrounded)
        {
            StartCoroutine(ElasticJumpRoutine());
            timer = 0;
        }

        if (isGrounded)
        {
            // 平时保持轻微的下压力，维持贴地感
            centerRB.AddForce(Vector2.down * 30f);

            float moveDir = Mathf.Sin(Time.time * 2f);
            centerRB.AddForce(Vector2.right * moveDir * moveSpeed);
            physicsRuntime.ApplyAmbientWobble(1.5f);
        }
    }

    IEnumerator ElasticJumpRoutine()
    {
        isJumping = true;

        // === 阶段1：渐进式蓄力 (Progressive Squash) ===
        float elapsed = 0f;

        while (elapsed < chargeTime)
        {
            // 计算当前进度 (0 ~ 1)
            float t = elapsed / chargeTime;

            // 使用曲线采样，让力的变化更自然（例如先慢后快）
            // 如果不想用曲线，也可以直接用 float currentForce = Mathf.Lerp(0, maxSquishForce, t);
            float curveValue = chargeCurve.Evaluate(t);
            float currentForce = Mathf.Lerp(0, maxSquishForce, curveValue);

            // 施加渐变的下压力
            centerRB.AddForce(Vector2.down * currentForce);

            // 边缘点也施加同样的渐变力
            foreach (var p in physicsRuntime.points)
            {
                if (p) p.GetComponent<Rigidbody2D>().AddForce(Vector2.down * currentForce * 0.5f);
            }

            // 随着蓄力增加，波动更加剧烈
            physicsRuntime.ApplyAmbientWobble(2f + 3f * t);

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // === 阶段2：释放与爆发 (Launch) ===
        Vector2 jumpDir = Vector2.up + new Vector2(Random.Range(-0.2f, 0.2f), 0);
        jumpDir.Normalize();

        centerRB.AddForce(jumpDir * jumpForce, ForceMode2D.Impulse);

        foreach (var t in physicsRuntime.points)
        {
            if (t)
            {
                Rigidbody2D rb = t.GetComponent<Rigidbody2D>();
                rb.AddForce(jumpDir * jumpForce * edgeAssist, ForceMode2D.Impulse);
                rb.drag = 0.5f;
            }
        }

        yield return new WaitForSeconds(0.2f);
        isJumping = false;
    }

    void CheckGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDist, groundLayer);
        isGrounded = hit.collider != null;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDist);
    }
}