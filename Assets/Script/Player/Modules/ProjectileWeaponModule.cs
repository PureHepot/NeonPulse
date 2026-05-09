using System.Collections.Generic;
using UnityEngine;

public abstract class ProjectileWeaponModule : WeaponModuleBase
{
    protected GameObject SpawnProjectile(ProjectileSpawnData spawnData, WeaponFireContext context)
    {
        if (spawnData == null || spawnData.prefab == null)
            return null;

        ApplyProjectileEffects(context, spawnData);

        GameObject projectileObject = ObjectPoolManager.Instance != null
            ? ObjectPoolManager.Instance.Get(spawnData.prefab, spawnData.position, spawnData.rotation)
            : Instantiate(spawnData.prefab, spawnData.position, spawnData.rotation);

        if (projectileObject == null)
            return null;

        ApplySpawnData(projectileObject, spawnData);
        NotifyProjectileSpawned(context, projectileObject);
        return projectileObject;
    }

    protected List<WeaponMuzzlePoint> BuildMuzzlePlan(IReadOnlyList<Transform> serializedMuzzles, int requestedCount, Transform fallbackOrigin)
    {
        requestedCount = Mathf.Max(1, requestedCount);

        var muzzlePlan = new List<WeaponMuzzlePoint>(requestedCount);
        if (serializedMuzzles != null)
        {
            for (int index = 0; index < serializedMuzzles.Count; index++)
            {
                var muzzle = serializedMuzzles[index];
                if (muzzle == null)
                    continue;

                muzzlePlan.Add(new WeaponMuzzlePoint
                {
                    position = muzzle.position,
                    rotation = muzzle.rotation,
                    visualTransform = muzzle,
                    isVirtual = false
                });
            }
        }

        if (muzzlePlan.Count == 0 && fallbackOrigin != null)
        {
            muzzlePlan.Add(new WeaponMuzzlePoint
            {
                position = fallbackOrigin.position,
                rotation = fallbackOrigin.rotation,
                visualTransform = fallbackOrigin,
                isVirtual = true
            });
        }

        if (muzzlePlan.Count > requestedCount)
            muzzlePlan.RemoveRange(requestedCount, muzzlePlan.Count - requestedCount);

        while (muzzlePlan.Count < requestedCount && fallbackOrigin != null)
        {
            float centeredIndex = muzzlePlan.Count - (requestedCount - 1) * 0.5f;
            Vector3 offset = fallbackOrigin.up * (0.18f * centeredIndex);
            muzzlePlan.Add(new WeaponMuzzlePoint
            {
                position = fallbackOrigin.position + offset,
                rotation = fallbackOrigin.rotation,
                visualTransform = null,
                isVirtual = true
            });
        }

        return muzzlePlan;
    }

    private static void ApplySpawnData(GameObject projectileObject, ProjectileSpawnData spawnData)
    {
        if (projectileObject == null || spawnData == null)
            return;

        var behaviours = projectileObject.GetComponents<MonoBehaviour>();
        for (int index = 0; index < behaviours.Length; index++)
        {
            if (behaviours[index] is IProjectileSpawnReceiver receiver)
            {
                receiver.ApplySpawnData(spawnData);
                return;
            }
        }
    }
}

public interface IProjectileSpawnReceiver
{
    void ApplySpawnData(ProjectileSpawnData spawnData);
}

public sealed class ProjectileSpawnData
{
    public GameObject prefab;
    public Vector3 position;
    public Quaternion rotation = Quaternion.identity;
    public int damage;
    public float speed;
    public float lifeTime;
    public LayerMask hitLayer;
    public LayerMask wallLayer;
    public bool homingEnabled;
    public float homingTurnRate;
    public float homingAcquireRadius;
    public float homingRetargetInterval;
}

public sealed class WeaponMuzzlePoint
{
    public Vector3 position;
    public Quaternion rotation = Quaternion.identity;
    public Transform visualTransform;
    public bool isVirtual;
}

public sealed class WeaponFireContext
{
    public WeaponFireContext(WeaponModuleBase weapon, int totalShots)
    {
        this.weapon = weapon;
        runtimeData = weapon != null ? weapon.RuntimeData : null;
        this.totalShots = totalShots;
    }

    public WeaponModuleBase weapon;
    public LoadoutModuleRuntimeData runtimeData;
    public int totalShots;
    public int shotIndex;
    public WeaponMuzzlePoint currentMuzzle;
}
