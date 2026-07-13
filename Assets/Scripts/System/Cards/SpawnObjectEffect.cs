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

    [Tooltip("生成數量")]
    public int spawnCount = 1;

    [Tooltip("物件持續時間 (秒)，若為 0 則不自動銷毀")]
    public float lifetime = 0f;

    public void ApplyEffect()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("[SpawnObjectEffect] 缺少 prefabToSpawn，無法生成物件。");
            return;
        }

        // 取得玩家位置作為生成基準 (如果找不到則以原點生成)
        Vector3 spawnCenter = Vector3.zero;
        var playerObj = GameObject.FindAnyObjectByType<PlayerController>();
        if (playerObj != null)
        {
            spawnCenter = playerObj.transform.position;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            // 在基準位置周圍加上一點隨機偏移，避免物件完全重疊
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-2f, 2f),
                0f,
                UnityEngine.Random.Range(-2f, 2f)
            );
            
            Vector3 spawnPos = spawnCenter + randomOffset;
            
            // 實例化物件
            GameObject spawnedObj = UnityEngine.Object.Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

            // 如果有設定 lifetime，則呼叫 Destroy 進行定時銷毀
            if (lifetime > 0f)
            {
                UnityEngine.Object.Destroy(spawnedObj, lifetime);
            }
        }
        
        Debug.Log($"[SpawnObjectEffect] 已生成 {spawnCount} 個 {prefabToSpawn.name}。");
    }

    public void RemoveEffect()
    {
        // 生成類型的卡牌通常是一次性觸發，不支援復原。
        // 如果未來有需要，可以在這裡加上清空已生成物件的邏輯。
    }
}
