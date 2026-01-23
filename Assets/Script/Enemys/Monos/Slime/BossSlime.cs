//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using DG.Tweening;

//public class BossSlime : EnemyBase
//{
//    [Header("Boss Settings")]
//    public float rollSpeed = 10f;       // 滚动速度
//    public float bounceSpeedX = 8f;     // 弹跳时的横向速度
//    public float gravityStrength = 3f;  // 重力切换时的力度

//    [Header("Split Settings")]
//    public GameObject smallSlimePrefab; // 小史莱姆预制体
//    public int splitThreshold = 20;     // 每受到多少伤害分裂一次
//    public float sizeReduction = 0.9f;  // 每次分裂缩小的比例
//    public float minSize = 0.5f;        // 最小尺寸

//    [Header("Arena Bounds")]
//    public Vector2 arenaSize = new Vector2(16, 9); // 战斗区域大小(假设屏幕中心是0,0)

//    // --- 状态机 ---
//    private BossBaseState currentState;
//    public SlimeSpawnState spawnState = new SlimeSpawnState();
//    //public SlimeRollState rollState = new SlimeRollState();
//    //public SlimeBounceState bounceState = new SlimeBounceState();

//    // --- 运行时数据 ---
//    private float lastSplitHp; // 上一次分裂时的血量
//    private Camera mainCam;

//    // 获取中心刚体（SlimeBuilder生成的那个）
//    public Rigidbody2D CenterRB => rb;

//    public override void OnSpawn()
//    {
//        base.OnSpawn();

//        mainCam = Camera.main;
//        lastSplitHp = maxHp;

//        // 初始无重力
//        rb.gravityScale = 0;

//        // 启动状态机：出生状态
//        ChangeState(spawnState);
//    }

//    protected override void MoveBehavior()
//    {
//        if (currentState != null)
//            currentState.LogicUpdate();
//    }

//    private void FixedUpdate()
//    {
//        if (isDead) return;
//        MoveBehavior();

//        if (currentState != null)
//            currentState.PhysicsUpdate();
//    }

//    public void ChangeState(BossBaseState newState)
//    {
//        if (currentState != null) currentState.Exit();
//        currentState = newState;
//        currentState.Enter(this);
//    }

//    // --- 核心：重写受击逻辑 (分裂) ---
//    public new void TakeDamage(int amount)
//    {
//        base.TakeDamage(amount); // 扣血

//        // 检查是否达到分裂阈值
//        if (lastSplitHp - currentHp >= splitThreshold)
//        {
//            Split();
//            lastSplitHp = currentHp; // 重置阈值计数
//        }
//    }

//    private void Split()
//    {
//        // 1. 变小
//        if (transform.localScale.x > minSize)
//        {
//            transform.DOScale(transform.localScale * sizeReduction, 0.3f).SetEase(Ease.OutBack);
//        }

//        // 2. 生成小史莱姆
//        if (smallSlimePrefab != null)
//        {
//            // 在随机稍微偏移一点的位置生成，防止卡在Boss身体里
//            Vector3 spawnPos = transform.position + (Vector3)Random.insideUnitCircle * 2f;
//            GameObject minion = ObjectPoolManager.Instance.Get(smallSlimePrefab, spawnPos, Quaternion.identity);

//            // 给小史莱姆一个初速度弹开
//            Rigidbody2D minionRb = minion.GetComponent<Rigidbody2D>();
//            if (minionRb != null)
//            {
//                Vector2 popDir = (spawnPos - transform.position).normalized;
//                minionRb.AddForce(popDir * 10f, ForceMode2D.Impulse);
//            }
//        }

//        // 播放分裂音效或特效...
//    }

//    // 简单的Debug画框，方便你在Scene里看Boss移动范围
//    private void OnDrawGizmosSelected()
//    {
//        Gizmos.color = Color.yellow;
//        Gizmos.DrawWireCube(Vector3.zero, arenaSize);
//    }
//}
