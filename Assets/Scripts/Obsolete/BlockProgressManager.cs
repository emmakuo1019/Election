using UnityEngine;

/// <summary>
/// 管理單次 Block (一組關卡房間) 的推進邏輯。
/// 所有狀態存放於 GameDB.Campaign (DontDestroyOnLoad)，不再使用 PlayerPrefs，
/// 遊戲崩潰時不會留下殘留狀態。
/// </summary>
public static class BlockProgressManager
{
    private const int DefaultBlockRoomCount = 5;
    private const string NormalRoomSceneName = "TestMVP";
    private const string SpecialRoomSceneName = "TestSpecial";
    private const string MapSceneName = "MapScene";
    private const float SpecialRoomChance = 0.2f;

    // ── 便捷存取 ─────────────────────────────────────────────────────

    private static CampaignData Campaign => GameDB.Instance?.Campaign;

    // ── 初始化與啟動 ─────────────────────────────────────────────────

    public static void InitBlock(int maxRooms, int blockIndex)
    {
        Campaign?.InitBlock(maxRooms, blockIndex);
    }

    /// <summary>
    /// 啟動指定 Block，生成房間序列並進入第一間房，回傳第一間房的場景名稱。
    /// </summary>
    public static string StartRandomBlock(int blockIndex, int maxRooms = DefaultBlockRoomCount)
    {
        if (Campaign == null)
        {
            Debug.LogError("[BlockProgressManager] GameDB.Instance 尚未初始化！");
            return NormalRoomSceneName;
        }

        int safeBlockIndex = Mathf.Clamp(blockIndex, 1, CampaignProgressManager.GetTotalBlockCount());
        int safeMaxRooms   = Mathf.Max(1, maxRooms);

        Campaign.InitBlock(safeMaxRooms, safeBlockIndex);
        Campaign.SetRoomSequence(GenerateRoomSequence(safeBlockIndex, safeMaxRooms));
        Campaign.EnterNextRoom();

        return GetCurrentRoomSceneName();
    }

    /// <summary>
    /// 依照戰役進度，啟動下一個 Block。
    /// </summary>
    public static string StartNextCampaignBlock(int maxRooms = DefaultBlockRoomCount)
    {
        int nextBlockIndex = CampaignProgressManager.GetNextBlockIndex();
        return StartRandomBlock(nextBlockIndex, maxRooms);
    }

    // ── 推進 ─────────────────────────────────────────────────────────

    public static void EnterNextRoom()
    {
        Campaign?.EnterNextRoom();
    }

    /// <summary>
    /// 前進到下一間房並回傳場景名稱；若已是最後一房則回傳 null。
    /// </summary>
    public static string AdvanceToNextRoom()
    {
        if (Campaign == null || !Campaign.HasBlockProgress() || Campaign.IsLastRoomInBlock())
            return null;

        Campaign.EnterNextRoom();
        return GetCurrentRoomSceneName();
    }

    // ── 完成 / 失敗 ──────────────────────────────────────────────────

    public static bool TryCompleteCurrentBlock()
    {
        if (Campaign == null || !Campaign.HasBlockProgress() || !Campaign.IsLastRoomInBlock())
            return false;

        CampaignProgressManager.AddCompletedBlock();
        if (GameDB.Instance?.Run != null)
            GameDB.Instance.Run.HasPendingSkillSelection = true;
        ClearBlockProgress();
        return true;
    }

    public static void FailCurrentBlock()
    {
        ClearBlockProgress();
    }

    public static void ClearBlockProgress()
    {
        Campaign?.ClearBlockProgress();
    }

    // ── 場景覆蓋 ─────────────────────────────────────────────────────

    public static void SetNextSceneOverride(string sceneName)
    {
        Campaign?.SetNextSceneOverride(sceneName);
    }

    /// <summary>
    /// 根據目前進度決定離開房間後要載入哪個場景。
    /// 若有場景覆蓋則優先使用（一次性消耗）。
    /// </summary>
    public static string GetSceneAfterRoomExit()
    {
        if (Campaign == null) return null;

        string overrideScene = Campaign.ConsumeNextSceneOverride();
        if (!string.IsNullOrWhiteSpace(overrideScene))
            return overrideScene;

        if (!Campaign.HasBlockProgress()) return null;

        if (Campaign.IsLastRoomInBlock())
        {
            bool completed = TryCompleteCurrentBlock();
            return completed ? MapSceneName : null;
        }

        return AdvanceToNextRoom();
    }

    // ── 查詢 ─────────────────────────────────────────────────────────

    public static bool HasBlockProgress()       => Campaign?.HasBlockProgress() ?? false;
    public static bool IsLastRoomInBlock()      => Campaign?.IsLastRoomInBlock() ?? false;
    public static int  GetCurrentRoomCount()    => Campaign?.CurrentRoomCount ?? 0;
    public static int  GetMaxRooms()            => Campaign?.MaxRoomsInBlock ?? DefaultBlockRoomCount;
    public static int  GetCurrentBlockIndex()   => Campaign?.CurrentBlockIndex ?? 1;
    public static string GetCurrentRoomSceneName() => Campaign?.GetCurrentRoomSceneName() ?? NormalRoomSceneName;

    // ── 內部輔助 ─────────────────────────────────────────────────────

    private static string[] GenerateRoomSequence(int blockIndex, int maxRooms)
    {
        string[] sequence = new string[maxRooms];
        for (int i = 0; i < maxRooms; i++)
        {
            sequence[i] = Random.value < SpecialRoomChance
                ? SpecialRoomSceneName
                : NormalRoomSceneName;
        }
        return sequence;
    }
}
