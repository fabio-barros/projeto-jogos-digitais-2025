using System.Collections.Generic;
using UnityEngine;

public class ObjectPool2D : MonoBehaviour
{
    private static ObjectPool2D instance;

    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly Dictionary<GameObject, int> activeCounts = new Dictionary<GameObject, int>();

    public static ObjectPool2D Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<ObjectPool2D>();
                if (instance == null)
                    instance = new GameObject("ObjectPool2D").AddComponent<ObjectPool2D>();
            }

            return instance;
        }
    }

    public int TotalInactiveCount
    {
        get
        {
            int count = 0;
            foreach (Queue<GameObject> pool in pools.Values)
                count += pool.Count;

            return count;
        }
    }

    public int TotalActiveCount
    {
        get
        {
            int count = 0;
            foreach (int active in activeCounts.Values)
                count += active;

            return count;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        return Instance.SpawnInternal(prefab, position, rotation);
    }

    public static void Despawn(GameObject instanceObject)
    {
        if (instanceObject == null)
            return;

        PooledObject2D pooledObject = instanceObject.GetComponent<PooledObject2D>();
        if (pooledObject == null || pooledObject.sourcePrefab == null || instance == null)
        {
            Destroy(instanceObject);
            return;
        }

        instance.DespawnInternal(instanceObject, pooledObject.sourcePrefab);
    }

    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0)
            return;

        Queue<GameObject> pool = GetPool(prefab);
        for (int i = 0; i < count; i++)
        {
            GameObject item = CreateInstance(prefab);
            item.SetActive(false);
            pool.Enqueue(item);
        }
    }

    private GameObject SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        Queue<GameObject> pool = GetPool(prefab);
        GameObject item = pool.Count > 0 ? pool.Dequeue() : CreateInstance(prefab);

        item.transform.SetPositionAndRotation(position, rotation);
        item.transform.localScale = prefab.transform.localScale;
        item.SetActive(true);

        if (!activeCounts.ContainsKey(prefab))
            activeCounts[prefab] = 0;
        activeCounts[prefab]++;

        return item;
    }

    private void DespawnInternal(GameObject item, GameObject sourcePrefab)
    {
        if (activeCounts.ContainsKey(sourcePrefab))
            activeCounts[sourcePrefab] = Mathf.Max(0, activeCounts[sourcePrefab] - 1);

        item.SetActive(false);
        GetPool(sourcePrefab).Enqueue(item);
    }

    private Queue<GameObject> GetPool(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            pools[prefab] = pool;
        }

        return pool;
    }

    private GameObject CreateInstance(GameObject prefab)
    {
        GameObject item = Instantiate(prefab, transform);
        PooledObject2D pooledObject = item.GetComponent<PooledObject2D>();
        if (pooledObject == null)
            pooledObject = item.AddComponent<PooledObject2D>();

        pooledObject.sourcePrefab = prefab;
        return item;
    }
}
