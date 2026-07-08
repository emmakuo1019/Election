using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 遊戲核心資料庫，負責跨場景保存所有遊戲狀態。
/// 統一所有數值的讀寫入口，並透過事件系統 (Event-Driven) 廣播數值變更。
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameDB : MonoBehaviour
{
    public static GameDB Instance { get; private set; }

    [Header("Data Modules")]
    public PlayerData Player { get; private set; }
    public RunData Run { get; private set; }
    public CampaignData Campaign { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化資料模組
        Player = new PlayerData();
        Run = new RunData();
        Campaign = new CampaignData();
    }

    /// <summary>
    /// 重新開始遊戲時，重置單局資料 (RunData)。
    /// 通常在離開結算畫面、回到主選單，或新局開始時呼叫。
    /// </summary>
    public void ResetRunData()
    {
        Run = new RunData();
        Debug.Log("[GameDB] RunData 已重置");
    }

    /// <summary>
    /// 重置戰役進度 (CampaignData)，回到遊戲最開始的狀態。
    /// 通常在回到主選單 / 總部場景時呼叫。
    /// </summary>
    public void ResetCampaignData()
    {
        Campaign = new CampaignData();
        Debug.Log("[GameDB] CampaignData 已重置");
    }
}

/// <summary>
/// 玩家長期資料 (跨單局保留的設定、解鎖、裝備技能等)
/// </summary>
[System.Serializable]
public class PlayerData
{
    // 當前裝備的技能
    public SkillData EquippedPartySkill { get; private set; }
    public SkillData BaseSkillJ { get; private set; }

    public event Action<SkillData> OnPartySkillEquipped;
    public event Action<SkillData> OnBaseSkillJEquipped;

    public void EquipPartySkill(SkillData skill)
    {
        EquippedPartySkill = skill;
        OnPartySkillEquipped?.Invoke(skill);
    }

    public void EquipBaseSkillJ(SkillData skill)
    {
        BaseSkillJ = skill;
        OnBaseSkillJEquipped?.Invoke(skill);
    }
}

/// <summary>
/// 單局/關卡運行資料 (HP, MP, 選票, 社會風氣, 政策卡 Buff 等)
/// </summary>
[System.Serializable]
public class RunData
{
    public List<string> AcquiredPolicyCards { get; private set; } = new List<string>();

    public void AddPolicyCard(string cardName)
    {
        if (!string.IsNullOrEmpty(cardName) && !AcquiredPolicyCards.Contains(cardName))
        {
            AcquiredPolicyCards.Add(cardName);
        }
    }

    #region 政治誠信 HP (Integrity HP)
    public float IntegrityHp { get; private set; } = 70f;
    public float MaxIntegrityHp { get; private set; } = 100f;
    
    // 參數：當前值, 最大值
    public event Action<float, float> OnIntegrityHpChanged;

    public void ModifyIntegrityHp(float amount)
    {
        IntegrityHp = Mathf.Clamp(IntegrityHp + amount, 0f, MaxIntegrityHp);
        OnIntegrityHpChanged?.Invoke(IntegrityHp, MaxIntegrityHp);
    }

    public void SetMaxIntegrityHp(float maxHp, bool refill = false)
    {
        MaxIntegrityHp = Mathf.Max(1f, maxHp);
        IntegrityHp = refill ? MaxIntegrityHp : Mathf.Clamp(IntegrityHp, 0f, MaxIntegrityHp);
        OnIntegrityHpChanged?.Invoke(IntegrityHp, MaxIntegrityHp);
    }
    #endregion

    #region 資金 MP (Funds MP)
    public int CurrentMP { get; private set; } = 100;
    public int MaxMP { get; private set; } = 100;
    
    // 參數：當前值, 最大值
    public event Action<int, int> OnMPChanged;

    public void ModifyMP(int amount)
    {
        CurrentMP = Mathf.Clamp(CurrentMP + amount, 0, MaxMP);
        OnMPChanged?.Invoke(CurrentMP, MaxMP);
    }

    public void SetMaxMP(int maxMp, bool refill = false)
    {
        MaxMP = Mathf.Max(1, maxMp);
        CurrentMP = refill ? MaxMP : Mathf.Clamp(CurrentMP, 0, MaxMP);
        OnMPChanged?.Invoke(CurrentMP, MaxMP);
    }
    #endregion

    #region 選票 (Votes)
    public int PlayerVotes { get; private set; } = 50;
    public int OpponentVotes { get; private set; } = 50;
    
    public int TotalVotes => PlayerVotes + OpponentVotes;
    public float PlayerVotePercentage => TotalVotes > 0 ? (float)PlayerVotes / TotalVotes : 0f;
    
    // 參數：玩家得票, 對手得票
    public event Action<int, int> OnVotesChanged;

    public void AddVote(int playerVoteDelta, int opponentVoteDelta)
    {
        PlayerVotes = Mathf.Max(0, PlayerVotes + playerVoteDelta);
        OpponentVotes = Mathf.Max(0, OpponentVotes + opponentVoteDelta);
        OnVotesChanged?.Invoke(PlayerVotes, OpponentVotes);
    }

    public void SetVotes(int playerVotes, int opponentVotes)
    {
        PlayerVotes = Mathf.Max(0, playerVotes);
        OpponentVotes = Mathf.Max(0, opponentVotes);
        OnVotesChanged?.Invoke(PlayerVotes, OpponentVotes);
    }

    public void AddPlayerVotes(int amount) => AddVote(amount, 0);
    public void AddOpponentVotes(int amount) => AddVote(0, amount);
    #endregion

    #region 社會風氣 (Social Atmosphere)
    public int SocialAtmosphere { get; private set; } = 0;
    public int MinAtmosphere { get; private set; } = -100;
    public int MaxAtmosphere { get; private set; } = 100;
    
