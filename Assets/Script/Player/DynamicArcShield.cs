using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(PolygonCollider2D))]
public class DynamicArcShield : MonoBehaviour
{
    [Header("Settings")]
    public float radius = 0.5f;           // 圆环半径
    public float thickness = 0.05f;      // 圆环厚度
    [Range(0, 180)]
    public float maxArcAngle = 180f;    // 最大角度 (半圆是180)
    public int segments = 30;           // 平滑度 (段数越多越圆)

    [Header("State")]
    [Range(0, 1)]
    public float deployFactor = 0f;     // 0 = 收起, 1 = 完全展开 (由外部控制)

    private LineRenderer lineRenderer;
    private PolygonCollider2D polyCollider;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        polyCollider = GetComponent<PolygonCollider2D>();

        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = thickness;
        lineRenderer.endWidth = thickness;
    }

    void Update()
    {
        UpdateShieldGeometry();
    }

    void UpdateShieldGeometry()
    {
        float currentAngle = maxArcAngle * deployFactor;

        if (currentAngle < 1f)
        {
            lineRenderer.positionCount = 0;
            polyCollider.pathCount = 0;
            return;
        }

        float startAngle = -currentAngle / 2f;
        float endAngle = currentAngle / 2f;
        float angleStep = currentAngle / segments;

        Vector3[] linePoints = new Vector3[segments + 1];

        Vector2[] colliderPoints = new Vector2[(segments + 1) * 2];

        for (int i = 0; i <= segments; i++)
        {
            float angleDeg = startAngle + (angleStep * i);
            float angleRad = angleDeg * Mathf.Deg2Rad;

            Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            linePoints[i] = dir * radius;

            float halfThick = thickness / 2f;

            colliderPoints[i] = dir * (radius + halfThick);

            int endBackIndex = colliderPoints.Length - 1 - i;
            colliderPoints[endBackIndex] = dir * (radius - halfThick);
        }

        lineRenderer.positionCount = linePoints.Length;
        lineRenderer.SetPositions(linePoints);

        polyCollider.pathCount = 1;
        polyCollider.SetPath(0, colliderPoints);
    }
}
