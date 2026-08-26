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
    public List<PolicyCardData> allPolicyCards = new List<PolicyCardData>();

    [Header("任務系統")]
    [Tooltip("戰役可用的任務池")]
    public MissionPool missionPool;
    
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

    // 選擇的黨內派系
    public FactionData SelectedFaction { get; private set; }
    public event Action<FactionData> OnFactionSelected;

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

    /// <summary>
    /// 選擇派系並自動裝備該派系的起始技能
    /// </summary>
    public void SelectFaction(FactionData faction)
    {
        SelectedFaction = faction;
        OnFactionSelected?.Invoke(faction);

        // 自動裝備派系起始技能到 J 鍵
        if (faction != null && faction.starterSkill != null)
        {
            EquipBaseSkillJ(faction.starterSkill);
            Debug.Log($"[PlayerData] 選擇派系：{faction.factionName}，裝備起始技能：{faction.starterSkill.skillName}");
        }
    }
}

/// <summary>
/// 單局/關卡運行資料 (HP, MP, 選票, 社會風氣, 政策卡 Buff 等)
/// </summary>
[System.Serializable]
public class RunData
{
    public List<string> AcquiredPolicyCards { get; private set; } = new List<string>();
    
    // 儲存玩家當前擁有的完整卡牌實體
    public List<PolicyCardData> ActiveCards { get; private set; } = new List<PolicyCardData>();
    
    public PlayerStatsData Stats { get; private set; } = new PlayerStatsData();

    // 技能選擇掛起 (Pending) 狀態，取代舊有的 PlayerPrefs 標記
    public bool HasPendingSkillSelection { get; set; } = false;

    public void AddPolicyCard(PolicyCardData card)
    {
        if (card != null && !AcquiredPolicyCards.Contains(card.cardName))
        {
            AcquiredPolicyCards.Add(card.cardName);
            ActiveCards.Add(card);

            // 套用卡牌效果
            card.ApplyAllEffects();
            
            // 通知 UI 數值可能已變更
            Stats.NotifyStatsChanged();
        }
    }



    /// <summary>
    /// 從 GameDB.Instance.allPolicyCards 載入所有政策卡，並過濾掉已經擁有的卡牌，隨機抽出指定數量。
    /// </summary>
    public List<PolicyCardData> DrawRandomPolicyCards(int count)
    {
#if UNITY_EDITOR
        if (GameDB.Instance.allPolicyCards == null || GameDB.Instance.allPolicyCards.Count == 0)
        {
            Debug.LogWarning("[GameDB] allPolicyCards 為空！正在 Editor 模式下嘗試自動載入 Assets/Data/PolicyCards/ 內的卡牌...");
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:PolicyCardData", new[] { "Assets/Data/PolicyCards" });
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                PolicyCardData cardData = UnityEditor.AssetDatabase.LoadAssetAtPath<PolicyCardData>(path);
                if (cardData != null)
                {
                    GameDB.Instance.allPolicyCards.Add(cardData);
                }
            }
        }
#endif

        var allCards = GameDB.Instance.allPolicyCards;
        List<PolicyCardData> tempPool = new List<PolicyCardData>();

        foreach (PolicyCardData card in allCards)
        {
            if (card != null && !AcquiredPolicyCards.Contains(card.cardName))
            {
                tempPool.Add(card);
            }
        }

        List<PolicyCardData> result = new List<PolicyCardData>();
        int drawCount = Mathf.Min(count, tempPool.Count);