    public float AtmosphereNormalized => (float)(SocialAtmosphere - MinAtmosphere) / (MaxAtmosphere - MinAtmosphere);
    
    // 參數：舊值, 新值
    public event Action<int, int> OnAtmosphereChanged;

    public void ModifyAtmosphere(int amount)
    {
        int oldValue = SocialAtmosphere;
        SocialAtmosphere = Mathf.Clamp(SocialAtmosphere + amount, MinAtmosphere, MaxAtmosphere);
        if (oldValue != SocialAtmosphere)
        {
            OnAtmosphereChanged?.Invoke(oldValue, SocialAtmosphere);
        }
    }

    public void ResetAtmosphere()
    {
        int oldValue = SocialAtmosphere;
        SocialAtmosphere = 0;
        if (oldValue != SocialAtmosphere)
        {
            OnAtmosphereChanged?.Invoke(oldValue, SocialAtmosphere);
        }
    }

    public float GetDarkVoterRate()
    {
        // 情緒動員越強 (正值)，深色選民越常出現。
        float emotionalBias = Mathf.InverseLerp(MinAtmosphere, MaxAtmosphere, SocialAtmosphere);
        return Mathf.Lerp(0.1f, 0.6f, emotionalBias);
    }

    public string GetAtmosphereDescription()
    {
        if (SocialAtmosphere > 70) return "🔴 強情緒動員";
        if (SocialAtmosphere > 30) return "🟠 傾向情感";
        if (SocialAtmosphere >= -30) return "⚪ 理性與情感平衡";
        if (SocialAtmosphere >= -70) return "🟢 傾向理性";
        return "🟢 強理性政策";
    }

    public bool IsRationalTendency() => SocialAtmosphere < 0;
    public bool IsEmotionalTendency() => SocialAtmosphere > 0;
    public bool IsNeutral() => SocialAtmosphere == 0;
    #endregion


}

/// <summary>
/// 戰役/推進進度資料，取代原本分散在 BlockProgressManager 與
/// CampaignProgressManager 裡的 PlayerPrefs 儲存。
/// 所有欄位存活於 GameDB (DontDestroyOnLoad)，遊戲崩潰不會留下殘留狀態。
/// </summary>
[System.Serializable]
public class CampaignData
{
    // ── 已完成的 Block 數量 ──────────────────────────────────────────
    public int CompletedBlocks { get; private set; } = 0;

    // ── 當前 Block 進度 ──────────────────────────────────────────────
    public int CurrentBlockIndex   { get; private set; } = 1;
    public int CurrentRoomCount    { get; private set; } = 0;
    public int MaxRoomsInBlock     { get; private set; } = 5;

    // 當前 Block 的房間場景序列，例如 ["TestMVP","TestSpecial","TestMVP",...]
    public string[] RoomSequence   { get; private set; } = System.Array.Empty<string>();

    // 用於強制覆蓋下一間房間要載入的場景 (例如失敗跳結算畫面)
    public string NextSceneOverride { get; private set; } = string.Empty;

    // ── 事件 ─────────────────────────────────────────────────────────
    public event System.Action<int, int> OnRoomProgressChanged;

    // ── CompletedBlocks ──────────────────────────────────────────────
    public void AddCompletedBlock()
    {
        CompletedBlocks++;
    }

    public void ResetCompletedBlocks()
    {
        CompletedBlocks = 0;
    }

    // ── Block 初始化 ─────────────────────────────────────────────────
    public void InitBlock(int maxRooms, int blockIndex)
    {
        CurrentRoomCount = 0;
        MaxRoomsInBlock  = Mathf.Max(1, maxRooms);
        CurrentBlockIndex = Mathf.Max(1, blockIndex);
        RoomSequence     = System.Array.Empty<string>();
        NextSceneOverride = string.Empty;
        OnRoomProgressChanged?.Invoke(CurrentRoomCount, MaxRoomsInBlock);
    }

    public void SetRoomSequence(string[] sequence)
    {
        RoomSequence = sequence ?? System.Array.Empty<string>();
    }

    // ── 房間推進 ─────────────────────────────────────────────────────
    public void EnterNextRoom()
    {
        CurrentRoomCount++;
        OnRoomProgressChanged?.Invoke(CurrentRoomCount, MaxRoomsInBlock);
    }

    // ── 查詢 ─────────────────────────────────────────────────────────
    public bool HasBlockProgress() => MaxRoomsInBlock > 0 && CurrentRoomCount > 0;

    public bool IsLastRoomInBlock() => CurrentRoomCount >= MaxRoomsInBlock;

    public string GetCurrentRoomSceneName()
    {
        if (RoomSequence.Length == 0) return string.Empty;
        int idx = Mathf.Clamp(CurrentRoomCount - 1, 0, RoomSequence.Length - 1);
        return RoomSequence[idx];
    }

    // ── 場景覆蓋 ─────────────────────────────────────────────────────
    public void SetNextSceneOverride(string sceneName)
    {
        NextSceneOverride = sceneName ?? string.Empty;
    }

    /// <summary>取出並清除場景覆蓋，一次性使用。</summary>
    public string ConsumeNextSceneOverride()
    {
        string scene = NextSceneOverride;
        NextSceneOverride = string.Empty;
        return scene;
    }

    // ── 清除 ─────────────────────────────────────────────────────────
    public void ClearBlockProgress()
    {
        CurrentRoomCount  = 0;
        MaxRoomsInBlock   = 0;
        CurrentBlockIndex = 1;
        RoomSequence      = System.Array.Empty<string>();
        NextSceneOverride = string.Empty;
    }
}
