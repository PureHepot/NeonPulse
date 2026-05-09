using UnityEngine;

public class EnemySpawnPointProvider
{
    private const int MaxAttempts = 16;

    public bool TryGetSpawnPoint(Transform playerTransform, float innerPadding, float outerPadding, out Vector3 spawnPoint)
    {
        spawnPoint = Vector3.zero;

        var camera = Camera.main;
        if (camera == null || !camera.orthographic)
            return false;

        Vector3 center = playerTransform != null ? playerTransform.position : camera.transform.position;
        float halfHeight = camera.orthographicSize;
        float halfWidth = halfHeight * camera.aspect;

        float minX = center.x - halfWidth - outerPadding;
        float maxX = center.x + halfWidth + outerPadding;
        float minY = center.y - halfHeight - outerPadding;
        float maxY = center.y + halfHeight + outerPadding;

        float innerMinX = center.x - halfWidth - innerPadding;
        float innerMaxX = center.x + halfWidth + innerPadding;
        float innerMinY = center.y - halfHeight - innerPadding;
        float innerMaxY = center.y + halfHeight + innerPadding;

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            spawnPoint = SampleRingPoint(minX, maxX, minY, maxY, innerMinX, innerMaxX, innerMinY, innerMaxY);
            if (IsSpawnPointValid(spawnPoint, center, halfWidth, halfHeight))
                return true;
        }

        return false;
    }

    private static Vector3 SampleRingPoint(
        float minX,
        float maxX,
        float minY,
        float maxY,
        float innerMinX,
        float innerMaxX,
        float innerMinY,
        float innerMaxY)
    {
        switch (Random.Range(0, 4))
        {
            case 0:
                return new Vector3(Random.Range(minX, maxX), Random.Range(innerMaxY, maxY), 0f);
            case 1:
                return new Vector3(Random.Range(minX, maxX), Random.Range(minY, innerMinY), 0f);
            case 2:
                return new Vector3(Random.Range(minX, innerMinX), Random.Range(minY, maxY), 0f);
            default:
                return new Vector3(Random.Range(innerMaxX, maxX), Random.Range(minY, maxY), 0f);
        }
    }

    private static bool IsSpawnPointValid(Vector3 spawnPoint, Vector3 center, float halfWidth, float halfHeight)
    {
        if (spawnPoint.x > center.x - halfWidth &&
            spawnPoint.x < center.x + halfWidth &&
            spawnPoint.y > center.y - halfHeight &&
            spawnPoint.y < center.y + halfHeight)
        {
            return false;
        }

        return Physics2D.OverlapCircle(spawnPoint, 0.35f) == null;
    }
}
