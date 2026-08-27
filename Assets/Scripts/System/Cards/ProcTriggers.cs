using System;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// 所有 IProcTrigger 實作集中在此檔案，減少散落的小檔案。
// 每個觸發器只做一件事：訂閱 BattleEventManager 上的對應事件，
// 條件符合時呼叫 onTriggered callback，其餘什麼也不做。
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 使用任意技能 (J/K/L) 後觸發一次。
/// fireOncePerRoom = true 時，每個房間只觸發第一次（透過 BattleEventManager.OnRoomCleared 重置）。
/// </summary>
[Serializable]
public class OnAnySkillUsedTrigger : IProcTrigger
{
    [Tooltip("true = 每間房只觸發一次（首次使用技能）")]
    public bool fireOncePerRoom = false;

    private Action _callback;
    private bool _firedThisRoom = false;

    public void Register(Action onTriggered)
    {
        _callback = onTriggered;
        _firedThisRoom = false;
        BattleEventManager.OnAnySkillUsed += OnSkillUsed;
        if (fireOncePerRoom)
            BattleEventManager.OnRoomCleared += ResetFired;
    }

    public void Unregister()
    {
        BattleEventManager.OnAnySkillUsed -= OnSkillUsed;
        if (fireOncePerRoom)
            BattleEventManager.OnRoomCleared -= ResetFired;
        _callback = null;
    }

    private void OnSkillUsed(SkillData _)
    {
        if (fireOncePerRoom && _firedThisRoom) return;
        _firedThisRoom = true;
        _callback?.Invoke();
    }

    private void ResetFired() => _firedThisRoom = false;
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 玩家成功轉化任一選民後觸發。
/// </summary>
[Serializable]
public class OnVoterConvertedTrigger : IProcTrigger
{
    private Action _callback;

    public void Register(Action onTriggered)
    {
        _callback = onTriggered;
        BattleEventManager.OnVoterConverted += OnConverted;
    }

    public void Unregister()
    {
        BattleEventManager.OnVoterConverted -= OnConverted;
        _callback = null;
    }

    // 只在玩家側轉化（side == 1）時觸發
    private void OnConverted(int side)
    {
        if (side == VoterData.PlayerSideSign)
            _callback?.Invoke();
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 在 windowSeconds 秒內轉化達到 threshold 名選民後觸發一次。
/// 觸發後重置計數，可重複觸發。
/// </summary>
[Serializable]
public class OnConsecutiveConvertTrigger : IProcTrigger
{
    [Tooltip("時間窗口（秒）")]
    public float windowSeconds = 3f;

    [Tooltip("需達到的轉化數量")]
    public int threshold = 3;

    private Action _callback;

    // ponytail: 用 ring-buffer 記時間戳成本更低，但 threshold ≤ 20 時 List 夠用
    private readonly System.Collections.Generic.List<float> _timestamps =
        new System.Collections.Generic.List<float>();

    public void Register(Action onTriggered)
    {
        _callback = onTriggered;
        BattleEventManager.OnVoterConverted += OnConverted;
    }

    public void Unregister()
    {
        BattleEventManager.OnVoterConverted -= OnConverted;
        _callback = null;
        _timestamps.Clear();
    }

    private void OnConverted(int side)
    {
        if (side != VoterData.PlayerSideSign) return;

        float now = Time.time;
        _timestamps.Add(now);

        // 移除超出時間窗口的舊紀錄
        _timestamps.RemoveAll(t => now - t > windowSeconds);

        if (_timestamps.Count >= threshold)
        {
            _timestamps.Clear(); // 重置，允許再次觸發
            _callback?.Invoke();
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 轉化深色（Dark）選民時觸發。
/// 需要 BattleEventManager 廣播時攜帶屬性資訊；
/// 此處透過偵聽 VoterData.OnConversionSuccess 的全域版本實現。
/// ponytail: BattleEventManager 目前不傳 VoterAttribute，
///   改用訂閱全部選民的 OnConversionSuccess + 檢查 HasDarkAttribute 最省改動。
///   如果未來 BattleEventManager 擴充屬性參數可直接替換。
/// </summary>
[Serializable]
public class OnDarkVoterConvertedTrigger : IProcTrigger
{
    private Action _callback;

    public void Register(Action onTriggered)
    {
        _callback = onTriggered;
        // 用 OnVoterConverted + Dark 屬性過濾：
        // VoterLogic.UpdateConversionState 先更新 ConvertedSide，
        // 再呼叫 TriggerOnVoterConverted。
        // 但此時我們無法從靜態事件得知屬性，
        // 改為訂閱自訂的 OnDarkVoterConverted（由 VoterLogic 額外觸發）。
        BattleEventManager.OnDarkVoterConverted += OnDarkConverted;
    }

    public void Unregister()
    {
        BattleEventManager.OnDarkVoterConverted -= OnDarkConverted;
        _callback = null;
    }

    private void OnDarkConverted() => _callback?.Invoke();
}
