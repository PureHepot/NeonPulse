using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : Singleton<ObjectPoolManager>
{
    private Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();

    private Dictionary<int, Transform> poolParents = new Dictionary<int, Transform>();

    private Transform objectPool;


    public override void Initialize()
    {
        base.Initialize();
        objectPool = GameObject.Find("ObjectPool").transform;
    }
    /// <summary>
    /// 从池中获取对象
    /// </summary>
    /// <param name="prefab">原本的预制体</param>
    /// <param name="position">生成位置</param>
    /// <param name="rotation">生成旋转</param>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        int key = prefab.GetInstanceID();

        //创建新池子
        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary.Add(key, new Queue<GameObject>());

            //归类
            GameObject parentObj = new GameObject("Pool_" + prefab.name);
            parentObj.transform.SetParent(objectPool);
            poolParents.Add(key, parentObj.transform);
        }

        GameObject objToSpawn;

        //取出对象
        Queue<GameObject> poolQueue = poolDictionary[key];

        if (poolQueue.Count > 0)
        {
            objToSpawn = poolQueue.Dequeue();
        }
        else
        {
            //队列空了，实例化一个新的
            objToSpawn = GameObject.Instantiate(prefab);
            PoolObject poolObj = objToSpawn.AddComponent<PoolObject>();
            poolObj.poolKey = key;
        }

        objToSpawn.transform.SetParent(null);
        objToSpawn.transform.position = position;
        objToSpawn.transform.rotation = rotation;
        objToSpawn.SetActive(true);

        IPoolable poolable = objToSpawn.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnSpawn();
        }

        return objToSpawn;
    }

    /// <summary>
    /// 将对象回收进池子
    /// </summary>
    public void Return(GameObject obj)
    {
        PoolObject poolObj = obj.GetComponent<PoolObject>();
        if (poolObj == null)
        {
            Debug.LogError($"试图回收一个非对象池创建的物体: {obj.name}，直接销毁。");
            GameObject.Destroy(obj);
            return;
        }

        int key = poolObj.poolKey;

        // 调用 IPoolable 接口 (清理状态)
        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnDespawn();
        }

        obj.SetActive(false);

        // 放回父节点下，保持整洁
        if (poolParents.ContainsKey(key))
        {
            obj.transform.SetParent(poolParents[key]);
        }

        // 进队
        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary.Add(key, new Queue<GameObject>());
        }
        poolDictionary[key].Enqueue(obj);
    }
}
