using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class SoftBody : MonoBehaviour
{
    private const float splineOffset = 0.5f;

    [SerializeField]
    public SpriteShapeController spriteShape;
    [SerializeField]
    public List<Transform> points;

    private void Awake()
    {
        //UpdateVerticies();
    }

    private void Update()
    {
        UpdateVerticies();
    }

    private void UpdateVerticies()
    {
        for(int i = 0; i < points.Count - 1; i++)
        {
            Vector2 vertex = points[i].localPosition;
            Vector2 towardsCenter = (Vector2.zero - vertex).normalized;

            float colliderRadius = points[i].GetComponent<CircleCollider2D>().radius;

            try
            {
                spriteShape.spline.SetPosition(i, vertex - towardsCenter * colliderRadius);
            }
            catch
            {
                Debug.Log("Too close to each other");
                spriteShape.spline.SetPosition(i, vertex - towardsCenter * (colliderRadius + splineOffset));
            }

            Vector2 lt = spriteShape.spline.GetLeftTangent(i);

            Vector2 newRt = Vector2.Perpendicular(towardsCenter) * lt.magnitude;
            Vector2 newLt = Vector2.zero - newRt;

            spriteShape.spline.SetLeftTangent(i, newLt);
            spriteShape.spline.SetRightTangent(i, newRt);
        }
    }
}
