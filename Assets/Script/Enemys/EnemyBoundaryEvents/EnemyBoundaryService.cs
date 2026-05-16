using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyBoundaryService
{
    private readonly List<TrackedEnemy> trackedEnemies = new();
    private readonly CameraBoundsSnapshot boundsSnapshot = new();

    private int checkPerFrame = 10;
    private float boundaryThreshold = 0.6f;
    private int currentCheckIndex;

    public void Configure(int checksPerFrame, float threshold)
    {
        checkPerFrame = Mathf.Max(1, checksPerFrame);
        boundaryThreshold = Mathf.Max(0.01f, threshold);
    }

    public void Reset()
    {
        trackedEnemies.Clear();
        currentCheckIndex = 0;
    }

    public void RegisterEnemy(EnemyBase enemy)
    {
        if (enemy == null)
            return;

        if (FindTrackedIndex(enemy) >= 0)
            return;

        EnemyBoundaryConstraint[] constraints = enemy.GetComponents<EnemyBoundaryConstraint>();
        EnemyBoundaryReaction[] reactions = enemy.GetComponents<EnemyBoundaryReaction>();
        bool hasConstraints = constraints != null && constraints.Length > 0;
        bool hasReactions = reactions != null && reactions.Length > 0;
        if (!hasConstraints && !hasReactions)
            return;

        trackedEnemies.Add(new TrackedEnemy(enemy, constraints, reactions));
    }

    public void UnregisterEnemy(EnemyBase enemy)
    {
        if (enemy == null)
            return;

        int trackedIndex = FindTrackedIndex(enemy);
        if (trackedIndex < 0)
            return;

        trackedEnemies.RemoveAt(trackedIndex);
        if (currentCheckIndex >= trackedEnemies.Count)
            currentCheckIndex = 0;
    }

    public void Tick(float deltaTime, Camera targetCamera)
    {
        if (targetCamera == null)
            return;

        boundsSnapshot.Rebuild(targetCamera, boundaryThreshold);
        CleanupInvalidEntries();
        if (trackedEnemies.Count == 0)
        {
            currentCheckIndex = 0;
            return;
        }

        for (int index = 0; index < trackedEnemies.Count; index++)
            trackedEnemies[index].TickContinuous(deltaTime, boundsSnapshot);

        int checksThisFrame = Mathf.Min(checkPerFrame, trackedEnemies.Count);
        for (int i = 0; i < checksThisFrame; i++)
        {
            if (trackedEnemies.Count == 0)
            {
                currentCheckIndex = 0;
                return;
            }

            if (currentCheckIndex >= trackedEnemies.Count)
                currentCheckIndex = 0;

            trackedEnemies[currentCheckIndex].TickBoundaryContact(boundsSnapshot);
            currentCheckIndex = (currentCheckIndex + 1) % trackedEnemies.Count;
        }
    }

    private void CleanupInvalidEntries()
    {
        for (int index = trackedEnemies.Count - 1; index >= 0; index--)
        {
            if (!trackedEnemies[index].IsValid)
                trackedEnemies.RemoveAt(index);
        }

        if (currentCheckIndex >= trackedEnemies.Count)
            currentCheckIndex = 0;
    }

    private int FindTrackedIndex(EnemyBase enemy)
    {
        for (int index = 0; index < trackedEnemies.Count; index++)
        {
            if (trackedEnemies[index].Enemy == enemy)
                return index;
        }

        return -1;
    }

    public readonly struct EnemyBoundaryContext
    {
        public EnemyBoundaryContext(EnemyBase enemy, Rigidbody2D rigidbody, CameraBoundsSnapshot bounds, bool hasArmedBoundaryEvents)
        {
            Enemy = enemy;
            Rigidbody = rigidbody;
            MinX = bounds.MinX;
            MaxX = bounds.MaxX;
            MinY = bounds.MinY;
            MaxY = bounds.MaxY;
            Threshold = bounds.Threshold;
            HasArmedBoundaryEvents = hasArmedBoundaryEvents;
        }

        public EnemyBase Enemy { get; }
        public Rigidbody2D Rigidbody { get; }
        public float MinX { get; }
        public float MaxX { get; }
        public float MinY { get; }
        public float MaxY { get; }
        public float Threshold { get; }
        public bool HasArmedBoundaryEvents { get; }
        public Vector2 Position => Enemy != null ? (Vector2)Enemy.transform.position : Vector2.zero;
    }

    private sealed class TrackedEnemy
    {
        private readonly EnemyBoundaryConstraint[] constraints;
        private readonly EnemyBoundaryReaction[] reactions;
        private readonly Rigidbody2D rigidbody;
        private bool hasArmedBoundaryEvents;

        public TrackedEnemy(EnemyBase enemy, EnemyBoundaryConstraint[] constraints, EnemyBoundaryReaction[] reactions)
        {
            Enemy = enemy;
            this.constraints = constraints;
            this.reactions = reactions;
            rigidbody = enemy != null ? enemy.GetComponent<Rigidbody2D>() : null;
        }

        public EnemyBase Enemy { get; }
        public bool IsValid => Enemy != null && Enemy.gameObject != null;

        public void TickContinuous(float deltaTime, CameraBoundsSnapshot bounds)
        {
            if (!IsValid)
                return;

            Vector2 position = Enemy.transform.position;
            if (!hasArmedBoundaryEvents && bounds.IsInsideArmingZone(position))
                hasArmedBoundaryEvents = true;

            var context = new EnemyBoundaryContext(Enemy, rigidbody, bounds, hasArmedBoundaryEvents);
            for (int index = 0; index < constraints.Length; index++)
            {
                EnemyBoundaryConstraint constraint = constraints[index];
                if (constraint == null)
                    continue;

                constraint.TickConstraint(context);
            }

            for (int index = 0; index < reactions.Length; index++)
            {
                EnemyBoundaryReaction reaction = reactions[index];
                if (reaction == null)
                    continue;

                reaction.SetFirstEntryPending(!hasArmedBoundaryEvents);
                reaction.AdvanceCooldown(deltaTime);
            }
        }

        public void TickBoundaryContact(CameraBoundsSnapshot bounds)
        {
            if (!IsValid || !hasArmedBoundaryEvents)
                return;

            if (!bounds.IsNearBoundary(Enemy.transform.position))
                return;

            var context = new EnemyBoundaryContext(Enemy, rigidbody, bounds, hasArmedBoundaryEvents);
            for (int index = 0; index < reactions.Length; index++)
            {
                EnemyBoundaryReaction reaction = reactions[index];
                if (reaction == null)
                    continue;

                if (reaction.TryReact(context))
                    break;
            }
        }
    }

    public sealed class CameraBoundsSnapshot
    {
        public float MinX { get; private set; }
        public float MaxX { get; private set; }
        public float MinY { get; private set; }
        public float MaxY { get; private set; }
        public float Threshold { get; private set; }

        public void Rebuild(Camera targetCamera, float threshold)
        {
            float camHeight = targetCamera.orthographicSize * 2f;
            float camWidth = camHeight * targetCamera.aspect;
            Vector3 cameraPosition = targetCamera.transform.position;

            MinX = cameraPosition.x - (camWidth * 0.5f);
            MaxX = cameraPosition.x + (camWidth * 0.5f);
            MinY = cameraPosition.y - (camHeight * 0.5f);
            MaxY = cameraPosition.y + (camHeight * 0.5f);
            Threshold = threshold;
        }

        public bool IsInsideArmingZone(Vector2 position)
        {
            return position.x > MinX + Threshold &&
                   position.x < MaxX - Threshold &&
                   position.y > MinY + Threshold &&
                   position.y < MaxY - Threshold;
        }

        public bool IsNearBoundary(Vector2 position)
        {
            return position.x <= MinX + Threshold ||
                   position.x >= MaxX - Threshold ||
                   position.y <= MinY + Threshold ||
                   position.y >= MaxY - Threshold;
        }
    }
}
