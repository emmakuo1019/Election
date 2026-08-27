using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵人全滅後在場景中生成獎勵物件，玩家選擇一個後其餘消失並解鎖出口。
///
/// 放置於戰鬥場景的 GameObject 上。
/// 在 Inspector 設定三個生成點（spawnPoints）與 RewardItem Prefab。
///
/// 流程：
///   OnAllEnemiesDefeated
///   → 從牌池抽 3 張卡
///   → 在 spawnPoints 生成 3 個 RewardItem
///   → 玩家選擇其中一個
///   → 套用卡牌效果、其餘物件消失
///   → 解鎖出口（TriggerAllEnemiesDefeated 已觸發，DoorController 已解鎖門）
///
/// 第 5/10 關技能選擇：
///   條件符合時生成技能選擇物件而非政策卡（TODO：待技能選擇系統建立後實作）
/// </summary>
public class RewardItemSpawner : MonoBehaviour
{
    [Header("生成設定")]
    [Tooltip("三個獎勵物件的生成位置，建議排列在場景中段偏右")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("RewardItem Prefab（含 3D 底座 + Billboard SpriteRenderer + Collider）")]
    [SerializeField] private RewardItem rewardItemPrefab;

    [Header("獎勵數量")]
    [SerializeField] private int rewardCount = 3;

    private readonly List<RewardItem> _spawnedItems = new List<RewardItem>();
    private bool _rewardActive = false;

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void OnEnable()
    {
        BattleEventManager.OnAllEnemiesDefeated += OnEnemiesDefeated;
    }

    private void OnDisable()
    {
        BattleEventManager.OnAllEnemiesDefeated -= OnEnemiesDefeated;
    }

    // ── 事件處理 ─────────────────────────────────────────────────────

    private void OnEnemiesDefeated()
    {
        if (_rewardActive) return;

        // 第 5/10 關：技能選擇（ponytail: TODO 待技能選擇系統建立後替換此分支）
        int roomNumber = GameDB.Instance?.Campaign.CurrentRoomCount ?? 0;
        if (roomNumber % 5 == 0 && roomNumber > 0)
        {
            Debug.Log($"[RewardItemSpawner] 第 {roomNumber} 關，技能選擇預留（尚未實作）");
            // TODO: SpawnSkillChoices();
            // 暫時 fallback 至普通選卡
        }

        SpawnRewardCards();
    }

    // ── 生成邏輯 ─────────────────────────────────────────────────────

    private void SpawnRewardCards()
    {
        if (rewardItemPrefab == null)
        {
            Debug.LogWarning("[RewardItemSpawner] rewardItemPrefab 未設定！");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[RewardItemSpawner] spawnPoints 未設定！");
            return;
        }

        var cards = GameDB.Instance?.Run.DrawRandomPolicyCards(rewardCount);
        if (cards == null || cards.Count == 0)
        {
            Debug.LogWarning("[RewardItemSpawner] 牌池為空，無法生成獎勵");
            return;
        }

        _rewardActive = true;
        _spawnedItems.Clear();

        int count = Mathf.Min(rewardCount, spawnPoints.Length, cards.Count);
        for (int i = 0; i < count; i++)
        {
            // ponytail: 每關僅生成 3 個，GC 壓力可忽略，不入池；若未來需大量動態生成再改 PoolManager
            RewardItem item = Instantiate(rewardItemPrefab, spawnPoints[i].position, spawnPoints[i].rotation);
            item.Setup(cards[i]);
            item.OnItemSelected += HandleItemSelected;
            _spawnedItems.Add(item);
        }

        Debug.Log($"[RewardItemSpawner] 生成 {count} 個獎勵物件");
    }

    // ── 選擇處理 ─────────────────────────────────────────────────────

    private void HandleItemSelected(RewardItem selected, PolicyCardData card)
    {
        // 套用卡牌效果
        if (card != null)
        {
            GameDB.Instance?.Run.AddPolicyCard(card);
            Debug.Log($"[RewardItemSpawner] 套用卡牌：{card.cardName}");
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

        // 先通知「獎勵已領取」，讓 DoorController 完成第二道解鎖條件，再觸發過關
        Debug.Log("[RewardItemSpawner] 選擇完成，通知獎勵完成並觸發過關");
        BattleEventManager.TriggerRewardCollected();
        BattleEventManager.TriggerRoomCleared();
    }
}
