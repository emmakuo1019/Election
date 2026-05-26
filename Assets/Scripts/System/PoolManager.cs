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
            Debug.LogWarning($"[PoolManager] 找不到 {instanceToRelease.name} 對應的 prefab 池，改為直接銷毀。");
            Destroy(instanceToRelease);
            return;
        }

        if (!pools.TryGetValue(prefab, out ObjectPool<GameObject> pool))
        {
            Debug.LogWarning($"[PoolManager] 找不到 prefab {prefab.name} 對應的池，改為直接銷毀實例。");
            instanceToPrefab.Remove(instanceToRelease);
            Destroy(instanceToRelease);
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
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxPoolSize);

        pools.Add(prefab, createdPool);
        return createdPool;
    }

    private GameObject CreateInstance(GameObject prefab)
    {
        GameObject createdInstance = Instantiate(prefab);
        createdInstance.name = prefab.name;
        instanceToPrefab[createdInstance] = prefab;
        createdInstance.SetActive(false);

        return createdInstance;
    }

    private void DestroyPooledInstance(GameObject pooledInstance)
    {
        if (pooledInstance != null)
        {
            instanceToPrefab.Remove(pooledInstance);
            Destroy(pooledInstance);
        }
    }

    private void PrepareInstanceForUse(GameObject instanceFromPool)
    {
        ParticleSystem[] particleSystems = instanceFromPool.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            particleSystem.Clear(true);
            particleSystem.Play(true);
        }
    }

    private void PrepareInstanceForRelease(GameObject instanceFromPool)
    {
        ParticleSystem[] particleSystems = instanceFromPool.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        instanceFromPool.SetActive(false);
    }
}
