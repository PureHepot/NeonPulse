using System.Collections.Generic;
using UnityEngine;

public class BossArenaLimiter
{
    private readonly List<GameObject> walls = new();
    private Vector2 center;
    private Vector2 halfExtents;

    public bool IsActive => walls.Count > 0;
    public Vector2 Center => center;
    public Vector2 HalfExtents => halfExtents;

    public void Activate(BossArenaConfig config, Transform parent = null)
    {
        Deactivate();

        var camera = Camera.main;
        Vector3 cameraCenter = camera != null ? camera.transform.position : Vector3.zero;

        center = (Vector2)cameraCenter + (config != null ? config.centerOffset : Vector2.zero);
        halfExtents = config != null ? config.halfExtents : new Vector2(8f, 4.5f);
        float thickness = config != null ? Mathf.Max(0.2f, config.wallThickness) : 1f;

        CreateWall("BossArena_Left", new Vector2(center.x - halfExtents.x - thickness * 0.5f, center.y), new Vector2(thickness, halfExtents.y * 2f), parent);
        CreateWall("BossArena_Right", new Vector2(center.x + halfExtents.x + thickness * 0.5f, center.y), new Vector2(thickness, halfExtents.y * 2f), parent);
        CreateWall("BossArena_Top", new Vector2(center.x, center.y + halfExtents.y + thickness * 0.5f), new Vector2(halfExtents.x * 2f, thickness), parent);
        CreateWall("BossArena_Bottom", new Vector2(center.x, center.y - halfExtents.y - thickness * 0.5f), new Vector2(halfExtents.x * 2f, thickness), parent);
    }

    public void Deactivate()
    {
        for (int i = 0; i < walls.Count; i++)
        {
            if (walls[i] != null)
                Object.Destroy(walls[i]);
        }

        walls.Clear();
    }

    private void CreateWall(string wallName, Vector2 position, Vector2 size, Transform parent)
    {
        var wall = new GameObject(wallName);
        if (parent != null)
            wall.transform.SetParent(parent, false);

        wall.transform.position = new Vector3(position.x, position.y, 0f);
        var rigidbody = wall.AddComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Static;

        var collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;

        walls.Add(wall);
    }
}
