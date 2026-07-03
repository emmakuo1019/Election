using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : MonoBehaviour
{
    [SerializeField] private int defaultCapacity = 8;
    [SerializeField] private int maxPoolSize = 32;

    private static PoolManager instance;

    private readonly Dictionary<GameObject, ObjectPool<GameObject>> pools = new Dictionary<GameObject, ObjectPool<GameObject>>();
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();
    private readonly Dictionary<GameObject, IPoolable> instanceToPoolables = new Dictionary<GameObject, IPoolable>();

    public static bool HasInstance => instance != null;

    public static PoolManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PoolManager>();

                if (instance == null)
                {
                    GameObject poolManagerObject = new GameObject(nameof(PoolManager));
                    instance = poolManagerObject.AddComponent<PoolManager>();
                }
            }

            return instance;
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
        DontDestroyOnLoad(gameObject);
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[PoolManager] Get 失敗：prefab 為空。");
            return null;
        }

        ObjectPool<GameObject> pool = GetOrCreatePool(prefab);
        GameObject instanceFromPool = pool.Get();

        Transform instanceTransform = instanceFromPool.transform;
        instanceTransform.SetPositionAndRotation(position, rotation);
        instanceFromPool.SetActive(true);
        PrepareInstanceForUse(instanceFromPool);

        return instanceFromPool;
    }

    public void Release(GameObject instanceToRelease)
    {
        if (instanceToRelease == null)
        {
            Debug.LogWarning("[PoolManager] Release 失敗：instance 為空。");
            return;
        }

        if (!instanceToPrefab.TryGetValue(instanceToRelease, out GameObject prefab))
        {
            Debug.LogWarning($"[PoolManager] 找不到 {instanceToRelease.name} 對應的 prefab 池，改為直接回收為 inactive。");
            PrepareInstanceForRelease(instanceToRelease);
            return;
        }

        if (!pools.TryGetValue(prefab, out ObjectPool<GameObject> pool))
        {
            Debug.LogWarning($"[PoolManager] 找不到 prefab {prefab.name} 對應的池，改為直接回收為 inactive。");
            instanceToPrefab.Remove(instanceToRelease);
            PrepareInstanceForRelease(instanceToRelease);
            return;
        }

        pool.Release(instanceToRelease);
    }

    private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out ObjectPool<GameObject> existingPool))
        {
            return existingPool;
        }

        ObjectPool<GameObject> createdPool = new ObjectPool<GameObject>(
            () => CreateInstance(prefab),
            actionOnGet: null,
            actionOnRelease: PrepareInstanceForRelease,
            actionOnDestroy: DestroyPooledInstance,
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize: maxPoolSize);

        pools.Add(prefab, createdPool);
        return createdPool;
    }

    private GameObject CreateInstance(GameObject prefab)
    {
        // 將生成的物件設為 PoolManager 的子物件，確保它們跟著 PoolManager 一起 DontDestroyOnLoad
        GameObject createdInstance = Instantiate(prefab, transform);
        createdInstance.name = prefab.name;
        instanceToPrefab[createdInstance] = prefab;

        if (createdInstance.TryGetComponent<IPoolable>(out var poolable))
        {
            instanceToPoolables[createdInstance] = poolable;
        }

        createdInstance.SetActive(false);

        return createdInstance;
    }

    private void DestroyPooledInstance(GameObject pooledInstance)
    {
        if (pooledInstance != null)
        {
            instanceToPrefab.Remove(pooledInstance);
            instanceToPoolables.Remove(pooledInstance);
            PrepareInstanceForRelease(pooledInstance);
        }
    }

    private void PrepareInstanceForUse(GameObject instanceFromPool)
    {
        if (instanceToPoolables.TryGetValue(instanceFromPool, out IPoolable poolable))
        {
            poolable.OnSpawn();
        }
        else if (instanceFromPool.TryGetComponent<IPoolable>(out poolable))
        {
            instanceToPoolables[instanceFromPool] = poolable;
            poolable.OnSpawn();
        }
    }

    private void PrepareInstanceForRelease(GameObject instanceFromPool)
    {
        if (instanceToPoolables.TryGetValue(instanceFromPool, out IPoolable poolable))
        {
            poolable.OnDespawn();
        }
        else if (instanceFromPool.TryGetComponent<IPoolable>(out poolable))
        {
            instanceToPoolables[instanceFromPool] = poolable;
            poolable.OnDespawn();
        }

        instanceFromPool.SetActive(false);
    }

    public void ReleaseAllActiveObjects()
    {
        // 為了避免在 foreach 迴圈中因為 Release 改變了 active 狀態或集合，先收集一份名單
        List<GameObject> activeObjects = new List<GameObject>();
        foreach (var obj in instanceToPrefab.Keys)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                activeObjects.Add(obj);
            }
        }
        
        foreach (var obj in activeObjects)
        {
            Release(obj);
        }
    }
}
