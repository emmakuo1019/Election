using System;
using UnityEngine;

/// <summary>
/// 生成物件效果積木：實作 ICardEffect 介面
/// 用於生成車隊、網軍等實體物件
/// </summary>
[Serializable]
public class SpawnObjectEffect : ICardEffect
{
    [Header("生成設定")]
    [Tooltip("要生成的 Prefab 物件")]
    public GameObject prefabToSpawn;

    [Tooltip("生成數量 (每次)")]
    public int spawnCount = 1;

    [Tooltip("生成頻率 (秒)，若為 0 則只在獲得卡牌時生成一次")]
    public float spawnInterval = 0f;

    [Tooltip("物件持續時間 (秒)，若為 0 則不自動銷毀")]
    public float lifetime = 0f;

    public void ApplyEffect()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("[SpawnObjectEffect] 缺少 prefabToSpawn，無法生成物件。");
            return;
        }

        // 1. 如果有設定生成間隔，交由 PolicyEffectRuntimeManager 來處理持續性協程生成
        if (spawnInterval > 0f)
        {
            if (PolicyEffectRuntimeManager.Instance != null)
            {
                PolicyEffectRuntimeManager.Instance.RegisterSpawnEffect(this);
                Debug.Log($"[SpawnObjectEffect] 已向 PolicyEffectRuntimeManager 註冊持續性生成效果，間隔: {spawnInterval}秒。");
            }
            else
            {
                Debug.LogWarning("[SpawnObjectEffect] 找不到 PolicyEffectRuntimeManager 實體，無法註冊持續生成效果！將執行單次生成防呆。");
                ExecuteSingleSpawn();
            }
        }
        else
        {
            // 2. 否則執行單次生成
            ExecuteSingleSpawn();
        }
    }

    public void RemoveEffect()
    {
        // 若為持續性生成，在效果移除時應從執行器註銷
        if (spawnInterval > 0f && PolicyEffectRuntimeManager.Instance != null)
        {
            PolicyEffectRuntimeManager.Instance.UnregisterSpawnEffect(this);
            Debug.Log("[SpawnObjectEffect] 已從 PolicyEffectRuntimeManager 註銷持續性生成效果。");
        }
    }

    private void ExecuteSingleSpawn()
    {
        Debug.Log($"[SpawnObjectEffect] 執行單次生成 => 物件: {(prefabToSpawn != null ? prefabToSpawn.name : "Null")}, 數量: {spawnCount}");

        Vector3 spawnCenter = Vector3.zero;
        var playerObj = GameObject.FindAnyObjectByType<PlayerController>();
        if (playerObj != null)
        {
            spawnCenter = playerObj.transform.position;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-2f, 2f),
                0f,
                UnityEngine.Random.Range(-2f, 2f)
            );
            Vector3 spawnPos = spawnCenter + randomOffset;

            GameObject spawnedObj = UnityEngine.Object.Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

            if (lifetime > 0f)
            {
                var autoDestroy = spawnedObj.GetComponent<AutoDestroy>();
                if (autoDestroy == null)
                {
                    autoDestroy = spawnedObj.AddComponent<AutoDestroy>();
                }
                autoDestroy.lifetime = lifetime;
            }
        }
        
        Debug.Log($"[SpawnObjectEffect] 已單次生成 {spawnCount} 個 {prefabToSpawn.name}。");
    }
}
