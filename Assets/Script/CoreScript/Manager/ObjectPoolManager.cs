using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoSingleton<ObjectPoolManager>
{
    private Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();
    private Dictionary<int, Transform> poolParents = new Dictionary<int, Transform>();

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        int key = prefab.GetInstanceID();

        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary.Add(key, new Queue<GameObject>());

            GameObject parentObj = new GameObject("Pool_" + prefab.name);
            parentObj.transform.SetParent(this.transform);
            poolParents.Add(key, parentObj.transform);
        }

        GameObject objToSpawn = null;
        Queue<GameObject> poolQueue = poolDictionary[key];

        while (poolQueue.Count > 0)
        {
            GameObject candidate = poolQueue.Dequeue();
            if (candidate != null)
            {
                objToSpawn = candidate;
                break;
            }
        }

        if (objToSpawn == null)
        {
            objToSpawn = Instantiate(prefab);
            PoolObject poolObj = objToSpawn.AddComponent<PoolObject>();
            poolObj.poolKey = key;
        }

        PoolObject pObj = objToSpawn.GetComponent<PoolObject>();
        if (pObj != null)
        {
            pObj.isInPool = false;
            pObj.spawnVersion++;
        }

        objToSpawn.transform.SetParent(null);
        objToSpawn.transform.position = position;
        objToSpawn.transform.rotation = rotation;
        ResetPooledParticles(objToSpawn);
        objToSpawn.SetActive(true);

        IPoolable poolable = objToSpawn.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnSpawn();
        }

        return objToSpawn;
    }

    public void Return(GameObject obj)
    {
        PoolObject poolObj = obj.GetComponent<PoolObject>();
        if (poolObj == null)
        {
            Debug.LogError($"Tried to return a non-pooled object: {obj.name}. Destroying it instead.");
            Destroy(obj);
            return;
        }

        if (poolObj.isInPool)
            return;

        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnDespawn();
        }

        int key = poolObj.poolKey;
        poolObj.isInPool = true;

        obj.SetActive(false);
        ResetPooledParticles(obj);

        if (poolParents.ContainsKey(key))
        {
            obj.transform.SetParent(poolParents[key]);
        }

        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary.Add(key, new Queue<GameObject>());
        }
        poolDictionary[key].Enqueue(obj);
    }

    private static void ResetPooledParticles(GameObject obj)
    {
        if (obj == null)
            return;

        var particleSystems = obj.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var particleSystem in particleSystems)
        {
            if (particleSystem == null)
                continue;

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(true);
        }
    }
}
