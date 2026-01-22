using UnityEngine;
using UnityEngine.U2D;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(SlimePhysicsRuntime))]
[RequireComponent(typeof(SlimeSkinSync))]
public class SlimeBuilder : MonoBehaviour
{
    [Header("结构设置")]
    public SpriteShapeController skin;
    [Range(6, 60)] public int pointCount = 16;
    public float radius = 2.5f;

    [Header("基础物理")]
    public float pointMass = 0.5f;
    public float baseDrag = 1f;

    [Header("Spring (Q弹动力)")]
    public float frequency = 3f;
    public float dampingRatio = 0.5f;

    [Header("虚拟内核 (中心限制)")]
    public float hardCoreRadius = 1.0f; // 绝对核心
    public float softCoreRadius = 2.0f; // 缓冲带

    [Header("虚拟辐条 (角度限制)")]
    [Range(0.1f, 0.9f)]
    public float hardAngleRatio = 0.3f; // 最小角度比例 (例如0.3表示必须保留30%的理想间隔)
    [Range(0.1f, 0.9f)]
    public float softAngleRatio = 0.7f; // 缓冲角度比例 (进入70%间隔时开始产生阻力)

    public void GenerateSlime()
    {
        if (skin == null) return;
        ClearOld();

        // 1. 中心刚体
        Rigidbody2D centerRB = GetComponent<Rigidbody2D>();
        if (!centerRB) centerRB = gameObject.AddComponent<Rigidbody2D>();
        // 清理碰撞体，完全靠代码控制
        foreach (var c in GetComponents<Collider2D>()) DestroyImmediate(c);

        List<Transform> points = new List<Transform>();
        List<Rigidbody2D> rbs = new List<Rigidbody2D>();
        float step = 360f / pointCount;

        // 2. 生成边缘点
        for (int i = 0; i < pointCount; i++)
        {
            float rad = (i * step) * Mathf.Deg2Rad;
            Vector2 pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;

            GameObject p = new GameObject($"Point_{i}");
            p.transform.SetParent(transform);
            p.transform.localPosition = pos;
            p.layer = LayerMask.NameToLayer("EnemySkin");

            Rigidbody2D rb = p.AddComponent<Rigidbody2D>();
            rb.mass = pointMass;
            rb.drag = baseDrag;
            rb.gravityScale = 1f;
            rb.freezeRotation = true;

            // 只有视觉碰撞体，无物理交互
            CircleCollider2D col = p.AddComponent<CircleCollider2D>();
            col.radius = (radius * Mathf.PI / pointCount) * 0.5f;

            // 连向中心
            SpringJoint2D sp = p.AddComponent<SpringJoint2D>();
            sp.connectedBody = centerRB;
            sp.autoConfigureDistance = true;
            sp.frequency = frequency;
            sp.dampingRatio = dampingRatio;

            points.Add(p.transform);
            rbs.Add(rb);
        }

        // 3. 边缘连边缘 (履带结构) - 保持外形
        for (int i = 0; i < pointCount; i++)
        {
            int next = (i + 1) % pointCount;
            DistanceJoint2D dj = rbs[i].gameObject.AddComponent<DistanceJoint2D>();
            dj.connectedBody = rbs[next];
            dj.autoConfigureDistance = true;
            dj.maxDistanceOnly = false;
        }

        Spline spline = skin.spline;
        spline.Clear();

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 pointLocalPos = skin.transform.InverseTransformPoint(points[i].position);

            spline.InsertPointAt(i, pointLocalPos);

            spline.SetTangentMode(i, ShapeTangentMode.Continuous);
        }

        // 刷新 SpriteShape
        skin.RefreshSpriteShape();

        // 4. 传递参数给 Runtime
        var runtime = GetComponent<SlimePhysicsRuntime>();
        runtime.points = points;
        runtime.centerRB = centerRB;
        runtime.baseDrag = baseDrag;

        // 传递半径参数
        runtime.hardLimitRadius = hardCoreRadius;
        runtime.cushionRadius = softCoreRadius;

        // 传递角度参数
        float idealAngle = 360f / pointCount;
        runtime.idealDeltaAngle = idealAngle;
        runtime.minAngleLimit = idealAngle * hardAngleRatio;   // 硬限制角度
        runtime.cushionAngleLimit = idealAngle * softAngleRatio; // 软限制角度

        var sync = GetComponent<SlimeSkinSync>();
        sync.skin = skin;
        sync.points = points;

        Debug.Log("双重阻尼场史莱姆生成完毕！");
    }

    void ClearOld()
    {
        var list = new List<GameObject>();
        foreach (Transform t in transform)
            if (t.name.StartsWith("Point_") && t != skin.transform) list.Add(t.gameObject);
        foreach (var g in list) DestroyImmediate(g);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SlimeBuilder))]
public class SlimeBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("生成结构")) ((SlimeBuilder)target).GenerateSlime();
    }
}
#endif