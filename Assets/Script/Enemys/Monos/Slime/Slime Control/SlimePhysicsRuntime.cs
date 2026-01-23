using UnityEngine;
using System.Collections.Generic;

public class SlimePhysicsRuntime : MonoBehaviour
{
    [HideInInspector] public List<Transform> points;
    [HideInInspector] public Rigidbody2D centerRB;

    [Header("基础参数")]
    [HideInInspector] public float baseDrag;
    [HideInInspector] public float idealDeltaAngle;

    [Header("自定义重力系统")]
    public Vector2 gravityDirection = Vector2.down;
    public float gravityMagnitude = 30f;

    [Header("物理开关")]
    public bool enablePhysicsSimulation = false;
    public bool enableGravity = true;

    [Header("径向限制 (中心距离)")]
    public float hardLimitRadius;
    public float cushionRadius;
    public float maxRadialDrag = 20f;
    public float radialPushForce = 50f;

    [Header("切向限制 (边缘夹角)")]
    [HideInInspector] public float minAngleLimit;
    [HideInInspector] public float cushionAngleLimit;

    [Header("波动效果")]
    public float wobbleSpeed = 3f; // 波动频率
    public float wobbleScale = 0.5f; // 噪点采样跨度

    public float maxAngularRepulsion = 40f;
    public float maxAngularBraking = 5f;

    [Header("表面张力 (防止折叠)")]
    // 新增：弯曲刚度，值越大越难折叠
    public float bendingStiffness = 60f;
    // 新增：平滑度，防止锐角 (0~1)，0.5表示试图保持直线
    [Range(0f, 1f)] public float surfaceSmoothness = 0.5f;

    private List<Rigidbody2D> allRBs = new List<Rigidbody2D>();
    private List<Rigidbody2D> pointRBs = new List<Rigidbody2D>();

    public void Initialize()
    {
        if (centerRB) allRBs.Add(centerRB);
        foreach (var t in points) if (t)
            {
                allRBs.Add(t.GetComponent<Rigidbody2D>());
                pointRBs.Add(t.GetComponent<Rigidbody2D>());
            }

        foreach (var rb in allRBs) { rb.gravityScale = 0; }
    }

    void FixedUpdate()
    {
        if (centerRB == null) return;
        //波动效果
        ApplyAmbientWobble(enablePhysicsSimulation ? 1f : 0.5f);
        
        if (!enablePhysicsSimulation) return;
        if (enableGravity)
        {
            Vector2 gravityForce = gravityDirection.normalized * gravityMagnitude;
            foreach (var rb in allRBs)
            {
                //重力模拟
                rb.AddForce(gravityForce * rb.mass);
            }
        }
        

        ApplyConstraints();
    }

    void ApplyConstraints()
    {
        Vector2 centerPos = centerRB.position;
        int count = pointRBs.Count;
        for (int i = 0; i < count; i++)
        {
            Rigidbody2D currentRB = pointRBs[i];
            ApplyRadialConstraint(currentRB, centerPos);

            Rigidbody2D nextRB = pointRBs[(i + 1) % count];
            ApplyAngularConstraint(currentRB, nextRB, centerPos);

            Rigidbody2D prevRB = pointRBs[(i - 1 + count) % count];
            ApplyBendingConstraint(prevRB, currentRB, nextRB, centerPos);
        }
    }

    public void AddForceToAll(Vector2 force, ForceMode2D mode = ForceMode2D.Force)
    {
        foreach (var rb in allRBs) rb.AddForce(force, mode);
    }

    // 径向阻尼 (防塌缩)
    void ApplyRadialConstraint(Rigidbody2D rb, Vector2 center)
    {
        Vector2 dir = rb.position - center;
        float dist = dir.magnitude;
        Vector2 normal = dir.normalized;

        if (dist < hardLimitRadius)
        {
            rb.AddForce(normal * radialPushForce, ForceMode2D.Impulse);
            rb.drag = 0;
        }
        else if (dist < cushionRadius)
        {
            float t = 1f - (dist - hardLimitRadius) / (cushionRadius - hardLimitRadius);
            rb.drag = Mathf.Lerp(baseDrag, maxRadialDrag, t);
        }
        else
        {
            rb.drag = baseDrag;
        }
    }