        for (int i = 0; i < drawCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, tempPool.Count);
            result.Add(tempPool[randomIndex]);
            tempPool.RemoveAt(randomIndex);
        }

        return result;
    }

    /// <summary>
    /// 隨機獲取單張未擁有的政策卡 (對接舊 UI 結算)
    /// </summary>
    public PolicyCardData GetRandomPolicyCard()
    {
        var cards = DrawRandomPolicyCards(1);
        return cards.Count > 0 ? cards[0] : null;
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
        return Mathf.Lerp(0.1f, 0.7f, emotionalBias);
    }

    /// <summary>
    /// 根據社會風氣計算冷感選民生成機率。
    /// 理性風氣越強 (負值)，冷感選民越常出現。
    /// </summary>
    public float GetColdVoterRate()
    {
        if (SocialAtmosphere >= 0) return 0f;
        float rationalBias = Mathf.InverseLerp(0, MinAtmosphere, SocialAtmosphere);
        return Mathf.Lerp(0.1f, 0.5f, rationalBias);
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
/// 一個待選/已選的房間選項，包含場景名稱與任務定義。
/// </summary>
[System.Serializable]
public struct RoomOption
{
    public string sceneName;
    public RoomMissionData mission;  // null = 無任務（例如 Boss 房）

    public RoomOption(string sceneName, RoomMissionData mission = null)
    {
        this.sceneName = sceneName;
        this.mission   = mission;
    }
}

/// <summary>
/// 戰役/推進進度資料，取代原本分散在 BlockProgressManager 與
/// CampaignProgressManager 裡的 PlayerPrefs 儲存。
/// 所有欄位存活於 GameDB (DontDestroyOnLoad)，遊戲崩潰不會留下殘留狀態。
/// </summary>
[System.Serializable]
public class CampaignData
{
    // ── 戰役常數與設定 ────────────────────────────────────────────────
    public const int TotalBlockCount = 3;
    public const int BossStageIndex = 4;

    private const string NormalRoomSceneName = "TestMVP";
    private const string SpecialRoomSceneName = "TestSpecial";
    private const float SpecialRoomChance = 0.2f;

    // ── 已完成的 Block 數量 ──────────────────────────────────────────
    public int CompletedBlocks { get; private set; } = 0;

    // ── 當前 Block 進度 ──────────────────────────────────────────────
    public int CurrentBlockIndex   { get; private set; } = 1;
    public int CurrentRoomCount    { get; private set; } = 0;
    public int MaxRoomsInBlock     { get; private set; } = 5;

    // 當前 Block 的房間場景序列，例如 ["TestMVP","TestSpecial","TestMVP",...]
    // ponytail: 保留供舊呼叫端相容，新流程走 PendingOptions/ActiveRoom
    public string[] RoomSequence   { get; private set; } = System.Array.Empty<string>();

    // ── 任務選項系統 ──────────────────────────────────────────────────
    // 玩家到達岔路口時顯示的兩個選項（由上一關結算後生成）
    public RoomOption[] PendingOptions { get; private set; } = System.Array.Empty<RoomOption>();

    // 玩家選擇後的當前房間（進入關卡時讀取任務資料）
    public RoomOption ActiveRoom { get; private set; }

    // 用於強制覆蓋下一間房間要載入的場景 (例如失敗跳結算畫面)
    public string NextSceneOverride { get; private set; } = string.Empty;

    // ── 已解鎖技能列表 ────────────────────────────────────────────────
    public List<SkillData> UnlockedSkills { get; private set; } = new List<SkillData>();
    public event System.Action<SkillData> OnSkillUnlocked;

    public void UnlockSkill(SkillData skill)
    {
        if (skill != null && !UnlockedSkills.Contains(skill))
        {
            UnlockedSkills.Add(skill);
            OnSkillUnlocked?.Invoke(skill);
            Debug.Log($"[CampaignData] 技能解鎖成功：{skill.skillName}");
        }
    }

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
        CurrentRoomCount  = 0;
        MaxRoomsInBlock   = Mathf.Max(1, maxRooms);
        CurrentBlockIndex = Mathf.Max(1, blockIndex);
        RoomSequence      = System.Array.Empty<string>();
        NextSceneOverride = string.Empty;
        PendingOptions    = System.Array.Empty<RoomOption>();
        ActiveRoom        = default;
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
        // 優先從玩家已選的 ActiveRoom 取場景名
        if (!string.IsNullOrEmpty(ActiveRoom.sceneName))
            return ActiveRoom.sceneName;

        // ponytail: fallback 至舊 RoomSequence，供尚未切換新流程的呼叫端相容
        if (RoomSequence.Length == 0) return string.Empty;
        int idx = Mathf.Clamp(CurrentRoomCount - 1, 0, RoomSequence.Length - 1);
        return RoomSequence[idx];
    }

    // ── 任務選項 ─────────────────────────────────────────────────────

    /// <summary>
    /// 結算後呼叫，為岔路口生成兩個待選房間選項。
    /// mission 參數由外部（StageClearState 或關卡設定）傳入；
    /// 傳 null 代表該路線無指定任務（保持舊行為）。
    /// </summary>
    public void GenerateNextOptions(RoomMissionData missionA = null, RoomMissionData missionB = null)
    {
        string sceneA = UnityEngine.Random.value < SpecialRoomChance ? SpecialRoomSceneName : NormalRoomSceneName;
        string sceneB = UnityEngine.Random.value < SpecialRoomChance ? SpecialRoomSceneName : NormalRoomSceneName;
        PendingOptions = new RoomOption[]
        {
            new RoomOption(sceneA, missionA),
            new RoomOption(sceneB, missionB),
        };
        Debug.Log($"[CampaignData] 生成岔路選項：A={sceneA} B={sceneB}");
    }

    /// <summary>
    /// 玩家選擇岔路（0=左門, 1=右門），設定 ActiveRoom 並清空 PendingOptions。
    /// </summary>
    public void SelectOption(int index)
    {
        if (PendingOptions.Length == 0)
        {
            Debug.LogWarning("[CampaignData] SelectOption 呼叫時 PendingOptions 為空");
            return;
        }
        int safeIndex = Mathf.Clamp(index, 0, PendingOptions.Length - 1);
        ActiveRoom = PendingOptions[safeIndex];
        PendingOptions = System.Array.Empty<RoomOption>();
        Debug.Log($"[CampaignData] 玩家選擇選項 {safeIndex}，場景：{ActiveRoom.sceneName}");
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
        PendingOptions    = System.Array.Empty<RoomOption>();
        ActiveRoom        = default;
    }

    // ── 戰役進度與 Block 推進移植 ──────────────────────────────────────

    public int GetNextBlockIndex()
    {
        return Mathf.Clamp(CompletedBlocks + 1, 1, TotalBlockCount);
    }

    public bool IsBlockCompleted(int blockIndex)
    {
        int normalized = Mathf.Clamp(blockIndex, 1, TotalBlockCount);
        return CompletedBlocks >= normalized;
    }

    public bool CanEnterBlock(int blockIndex)
    {
        int normalized = Mathf.Clamp(blockIndex, 1, TotalBlockCount);
        return CompletedBlocks == normalized - 1;
    }

    public bool CanEnterBossStage()
    {
        return CompletedBlocks >= TotalBlockCount;
    }

    /// <summary>
    /// 啟動指定 Block，生成房間序列並進入第一間房，回傳第一間房的場景名稱。
    /// </summary>
    public string StartRandomBlock(int blockIndex, int maxRooms = 5)
    {
        int safeBlockIndex = Mathf.Clamp(blockIndex, 1, TotalBlockCount);
        int safeMaxRooms   = Mathf.Max(1, maxRooms);

        InitBlock(safeMaxRooms, safeBlockIndex);
        SetRoomSequence(GenerateRoomSequence(safeBlockIndex, safeMaxRooms));
        EnterNextRoom();

        return GetCurrentRoomSceneName();
    }

    /// <summary>
    /// 依照戰役進度，啟動下一個 Block。
    /// </summary>
    public string StartNextCampaignBlock(int maxRooms = 5)
    {
        int nextBlockIndex = GetNextBlockIndex();
        return StartRandomBlock(nextBlockIndex, maxRooms);
    }

    public bool TryCompleteCurrentBlock()
    {
        if (!HasBlockProgress() || !IsLastRoomInBlock())
            return false;

        AddCompletedBlock();
        
        // 標記待選技能
        if (GameDB.Instance != null && GameDB.Instance.Run != null)
        {
            GameDB.Instance.Run.HasPendingSkillSelection = true;
        }

        ClearBlockProgress();
        return true;
    }

    public void FailCurrentBlock()
    {
        ClearBlockProgress();
    }

    private string[] GenerateRoomSequence(int blockIndex, int maxRooms)
    {
        string[] sequence = new string[maxRooms];
        for (int i = 0; i < maxRooms; i++)
        {
            sequence[i] = UnityEngine.Random.value < SpecialRoomChance
                ? SpecialRoomSceneName
                : NormalRoomSceneName;
        }
        return sequence;
    }
}
