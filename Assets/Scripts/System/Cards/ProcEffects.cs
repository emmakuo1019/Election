using System;
using System.Collections;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// 所有 Proc 觸發型 ICardEffect 實作集中在此檔案。
// 這些效果設計成：每次觸發條件成立就呼叫一次 ApplyEffect()，
// RemoveEffect() 在大多數情況下是 no-op（短暫效果用 coroutine 自我管理）。
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 暫時修改 convertChance（加法 delta）。
/// 效果結束後自動恢復原值。
/// </summary>
[Serializable]
public class ConvertChanceModifierEffect : ICardEffect
{
    [Tooltip("轉化率偏移值（例如 0.2 = +20%）")]
    public float delta = 0.2f;

    [Tooltip("持續時間（秒）")]
    public float durationSeconds = 3f;

    public void ApplyEffect()
    {
        var stats = GameDB.Instance?.Run?.Stats;
        if (stats == null) return;

        float newChance = stats.ModifiedConvertChance + delta;
        stats.SetModifier(StatType.ConvertChance, newChance);

        // durationSeconds <= 0 → 永久累積，不還原
        if (durationSeconds > 0f)
            CoroutineRunner.Run(RestoreAfterDelay(delta, durationSeconds));
    }

    public void RemoveEffect() { }

    private static IEnumerator RestoreAfterDelay(float appliedDelta, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        var stats = GameDB.Instance?.Run?.Stats;
        if (stats != null)
            stats.SetModifier(StatType.ConvertChance, stats.ModifiedConvertChance - appliedDelta);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 暫時提升演說攻擊範圍（乘數）。
/// 委託給 PlayerAttack.ApplyTemporaryRangeBoost()，它自己管理 coroutine。
/// </summary>
[Serializable]
public class AttackRangeModifierEffect : ICardEffect
{
    [Tooltip("倍率（例如 1.5 = +50%）")]
    public float multiplier = 1.5f;

    [Tooltip("持續時間（秒）")]
    public float durationSeconds = 8f;

    public void ApplyEffect()
    {
        var attack = UnityEngine.Object.FindAnyObjectByType<PlayerAttack>();
        if (attack == null)
        {
            Debug.LogWarning("[AttackRangeModifierEffect] 找不到 PlayerAttack，跳過。");
            return;
        }
        attack.ApplyTemporaryRangeBoost(multiplier, durationSeconds);
    }

    public void RemoveEffect() { }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 對玩家周圍 radius 範圍內所有非玩家陣營選民施加立場偏移 amount（正 = 偏向玩家）。
/// </summary>
[Serializable]
public class VoterPositionBurstEffect : ICardEffect
{
    [Tooltip("影響半徑（公尺）")]
    public float radius = 2f;

    [Tooltip("立場偏移量（正值偏向玩家）")]
    public int amount = 1;

    public void ApplyEffect()
    {
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        // ponytail: OverlapSphere 每次觸發一次，N 通常 < 30，不需快取
        var hits = Physics.OverlapSphere(player.transform.position, radius);
        foreach (var col in hits)
        {
            var voter = col.GetComponentInParent<VoterLogic>();
            if (voter == null || voter.Data == null) continue;
            if (voter.Data.IsPlayerAligned) continue; // 已是己方，跳過

            voter.OnInfluence(amount, true, player.transform.position, false);
        }
    }

    public void RemoveEffect() { }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 在 durationSeconds 內，每次觸發時把 convertChance 提升 delta。
/// 效果與 ConvertChanceModifierEffect 相同，語意上是「Combo 加速」。
/// 複用同一個還原邏輯。
/// ponytail: 兩者邏輯完全一致，直接繼承省一個 class 宣告。
/// </summary>
[Serializable]
public class ComboAccelEffect : ConvertChanceModifierEffect { }

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 提供一層誠信護盾，吸收下一次誠信扣除。
/// 透過 PlayerHealthSystem 的護盾層數計數實現。
/// </summary>
[Serializable]
public class IntegrityShieldEffect : ICardEffect
{
    public void ApplyEffect()
    {
        if (PlayerHealthSystem.HasInstance)
            PlayerHealthSystem.Instance.AddShield(1);
        else
            Debug.LogWarning("[IntegrityShieldEffect] 找不到 PlayerHealthSystem。");
    }

    public void RemoveEffect() { }
}