    /// <summary>
    /// 施加环境波动 (每帧调用)
    /// </summary>
    /// <param name="intensity">波动强度 (0~1)</param>
    public void ApplyAmbientWobble(float intensity)
    {
        if (centerRB == null || allRBs.Count == 0) return;

        float time = Time.time * wobbleSpeed;

        for (int i = 0; i < allRBs.Count; i++)
        {
            Rigidbody2D rb = allRBs[i];

            float noise = Mathf.PerlinNoise(time, i * wobbleScale) - 0.5f;

            // 沿着径向施加力
            Vector2 dir = (rb.position - centerRB.position).normalized;

            rb.AddForce(dir * noise * intensity * 50f);
        }
    }

    // 切向阻尼 (防乱序)
    void ApplyAngularConstraint(Rigidbody2D rb1, Rigidbody2D rb2, Vector2 center)
    {
        Vector2 dir1 = (rb1.position - center).normalized;
        Vector2 dir2 = (rb2.position - center).normalized;
        float angle = Vector2.Angle(dir1, dir2);
        Vector2 tangentDir = (rb2.position - rb1.position).normalized;

        if (angle < minAngleLimit)
        {
            float push = radialPushForce * 0.5f;
            rb1.AddForce(-tangentDir * push, ForceMode2D.Impulse);
            rb2.AddForce(tangentDir * push, ForceMode2D.Impulse);

            Vector2 v1Tangent = Vector2.Dot(rb1.velocity, tangentDir) * tangentDir;
            Vector2 v2Tangent = Vector2.Dot(rb2.velocity, tangentDir) * tangentDir;
            rb1.velocity -= v1Tangent * 0.5f;
            rb2.velocity -= v2Tangent * 0.5f;
        }
        else if (angle < cushionAngleLimit)
        {
            float t = 1f - (angle - minAngleLimit) / (cushionAngleLimit - minAngleLimit);
            float forceMag = Mathf.Lerp(0, maxAngularRepulsion, t);
            rb1.AddForce(-tangentDir * forceMag);
            rb2.AddForce(tangentDir * forceMag);

            Vector2 relVel = rb2.velocity - rb1.velocity;
            Vector2 dampingForce = relVel * (maxAngularBraking * t);
            rb2.AddForce(-dampingForce);
            rb1.AddForce(dampingForce);
        }
    }

    // 弯曲限制 (防折叠/防锐角)
    void ApplyBendingConstraint(Rigidbody2D prev, Rigidbody2D current, Rigidbody2D next, Vector2 center)
    {
        Vector2 p1 = prev.position;
        Vector2 p2 = current.position;
        Vector2 p3 = next.position;

        // 1. 计算 Prev 到 Next 的向量 (基准线)
        Vector2 baseline = p3 - p1;
        Vector2 baselineDir = baseline.normalized;

        // 2. 计算基准线的中点
        Vector2 midpoint = (p1 + p3) * 0.5f;

        // 3. 计算 Current 实际上在基准线的哪里
        // 我们通过向基准线的法线方向投影来判断
        // 计算法线 (指向外侧)
        Vector2 normal = new Vector2(-baselineDir.y, baselineDir.x);

        // 确保法线是指向远离圆心的一侧 (修正方向)
        if (Vector2.Dot(normal, midpoint - center) < 0)
        {
            normal = -normal;
        }

        // 4. 计算 Current 到 Midpoint 的距离向量
        Vector2 offset = p2 - midpoint;

        // 5. 投影到法线上，正数表示凸(在外侧)，负数表示凹(在内侧)
        float height = Vector2.Dot(offset, normal);

        // 我们希望 height 至少是正的 (凸的)
        // 甚至希望它保持一定的曲率 (idealHeight)
        // idealHeight 可以简单理解为：在完美圆形下，圆弧高度是多少
        // 这里简化处理：只要 height < 0 (内凹) 或者 height 很小 (太直/锐角)，就推出去

        // 允许的最小拱起高度 (基于两点间距，模拟曲率)
        float minHeight = baseline.magnitude * surfaceSmoothness * 0.2f;

        if (height < minHeight)
        {
            // 计算修正力：推向法线方向
            // 凹陷得越深，推力越大
            float depth = minHeight - height;
            Vector2 correctionForce = normal * (depth * bendingStiffness);

            // 施加给中间点
            current.AddForce(correctionForce);

            // 根据牛顿第三定律，给两边施加反作用力 (把它俩往里拉，或者往下压)
            // 这样能让三角形"鼓"起来
            prev.AddForce(-correctionForce * 0.5f);
            next.AddForce(-correctionForce * 0.5f);

            // 紧急阻尼：如果发生折叠，迅速杀掉指向圆心的速度，防止震荡
            Vector2 radialVel = Vector2.Dot(current.velocity, (center - p2).normalized) * (center - p2).normalized;
            current.velocity -= radialVel * 0.1f;
        }
    }
}