using UnityEngine;
using UnityEngine.U2D;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

[ExecuteInEditMode]
public class SlimeBuilder : MonoBehaviour
{
    [Header("Settings")]
    public SpriteShapeController skin; // 拖入 Sprite Shape 物体
    [Range(6, 60)] public int pointCount = 12; // 边缘点数量
    public float radius = 2f; // 半径

    [Header("Physics Settings")]
    public float pointMass = 0.5f;
    public float linearDrag = 1f;
    public float angularDrag = 1f;

    [Header("Spring Settings (Stiffness)")]
    public float centerFrequency = 5f; // 连向中心的硬度
    public float centerDamping = 0.5f;
    public float edgeFrequency = 8f;   // 边缘连接的硬度
    public float edgeDamping = 0.3f;

    [Header("Generated References (Read Only)")]
    public List<Transform> generatedPoints = new List<Transform>();

    public void GenerateSlime()
    {
        if (skin == null)
        {
            Debug.LogError("请先赋值 Skin (SpriteShapeController)！");
            return;
        }

        //清理旧物体
        ClearOldPoints();

        //准备 Sprite Shape 样条线
        Spline spline = skin.spline;
        spline.Clear();

        //获取或添加中心刚体
        Rigidbody2D centerRB = GetComponent<Rigidbody2D>();
        if (centerRB == null)
        {
            centerRB = gameObject.AddComponent<Rigidbody2D>();
            centerRB.gravityScale = 1; 
            centerRB.mass = pointCount * pointMass; // 中心质量大一点
            centerRB.drag = linearDrag;
            centerRB.angularDrag = angularDrag;
        }

        //生成边缘点
        float angleStep = 360f / pointCount;
        List<Rigidbody2D> pointRBs = new List<Rigidbody2D>();

        for (int i = 0; i < pointCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            Vector3 worldPos = transform.position + pos;

            // 创建物体
            GameObject pObj = new GameObject($"Point ({i})");
            pObj.transform.SetParent(transform);
            pObj.transform.position = worldPos;

            // 添加物理组件
            Rigidbody2D rb = pObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.mass = pointMass;
            rb.drag = linearDrag;
            rb.angularDrag = angularDrag;
            // 锁定旋转，防止关节乱扭
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // 添加碰撞体
            CircleCollider2D col = pObj.AddComponent<CircleCollider2D>();
            col.radius = radius * Mathf.PI / pointCount * 0.5f; // 动态计算合适的大小

            pointRBs.Add(rb);
            generatedPoints.Add(pObj.transform);

            // --- 关节 1: 连向中心 (支撑力) ---
            AddSpring(pObj, centerRB, centerFrequency, centerDamping);

            // --- 更新 Sprite Shape ---
            // 这里的切线(Tangent)设置为自动平滑模式的大概值
            float tangentLen = (2f * Mathf.PI * radius / pointCount) * 0.5f;
            Vector3 tangent = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0) * tangentLen;
            // 注意：Spline 是局部坐标，但我们后面会用 SkinSync 脚本实时更新，这里只是初始化形状
            spline.InsertPointAt(i, pos);
            spline.SetTangentMode(i, ShapeTangentMode.Continuous);
            spline.SetLeftTangent(i, -tangent);
            spline.SetRightTangent(i, tangent);
        }

        // 5. 再次循环，添加边缘关节 (表面张力)
        for (int i = 0; i < pointCount; i++)
        {
            GameObject currentObj = generatedPoints[i].gameObject;

            // 获取前一个和后一个的索引 (处理循环)
            int prevIndex = (i - 1 + pointCount) % pointCount;
            int nextIndex = (i + 1) % pointCount;

            // --- 关节 2 & 3: 连向左右邻居 ---
            // 注意：为了物理稳定性，双向连接（我连你，你连我）通常会导致过度僵硬或抖动。
            // 最佳实践是形成一个单向闭环链条，或者只连 Next。
            // 但既然你需要 "3个关节"，我们可以做一个交叉结构增强稳定性。

            // 连向 Next (构成圆环)
            AddSpring(currentObj, pointRBs[nextIndex], edgeFrequency, edgeDamping);

            // 连向 Previous (增强结构，防止断裂)
            AddSpring(currentObj, pointRBs[prevIndex], edgeFrequency, edgeDamping);
        }

        // 6. 自动挂载运行时同步脚本
        SlimeSkinSync sync = GetComponent<SlimeSkinSync>();
        if (sync == null) sync = gameObject.AddComponent<SlimeSkinSync>();
        sync.skin = skin;
        sync.points = new List<Transform>(generatedPoints);

        Debug.Log($"生成完毕！创建了 {pointCount} 个边缘点。");
    }

    void AddSpring(GameObject obj, Rigidbody2D target, float freq, float damp)
    {
        SpringJoint2D sp = obj.AddComponent<SpringJoint2D>();
        sp.connectedBody = target;
        sp.autoConfigureDistance = true; // 自动记录当前距离为静止距离
        sp.frequency = freq;
        sp.dampingRatio = damp;
    }

    void ClearOldPoints()
    {
        // 删除旧的 Point 物体
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("Point") && child != skin.transform)
            {
                DestroyImmediate(child.gameObject);
            }
        }
        generatedPoints.Clear();
    }
}

// 这是一个自定义 Editor，用于在 Inspector 显示按钮
#if UNITY_EDITOR
[CustomEditor(typeof(SlimeBuilder))]
public class SlimeBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        SlimeBuilder script = (SlimeBuilder)target;
        if (GUILayout.Button("Generate Slime Body", GUILayout.Height(30)))
        {
            script.GenerateSlime();
        }
    }
}
#endif
