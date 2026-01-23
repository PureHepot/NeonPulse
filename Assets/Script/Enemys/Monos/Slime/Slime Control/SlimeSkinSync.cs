using UnityEngine;
using UnityEngine.U2D;
using System.Collections.Generic;

public class SlimeSkinSync : MonoBehaviour
{
    public SpriteShapeController skin;
    public List<Transform> points;

    void Update()
    {
        if (skin == null || points == null) return;
        Spline spline = skin.spline;
        if (spline.GetPointCount() != points.Count) return;

        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null) continue;

            Vector3 localPos = skin.transform.InverseTransformPoint(points[i].position);
            spline.SetPosition(i, localPos);

            // 简单的切线计算
            int next = (i + 1) % points.Count;
            int prev = (i - 1 + points.Count) % points.Count;
            Vector3 pNext = skin.transform.InverseTransformPoint(points[next].position);
            Vector3 pPrev = skin.transform.InverseTransformPoint(points[prev].position);

            Vector3 dir = (pNext - pPrev).normalized;
            float dist = Vector3.Distance(pNext, pPrev);

            Vector3 tangent = dir * dist * 0.3f;
            spline.SetLeftTangent(i, -tangent);
            spline.SetRightTangent(i, tangent);
        }
    }
}