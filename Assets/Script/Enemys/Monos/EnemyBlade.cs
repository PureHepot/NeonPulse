using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnemyBlade : EnemyBase
{
    [Header("Blade Settings")]
    public float rotationSpeedIdle = 180f; // 姝ｅ父鑷浆閫熷害
    public float rotationSpeedAttack = 720f; // 鏀诲嚮鑷浆閫熷害
    public float aggroRange = 1.0f; // 绱㈡晫鑼冨洿

    [Header("Movement")]
    public float enterSpeed = 3f;
    public Vector2 centerAreaSize = new Vector2(10, 6); // 灞忓箷涓績鍖哄煙澶у皬

    [Header("Attack Stats")]
    public float slashSpeed = 25f; // 鍐插埡閫熷害
    public float turnRate = 5f;    // 杞悜鐜?(寮х嚎寮洸绋嬪害)
    public float attackDuration = 1.5f; // 鍗曟鍐查攱鏈€澶ф寔缁椂闂?
    public float missRecoveryTime = 1.0f; // 鏈懡涓殑杩熺紦鏃堕棿
    public int attacksPerRound = 3; // 涓€杞敾鍑诲啿鍑犳

    [Header("Colors (HDR)")]
    [ColorUsage(true, true)] public Color aggroColor = new Color(1, 0, 0.2f) * 4f; // 鏀诲嚮鑹?(绾㈣壊楂樹寒)

    // 鍐呴儴鐘舵€?
    private enum State { Entering, Prowling, AggroTrans, Slashing, Recovering, Bouncing }
    private State currentState;
    private float stateTimer;
    private int currentAttackCount;
    private Vector2 targetDir;

    // 寮曠敤
    private TrailRenderer trail;

    public override void OnSpawn()
    {
        base.OnSpawn();

        currentState = State.Entering;
        currentAttackCount = 0;

        if (!trail) trail = GetComponentInChildren<TrailRenderer>();
        if (trail)
        {
            trail.Clear();
            trail.emitting = false;
        }

        // 闅忔満涓€涓睆骞曚腑蹇冪殑鐩爣鐐逛綔涓哄叆鍦虹粓鐐?
        Vector2 randomCenter = new Vector2(
            Random.Range(-centerAreaSize.x / 2, centerAreaSize.x / 2),
            Random.Range(-centerAreaSize.y / 2, centerAreaSize.y / 2)
        );
        targetDir = (randomCenter - (Vector2)transform.position).normalized;
    }

    protected override void MoveBehavior()
    {
        // 鎸佺画鑷浆 (鏍规嵁鐘舵€佹敼鍙橀€熷害)
        float currentRotSpeed = (currentState == State.Slashing || currentState == State.Bouncing)
                                ? rotationSpeedAttack : rotationSpeedIdle;
        transform.Rotate(0, 0, -currentRotSpeed * Time.deltaTime);

        // 鐘舵€佹満
        switch (currentState)
        {
            case State.Entering:
                HandleEntering();
                break;
            case State.Prowling:
                HandleProwling();
                break;
            case State.AggroTrans:
                break;
            case State.Slashing:
                HandleSlashing();
                break;
            case State.Recovering:
                HandleRecovering();
                break;
            case State.Bouncing:
                // 鐗╃悊鍙嶅脊涓紝浠呯瓑寰呴€熷害琛板噺
                if (rb.velocity.magnitude < 5f)
                {
                    StartNextAttackOrReset();
                }
                break;
        }
    }

    void HandleEntering()
    {
        DriveVelocity(targetDir * enterSpeed, 1.5f);

        // 妫€娴嬫槸鍚﹀埌杈句腑蹇冨尯鍩?(绠€鍗曡窛绂诲垽瀹氾紝鎴栬€呭垽鏂槸鍚﹁繘鍏ュ睆骞曡寖鍥?
        if (Mathf.Abs(transform.position.x) < centerAreaSize.x / 2 + 2f &&
            Mathf.Abs(transform.position.y) < centerAreaSize.y / 2 + 2f)
        {
            currentState = State.Prowling;
            DriveVelocity(rb.velocity.normalized * (enterSpeed * 0.5f), 1.2f); // 鍑忛€熷贰閫?
        }
    }

    void HandleProwling()
    {
        // 绠€鍗曠殑鎯€ф父鑽★紝纰板埌澧欏鍙嶅脊鐢辩墿鐞嗘潗璐ㄥ鐞嗭紝杩欓噷鍙仛绱㈡晫
        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= aggroRange)
            {
                StartAggro();
            }
        }
    }

    void StartAggro()
    {
        currentState = State.AggroTrans;
        StopMovementDrive(); // 鍋滀笅钃勫姏

        // 棰滆壊娓愬彉 -> 鍙樼孩
        if (bodyRenderer)
        {
            bodyRenderer.DOKill();
            bodyRenderer.DOColor(aggroColor, 0.5f);
        }

        // 钃勫姏鎶栧姩
        transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 10, 1).OnComplete(() =>
        {
            currentAttackCount = 0;
            StartSlashAttack();
        });
    }

    void StartSlashAttack()
    {
        currentState = State.Slashing;
        stateTimer = attackDuration;

        // 寮€鍚嫋灏?
        if (trail) trail.emitting = true;

        if (playerTransform != null)
        {
            // 鍒濆鍐查攱鏂瑰悜锛氱◢寰鍒や竴鐐圭偣锛屾垨鑰呯洿鎺ユ寚鍚戠帺瀹?
            targetDir = (playerTransform.position - transform.position).normalized;
            SnapVelocity(targetDir * slashSpeed);
        }
    }

    void HandleSlashing()
    {
        stateTimer -= Time.deltaTime;

        if (playerTransform != null)
        {
            // --- 鏍稿績锛氬姬绾胯繍鍔ㄩ€昏緫 ---
            // 绫讳技浜庡寮瑰埗瀵硷紝涓嶆柇淇閫熷害鏂瑰悜鎸囧悜鐜╁锛屼絾闄愬埗淇鐜?TurnRate)
            // 杩欐牱濡傛灉閫熷害澶熷揩锛屽畠灏变細鍒掑嚭涓€閬撳姬绾胯€屼笉鏄洿绾?

            Vector2 desiredDir = (playerTransform.position - transform.position).normalized;

            // 浣跨敤 RotateTowards 骞虫粦杞悜
            // 杩欓噷鐨?step 鏄姬搴︼紝TurnRate 瓒婂ぇ鎷愬集瓒婃€?
            Vector2 currentVelocity = rb.velocity;
            Vector2 currentDir = currentVelocity.sqrMagnitude > 0.0001f ? currentVelocity.normalized : targetDir;
            Vector2 newDir = Vector3.RotateTowards(currentDir, desiredDir, turnRate * Time.deltaTime, 0f);

            DriveVelocity(newDir * slashSpeed, 3f);
        }

        // 瓒呮椂鏈懡涓?(Miss)
        if (stateTimer <= 0)
        {
            EnterMissRecovery();
        }
    }

    void EnterMissRecovery()
    {
        currentState = State.Recovering;
        stateTimer = missRecoveryTime;

        StopMovementDrive();

        // 寮哄姏鍒硅溅
        rb.drag = 5f;

        if (trail) trail.emitting = false;

        // 瑙嗚锛氶鑹茬◢寰殫娣′竴鐐硅〃绀哄枠鎭? (鍙€?
    }

    void HandleRecovering()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            rb.drag = 0f; // 鎭㈠闃诲姏
            StartNextAttackOrReset();
        }
    }

    void StartNextAttackOrReset()
    {
        currentAttackCount++;
        if (currentAttackCount < attacksPerRound)
        {
            // 缁х画涓嬩竴娆″啿閿?
            StartSlashAttack();
        }
        else
        {
            // 鏀诲嚮杞缁撴潫锛屽洖鍒版甯哥姸鎬?
            currentState = State.Prowling;
            currentAttackCount = 0;

            // 棰滆壊鎭㈠
            if (bodyRenderer) bodyRenderer.DOColor(normalColor, 1f);

            // 绋嶅井杩滅鐜╁涓€鐐癸紝闃叉璐磋劯涓嶅姩
            Vector2 retreatDir = (transform.position - playerTransform.position).normalized;
            SnapVelocity(retreatDir * enterSpeed);
        }
    }


    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        var shield = collision.collider.gameObject.GetComponent<ShieldController>();
        if (shield != null)
        {
            return;
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            var health = collision.gameObject.GetComponentInChildren<HealthModule>();
            if (health != null) health.TakeDamage(contactDamage, transform);

            if (currentState == State.Slashing)
            {
                currentState = State.Bouncing;
                if (trail) trail.emitting = false;

                // 璁＄畻鍙嶅脊鏂瑰悜锛氭部鐫€娉曠嚎鍙嶅皠
                Vector2 normal = collision.contacts[0].normal;
                Vector2 reflectDir = Vector2.Reflect(rb.velocity.normalized, normal);

                // 鏂藉姞鍙嶅脊鍔?
                SnapVelocity(reflectDir * (slashSpeed * 0.6f)); // 绋嶅井闄嶉€熷弽寮?
            }
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("ArenaWall"))
        {
            if (currentState == State.Slashing)
            {
                EnterMissRecovery();
                SnapVelocity(collision.contacts[0].normal * 5f);
            }
        }

        base.OnCollisionEnter2D(collision);
    }
}
