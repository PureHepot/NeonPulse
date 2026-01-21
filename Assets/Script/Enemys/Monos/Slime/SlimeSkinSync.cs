using UnityEngine;
using UnityEngine.U2D;
using System.Collections.Generic;

public class SlimeSkinSync : MonoBehaviour
{
    public SpriteShapeController skin;
    public List<Transform> points;

    void Update()
    {
        if (skin == null || points == null || points.Count == 0) return;

        Spline spline = skin.spline;

        // 如果点数不匹配，不执行（防止报错）
        if (spline.GetPointCount() != points.Count) return;

        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null) continue;

            // 将物理点的世界坐标转换为 SpriteShape 的局部坐标
            Vector3 localPos = skin.transform.InverseTransformPoint(points[i].position);

            // 更新样条点位置
            spline.SetPosition(i, localPos);

            // 保持切线平滑 (Q弹的关键)
            // 我们通过计算邻居向量来动态调整切线方向，让曲线更圆润
            Vector3 prevPos = skin.transform.InverseTransformPoint(points[(i - 1 + points.Count) % points.Count].position);
            Vector3 nextPos = skin.transform.InverseTransformPoint(points[(i + 1) % points.Count].position);

            // 计算切线向量：指向 (Next - Prev) 的方向
            Vector3 tangentDir = (nextPos - prevPos).normalized;
            float tangentLen = Vector3.Distance(prevPos, nextPos) * 0.3f; // 0.3 是平滑系数

            spline.SetLeftTangent(i, -tangentDir * tangentLen);
            spline.SetRightTangent(i, tangentDir * tangentLen);
        }
    }
}
