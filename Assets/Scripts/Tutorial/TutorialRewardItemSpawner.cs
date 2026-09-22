using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 教學專用的獎勵物件生成器。
/// 
/// 與 RewardItemSpawner 的差異：
///   - 使用固定的 3 張政策卡（在 Inspector 中設定），而非從牌池隨機抽取
///   - 生成邏輯完全相同，確保與正式關卡的獎勵選擇流程一致
/// 
/// 職責：
///   1. 監聽 OnRewardSelectionRequested 事件
///   2. 在指定位置生成 3 個 RewardItem（使用 fixedCards 陣列）
///   3. 玩家選擇後套用卡牌效果、移除其餘物件、觸發 TriggerRewardCollected
/// 
/// 使用方式：
///   1. 將此腳本掛在教學場景的獎勵管理物件上
///   2. 在 Inspector 中設定 3 個 spawnPoints（生成位置）
///   3. 設定 rewardItemPrefab（RewardItem Prefab）
///   4. 在 fixedCards 陣列中拖入 3 張想要在教學中展示的政策卡
/// 
/// 注意：
///   此腳本僅用於教學場景，與 RewardItemSpawner 不會同時存在於同一個場景中。
/// </summary>
public class TutorialRewardItemSpawner : MonoBehaviour
{
    [Header("生成設定")]
    [Tooltip("三個獎勵物件的生成位置，建議排列在場景中段偏右")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("RewardItem Prefab（含 3D 底座 + Billboard SpriteRenderer + Collider）")]
    [SerializeField] private RewardItem rewardItemPrefab;

    [Header("教學固定卡牌")]
    [Tooltip("教學中固定顯示的 3 張政策卡（不從牌池抽取，而是在此直接指定）")]
    [SerializeField] private PolicyCardData[] fixedCards = new PolicyCardData[3];

    private readonly List<RewardItem> _spawnedItems = new List<RewardItem>();
    private bool _rewardActive = false;

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void OnEnable()
    {
        BattleEventManager.OnRewardSelectionRequested += OnRewardSelectionRequested;
    }

    private void OnDisable()
    {
        BattleEventManager.OnRewardSelectionRequested -= OnRewardSelectionRequested;
    }

    // ── 事件處理 ─────────────────────────────────────────────────────

    private void OnRewardSelectionRequested(bool useDistressReward)
    {
        if (_rewardActive) return;
        SpawnRewardCards();
    }

    // ── 生成邏輯 ─────────────────────────────────────────────────────

    private void SpawnRewardCards()
    {
        if (rewardItemPrefab == null)
        {
            Debug.LogWarning("[TutorialRewardItemSpawner] rewardItemPrefab 未設定！");
            BattleEventManager.TriggerRewardCollected();
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[TutorialRewardItemSpawner] spawnPoints 未設定！");
            BattleEventManager.TriggerRewardCollected();
            return;
        }

        // 檢查固定卡牌陣列
        if (fixedCards == null || fixedCards.Length == 0)
        {
            Debug.LogWarning("[TutorialRewardItemSpawner] fixedCards 未設定，無法生成獎勵");
            BattleEventManager.TriggerRewardCollected();
            return;
        }

        // 過濾掉 null 的卡牌
        var validCards = new List<PolicyCardData>();
        foreach (var card in fixedCards)
        {
            if (card != null)
                validCards.Add(card);
        }

        if (validCards.Count == 0)
        {
            Debug.LogWarning("[TutorialRewardItemSpawner] fixedCards 中沒有有效的卡牌資料");
            BattleEventManager.TriggerRewardCollected();
            return;
        }

        _rewardActive = true;
        _spawnedItems.Clear();

        int count = Mathf.Min(spawnPoints.Length, validCards.Count);
        if (count == 0)
        {
            BattleEventManager.TriggerRewardCollected();
            return;
        }

        for (int i = 0; i < count; i++)
        {
            // 每關僅生成 3 個，GC 壓力可忽略，不入池
            RewardItem item = Instantiate(rewardItemPrefab, spawnPoints[i].position, spawnPoints[i].rotation);
            item.Setup(validCards[i]);
            item.OnItemSelected += HandleItemSelected;
            _spawnedItems.Add(item);
        }

        Debug.Log($"[TutorialRewardItemSpawner] 生成 {count} 個獎勵物件（教學固定卡牌）");
    }

    // ── 選擇處理 ─────────────────────────────────────────────────────

    private void HandleItemSelected(RewardItem selected, PolicyCardData card)
    {
        // 套用卡牌效果
        if (card != null)
        {
            GameDB.Instance?.Run.AddPolicyCard(card);
            Debug.Log($"[TutorialRewardItemSpawner] 套用卡牌：{card.cardName}");
        }

        // 移除所有獎勵物件（包含已選與未選）
        foreach (var item in _spawnedItems)
        {
            if (item != null)
            {
                item.OnItemSelected -= HandleItemSelected;
                Destroy(item.gameObject);
            }
        }
        _spawnedItems.Clear();
        _rewardActive = false;

        Debug.Log("[TutorialRewardItemSpawner] 選擇完成，通知獎勵完成");
        BattleEventManager.TriggerRewardCollected();
    }
}
