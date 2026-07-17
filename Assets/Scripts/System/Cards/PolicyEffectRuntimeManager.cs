using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 政策卡持續性效果的執行器，負責管理定時生成等持續性行為的生命週期。
/// 掛載於戰鬥場景中，在場景銷毀時會清理所有生成物，避免內存洩漏。
/// </summary>
public class PolicyEffectRuntimeManager : MonoBehaviour
{
    public static PolicyEffectRuntimeManager Instance { get; private set; }

    // 用於追蹤所有生成出的物件實體
    private List<GameObject> activeSpawnedObjects = new List<GameObject>();
    
    // 用於記錄運行中的協程，以便在需要時（如 RemoveEffect）停止它們
    private Dictionary<SpawnObjectEffect, Coroutine> runningCoroutines = new Dictionary<SpawnObjectEffect, Coroutine>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        Cleanup();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 清理所有由政策卡產生的協程與生成物，避免內存洩漏
    /// </summary>
    public void Cleanup()
    {
        // 1. 停止所有運行中的協程
        StopAllCoroutines();
        runningCoroutines.Clear();

        // 2. 強制銷毀所有生成出的物件實體，防止內存與物件洩漏
        foreach (var obj in activeSpawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        activeSpawnedObjects.Clear();
        Debug.Log("[PolicyEffectRuntimeManager] 執行 Cleanup 清理所有生成物");
    }

    /// <summary>
    /// 註冊持續生成效果
    /// </summary>
    public void RegisterSpawnEffect(SpawnObjectEffect effect)
    {
        if (effect == null) return;

        // 如果該效果已經註冊過，先停止舊的協程
        if (runningCoroutines.TryGetValue(effect, out Coroutine oldCoroutine))
        {
            if (oldCoroutine != null)
            {
                StopCoroutine(oldCoroutine);
            }
            runningCoroutines.Remove(effect);
        }

        // 啟動新的定時生成協程
        Coroutine newCoroutine = StartCoroutine(SpawnRoutine(effect));
        runningCoroutines[effect] = newCoroutine;
    }

    /// <summary>
    /// 註銷/停止持續生成效果
    /// </summary>
    public void UnregisterSpawnEffect(SpawnObjectEffect effect)
    {
        if (effect == null) return;

        if (runningCoroutines.TryGetValue(effect, out Coroutine coroutine))
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            runningCoroutines.Remove(effect);
        }
    }

    private IEnumerator SpawnRoutine(SpawnObjectEffect effect)
    {
        // 如果有設定間隔，則持續定時生成；否則只生成一次後退出
        if (effect.spawnInterval <= 0f)
        {
            ExecuteSpawn(effect);
            yield break;
        }

        // 先等待一個間隔再開始第一次生成，或是可以立即生成一次
        // 這裡我們選擇立即先生成一次，然後每隔間隔生成
        ExecuteSpawn(effect);

        while (true)
        {
            yield return new WaitForSeconds(effect.spawnInterval);
            ExecuteSpawn(effect);
        }
    }

    private void ExecuteSpawn(SpawnObjectEffect effect)
    {
        Debug.Log($"[PolicyEffectRuntimeManager] 執行定時生成 => 物件: {(effect.prefabToSpawn != null ? effect.prefabToSpawn.name : "Null")}, 數量: {effect.spawnCount}");

        if (effect.prefabToSpawn == null) return;

        // 以玩家位置為基準生成
        Vector3 spawnCenter = Vector3.zero;
        var playerObj = GameObject.FindAnyObjectByType<PlayerController>();
        if (playerObj != null)
        {
            spawnCenter = playerObj.transform.position;
        }

        for (int i = 0; i < effect.spawnCount; i++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-3f, 3f),
                0f,
                Random.Range(-3f, 3f)
            );
            Vector3 spawnPos = spawnCenter + randomOffset;

            GameObject spawnedObj = Instantiate(effect.prefabToSpawn, spawnPos, Quaternion.identity);
            
            // 追蹤此生成物件
            activeSpawnedObjects.Add(spawnedObj);

            // 若有設定生存時間，則自動掛載 AutoDestroy 生命週期管理組件 (資料驅動)
            if (effect.lifetime > 0f)
            {
                var autoDestroy = spawnedObj.GetComponent<AutoDestroy>();
                if (autoDestroy == null)
                {
                    autoDestroy = spawnedObj.AddComponent<AutoDestroy>();
                }
                autoDestroy.lifetime = effect.lifetime;
            }
        }
    }

    // 輔助清理 Null 的引用，避免 activeSpawnedObjects 列表隨時間膨脹
    private void LateUpdate()
    {
        activeSpawnedObjects.RemoveAll(item => item == null);
    }
}
