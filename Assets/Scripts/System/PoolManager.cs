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
        // 改為場景級別單例，不跨場景保留
        // DontDestroyOnLoad(gameObject); // 已移除
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            
            // 清理所有對象池
            foreach (var pool in pools.Values)
            {
                pool.Clear();
            }
            pools.Clear();
            instanceToPrefab.Clear();
            instanceToPoolables.Clear();
        }
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
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
            return;
        }

        if (!instanceToPrefab.TryGetValue(instanceToRelease, out GameObject prefab))
        {
            PrepareInstanceForRelease(instanceToRelease);
            return;
        }

        if (!pools.TryGetValue(prefab, out ObjectPool<GameObject> pool))
        {
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
        // 不設父物件，讓對象池物件與場景綁定，場景卸載時自動清理
        GameObject createdInstance = Instantiate(prefab);
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
