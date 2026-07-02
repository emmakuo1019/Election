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
    /// 重新開始遊戲時，重置單局資料 (RunData)
    /// 注意：如果是在遊戲中重置，請確保 UI 有重新抓取參考或重新訂閱。
    /// 通常在離開結算畫面、回到主選單，或新局開始時呼叫。
    /// </summary>
    public void ResetRunData()
    {
        Run = new RunData();
        Debug.Log("[GameDB] RunData 已重置");
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
    
    // 參數：當前社會風氣
    public event Action<int> OnAtmosphereChanged;

    public void ModifyAtmosphere(int amount)
    {
        SocialAtmosphere = Mathf.Clamp(SocialAtmosphere + amount, MinAtmosphere, MaxAtmosphere);
        OnAtmosphereChanged?.Invoke(SocialAtmosphere);
    }

    public void ResetAtmosphere()
    {
        SocialAtmosphere = 0;
        OnAtmosphereChanged?.Invoke(SocialAtmosphere);
    }
    #endregion


}

/// <summary>
/// 戰役/推進進度資料 (保留目前 PlayerPrefs 處理的關卡狀態)
/// </summary>
[System.Serializable]
public class CampaignData
{
    public int CompletedBlocks { get; private set; }
    public int CurrentBlockIndex { get; private set; } = 1;
    public int CurrentRoomCount { get; private set; } = 0;
    public int MaxRoomsInBlock { get; private set; } = 5;

    public event Action<int, int> OnRoomProgressChanged;

    public void SetCampaignProgress(int completedBlocks, int currentBlockIndex)
    {
        CompletedBlocks = completedBlocks;
        CurrentBlockIndex = currentBlockIndex;
    }

    public void SetRoomProgress(int roomCount, int maxRooms)
    {
        CurrentRoomCount = roomCount;
        MaxRoomsInBlock = maxRooms;
        OnRoomProgressChanged?.Invoke(CurrentRoomCount, MaxRoomsInBlock);
    }
}
