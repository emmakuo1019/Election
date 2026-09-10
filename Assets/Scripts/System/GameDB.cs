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
    [SerializeField] private MissionPool _missionPool;
    public MissionPool MissionPool => _missionPool;

    [Tooltip("教學關卡使用的任務資料（設 HUDLayout = Tutorial 即可隱藏不需要的 HUD）")]
    [SerializeField] private RoomMissionData _tutorialMission;
    public RoomMissionData TutorialMission => _tutorialMission;

    [Header("戰役定義")]
    [SerializeField] private CampaignDefinition _campaignDefinition;
    public CampaignDefinition CampaignDefinition => _campaignDefinition;
    
    public PlayerData Player { get; private set; }
    public RunData Run { get; private set; }
    public CampaignData Campaign { get; private set; }
    public event Action<CampaignData> OnCampaignChanged;

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
        Campaign = new CampaignData(_campaignDefinition, _missionPool);
    }

    /// <summary>
    /// 重新開始遊戲時，重置單局資料 (RunData)。
    /// 通常在離開結算畫面、回到主選單，或新局開始時呼叫。
    /// </summary>
    public void ResetRunData()
    {
        // 清除舊 Run 的觸發器訂閱（若 Manager 還在場景中）
        PolicyEffectRuntimeManager.Instance?.ClearAllTriggers();

        Run = new RunData();
        Debug.Log("[GameDB] RunData 已重置");
    }

    /// <summary>
    /// 重置戰役進度 (CampaignData)，回到遊戲最開始的狀態。
    /// 通常在回到主選單 / 總部場景時呼叫。
    /// </summary>
    public void ResetCampaignData()
    {
        Campaign = new CampaignData(_campaignDefinition, _missionPool);
        OnCampaignChanged?.Invoke(Campaign);
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
    private readonly List<SkillData> _unlockedSkills = new List<SkillData>();
    public IReadOnlyList<SkillData> UnlockedSkills => _unlockedSkills;

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

    /// <summary>技能解鎖屬於玩家資料，不屬於每局會重置的 CampaignData。</summary>
    public bool UnlockSkill(SkillData skill)
    {
        if (skill == null || _unlockedSkills.Contains(skill)) return false;
        _unlockedSkills.Add(skill);
        return true;
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

            // 套用永久被動效果（有 trigger 的卡在 ApplyAllEffects 內只套氛圍）
            card.ApplyAllEffects();

            // 有觸發條件的卡 → 向 Bridge 註冊事件訂閱
            if (card.trigger != null)
                PolicyEffectRuntimeManager.Instance?.RegisterCard(card);

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
    /// 失勢獎勵只提供普通卡，且在可行時保證至少一張通用卡。
    /// 這是可恢復的 Build 壓力，不是隨機塞入負面效果的隱性死刑。
    /// </summary>
    public List<PolicyCardData> DrawDistressPolicyCards(int count)
    {
        var available = new List<PolicyCardData>();
        foreach (PolicyCardData card in GameDB.Instance.allPolicyCards)
        {
            if (card != null && card.Rarity == CardRarity.Common && !AcquiredPolicyCards.Contains(card.cardName))
                available.Add(card);
        }

        var result = new List<PolicyCardData>();
        PolicyCardData neutral = available.Find(card => card.faction == null);
        if (neutral != null)
        {
            result.Add(neutral);
            available.Remove(neutral);
        }

        while (result.Count < count && available.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, available.Count);
            result.Add(available[index]);
            available.RemoveAt(index);
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
    
    // ── 即時票數統計（基於場上選民）────────────────────────────────
    
    /// <summary>取得場上玩家陣營的選民數量（即時統計）</summary>
    public int GetCurrentPlayerVotes()
    {
        int count = 0;
        foreach (var voter in VoterLogic.GetAllActiveVoters())
        {
            if (voter != null && voter.Data != null && 
                voter.Data.ConvertedSide == VoterData.PlayerSideSign)
            {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>取得場上對手陣營的選民數量（即時統計）</summary>
    public int GetCurrentOpponentVotes()
    {
        int count = 0;
        foreach (var voter in VoterLogic.GetAllActiveVoters())
        {
            if (voter != null && voter.Data != null && 
                voter.Data.ConvertedSide == VoterData.EnemySideSign)
            {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>取得場上總選民數量（即時統計），包含所有選民（已轉化 + 中立 + 深色選民）</summary>
    public int GetCurrentTotalVotes()
    {
        int count = 0;
        foreach (var voter in VoterLogic.GetAllActiveVoters())
        {
            if (voter != null && voter.Data != null)
            {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>取得玩家當前得票率（即時統計，基於場上所有選民，包含深色選民和中立選民）</summary>
    public float GetCurrentVotePercentage()
    {
        int total = GetCurrentTotalVotes();
        return total > 0 ? (float)GetCurrentPlayerVotes() / total : 0f;
    }
    
    // ──────────────────────────────────────────────────────────────
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

/// <summary>一個待選或已選的任務，場景由 CampaignDefinition 統一驗證。</summary>
[System.Serializable]
public struct RoomOption
{
    public string sceneName;
    public RoomMissionData mission;

    public RoomOption(string sceneName, RoomMissionData mission)
    {
        this.sceneName = sceneName;
        this.mission = mission;
    }
}

[System.Serializable]
public class CampaignData
{
    private const string DefaultArenaScene = "TestMVP";
    private readonly CampaignDefinition _definition;
    private readonly MissionPool _missionPool;

    public int Seed { get; private set; }
    public int CurrentNodeNumber { get; private set; }
    public EncounterNodeRole CurrentRole { get; private set; }
    public RoomOption ActiveRoom { get; private set; }
    public RoomOption[] PendingOptions { get; private set; } = System.Array.Empty<RoomOption>();
    public List<EncounterResult> Results { get; } = new List<EncounterResult>();
    public bool IsTutorialActive { get; private set; }
    public event Action OnProgressChanged;
    private int _resolvedNodeNumber = -1;

    public CampaignData(CampaignDefinition definition, MissionPool missionPool)
    {
        _definition = definition;
        _missionPool = missionPool;
        Seed = definition != null ? definition.DefaultSeed : 0;
    }

    public bool StartTutorial(RoomMissionData fallbackTutorialMission)
    {
        RoomMissionData mission = _definition != null && _definition.TutorialMission != null
            ? _definition.TutorialMission
            : fallbackTutorialMission;
        if (mission == null) return false;

        IsTutorialActive = true;
        CurrentNodeNumber = 0;
        CurrentRole = EncounterNodeRole.Opening;
        ActiveRoom = new RoomOption(ResolveScene(mission), mission);
        PendingOptions = System.Array.Empty<RoomOption>();
        _resolvedNodeNumber = -1;
        NotifyProgressChanged();
        return true;
    }

    public bool StartFormalCampaign()
    {
        string error = _definition == null ? "缺少 CampaignDefinition。" : string.Empty;
        if (_definition == null || !_definition.IsValid(out error))
        {
            Debug.LogError($"[CampaignData] 無法啟動戰役：{error}");
            return false;
        }

        if (!HasValidMissionChoices(out error))
        {
            Debug.LogError($"[CampaignData] 無法啟動戰役：{error}");
            return false;
        }

        Results.Clear();
        PendingOptions = System.Array.Empty<RoomOption>();
        IsTutorialActive = false;
        _resolvedNodeNumber = -1;
        return ActivateFixedNode(1);
    }

    public void ResolveCurrentEncounter(EncounterOutcome outcome)
    {
        Debug.Log($"[CampaignData] ResolveCurrentEncounter 被調用：CurrentNodeNumber={CurrentNodeNumber}, _resolvedNodeNumber={_resolvedNodeNumber}, IsTutorialActive={IsTutorialActive}, outcome={outcome}");
        
        if (IsTutorialActive || CurrentNodeNumber <= 0 || _resolvedNodeNumber == CurrentNodeNumber)
        {
            Debug.Log($"[CampaignData] 跳過結算：IsTutorialActive={IsTutorialActive}, CurrentNodeNumber={CurrentNodeNumber}, _resolvedNodeNumber={_resolvedNodeNumber}");
            return;
        }
        
        Results.Add(new EncounterResult(CurrentNodeNumber, ActiveRoom.mission, outcome));
        _resolvedNodeNumber = CurrentNodeNumber;
        Debug.Log($"[CampaignData] ✓ 已結算節點 {CurrentNodeNumber}，_resolvedNodeNumber 已更新為 {_resolvedNodeNumber}");
    }

    public bool TryPrepareNextStep(out bool needsRouteChoice, out bool isRunComplete)
    {
        needsRouteChoice = false;
        isRunComplete = false;

        Debug.Log($"[CampaignData] TryPrepareNextStep 被調用：CurrentNodeNumber={CurrentNodeNumber}, _resolvedNodeNumber={_resolvedNodeNumber}, IsTutorialActive={IsTutorialActive}");

        if (IsTutorialActive)
        {
            IsTutorialActive = false;
            return StartFormalCampaign();
        }

        if (_resolvedNodeNumber != CurrentNodeNumber)
        {
            Debug.LogError($"[CampaignData] 任務尚未結算，不能推進正式節點。CurrentNodeNumber={CurrentNodeNumber}, _resolvedNodeNumber={_resolvedNodeNumber}");
            return false;
        }

        if (CurrentNodeNumber >= CampaignDefinition.FormalNodeCount)
        {
            isRunComplete = true;
            return true;
        }

        int nextNode = CurrentNodeNumber + 1;
        CampaignNodeDefinition next = _definition.GetNode(nextNode);
        if (next == null)
        {
            Debug.LogError($"[CampaignData] 缺少第 {nextNode} 節點定義。");
            return false;
        }

        Debug.Log($"[CampaignData] 準備進入節點 {nextNode}，角色：{next.role}");

        if (next.role == EncounterNodeRole.MissionChoice)
        {
            // UI 重載、雙門重進 Trigger 都只能讀到同一組已存選項，絕不能重新抽取。
            if (PendingOptions.Length == 2)
            {
                Debug.Log($"[CampaignData] 已有待選路線，直接返回");
                needsRouteChoice = true;
                return true;
            }

            PendingOptions = BuildOffers(nextNode);
            needsRouteChoice = PendingOptions.Length == 2;
            Debug.Log($"[CampaignData] 已生成 {PendingOptions.Length} 個路線選項，needsRouteChoice={needsRouteChoice}");
            return needsRouteChoice;
        }

        return ActivateFixedNode(nextNode);
    }

    public bool TrySelectRoute(int optionIndex)
    {
        if (PendingOptions.Length != 2 || optionIndex < 0 || optionIndex >= PendingOptions.Length)
            return false;

        ActiveRoom = PendingOptions[optionIndex];
        PendingOptions = System.Array.Empty<RoomOption>();
        CurrentNodeNumber++;
        CurrentRole = EncounterNodeRole.MissionChoice;
        _resolvedNodeNumber = -1;
        NotifyProgressChanged();
        return true;
    }

    public string GetCurrentRoomSceneName() => ActiveRoom.sceneName;

    public CampaignNodeDefinition GetCurrentNode()
    {
        return CurrentNodeNumber > 0 ? _definition?.GetNode(CurrentNodeNumber) : null;
    }

    private bool ActivateFixedNode(int nodeNumber)
    {
        CampaignNodeDefinition node = _definition.GetNode(nodeNumber);
        if (node == null) return false;

        CurrentNodeNumber = nodeNumber;
        CurrentRole = node.role;
        PendingOptions = System.Array.Empty<RoomOption>();
        _resolvedNodeNumber = -1;
        ActiveRoom = new RoomOption(
            !string.IsNullOrWhiteSpace(node.sceneName) ? node.sceneName : ResolveScene(node.fixedMission),
            node.fixedMission);
        NotifyProgressChanged();
        return !string.IsNullOrWhiteSpace(ActiveRoom.sceneName);
    }

    private RoomOption[] BuildOffers(int nextNode)
    {
        if (_missionPool == null) return System.Array.Empty<RoomOption>();
        RoomMissionData[] missions = _missionPool.DrawEligible(2, nextNode, Seed + nextNode, Results);
        if (missions.Length != 2 || missions[0] == null || missions[1] == null) return System.Array.Empty<RoomOption>();
        return new[] { new RoomOption(ResolveScene(missions[0]), missions[0]), new RoomOption(ResolveScene(missions[1]), missions[1]) };
    }

    private void NotifyProgressChanged() => OnProgressChanged?.Invoke();

    private static string ResolveScene(RoomMissionData mission)
    {
        return mission != null && !string.IsNullOrWhiteSpace(mission.sceneName)
            ? mission.sceneName
            : DefaultArenaScene;
    }

    private bool HasValidMissionChoices(out string error)
    {
        if (_missionPool == null || _missionPool.entries == null)
        {
            error = "缺少 MissionPool 或任務列表。";
            return false;
        }

        foreach (int nodeNumber in new[] { 2, 3, 5, 6, 7 })
        {
            int eligibleCount = 0;
            foreach (MissionPool.Entry entry in _missionPool.entries)
            {
                if (entry.mission != null && entry.weight > 0 && entry.mission.IsEligibleForNode(nodeNumber))
                    eligibleCount++;
            }

            if (eligibleCount < 2)
            {
                error = $"第 {nodeNumber} 節點至少需要 2 個有效任務，目前只有 {eligibleCount} 個。";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }
}
