using UnityEngine;

public class BossArenaLimiter
{
    private Vector2 center;
    private Vector2 halfExtents;
    private bool isActive;

    public bool IsActive => isActive;
    public Vector2 Center => center;
    public Vector2 HalfExtents => halfExtents;
    public Vector2 MinBounds => center - halfExtents;
    public Vector2 MaxBounds => center + halfExtents;

    public void Activate(BossArenaConfig config)
    {
        var camera = Camera.main;
        Vector3 cameraCenter = camera != null ? camera.transform.position : Vector3.zero;

        center = (Vector2)cameraCenter + (config != null ? config.centerOffset : Vector2.zero);
        halfExtents = config != null ? config.halfExtents : new Vector2(8f, 4.5f);
        halfExtents.x = Mathf.Max(0.5f, halfExtents.x);
        halfExtents.y = Mathf.Max(0.5f, halfExtents.y);
        isActive = true;
    }

    public void ConstrainPlayer(PlayerController player)
    {
        if (!isActive || player == null || player.IsDead)
            return;

        player.ClampToBounds(MinBounds, MaxBounds);
    }

    public void Deactivate()
    {
        isActive = false;
    }
}
