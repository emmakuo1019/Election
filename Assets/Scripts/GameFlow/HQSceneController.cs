using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 總部場景控制器。
///
/// 流程：
///   Step.Candidate  ─ 進場預設鏡頭對準候選人，Space/Enter/J 確認後進入 Step.Faction
///   Step.Faction    ─ A/D 切換各派系大佬鏡頭，Space/Enter/J 確認派系並出發
///
/// 場景設定：
///   vcamCandidate   ─ 對準男候選人的鏡頭（進場預設）
///   vcamFactions[]  ─ 每個派系一顆鏡頭，依序對應 factions[]
/// </summary>
public class HQSceneController : MonoBehaviour
{
    public static HQSceneController Instance { get; private set; }

    // ── 候選人鏡頭 ────────────────────────────────────────────────────
    [Header("候選人鏡頭（進場預設）")]
    [Tooltip("對準男候選人的 CinemachineCamera，場景一載入就啟用")]
    [SerializeField] private CinemachineCamera vcamCandidate;

    // ── 派系鏡頭 ──────────────────────────────────────────────────────
    [Header("派系鏡頭（與 factions 陣列一一對應）")]
    [Tooltip("每個派系一顆 CinemachineCamera，對準沙發上各自的大佬 NPC")]
    [SerializeField] private CinemachineCamera[] vcamFactions;

    // ── 派系資料 ──────────────────────────────────────────────────────
    [Header("可選派系（依序對應 vcamFactions）")]
    public FactionData[] factions;

    // ── 流程步驟 ──────────────────────────────────────────────────────
    public enum HQStep { Candidate, Faction }
    public HQStep CurrentStep { get; private set; } = HQStep.Candidate;

    // ── 內部狀態 ──────────────────────────────────────────────────────
    private int factionCursor = 0;

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        // 先把所有鏡頭壓低，再立刻啟用候選人鏡頭，
        // 讓 Cinemachine 從第一幀就接管攝影機，避免進場時從相機物理位置 blend 過來的問題。
        SetAllPriority(10);
        if (vcamCandidate != null)
            vcamCandidate.Priority.Value = 20;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── 外部進入點（由 HQState 呼叫）─────────────────────────────────

    /// <summary>
    /// 場景載入完成後，由 HQState 呼叫，正式啟動流程。
    /// 預設：鏡頭對準候選人，顯示候選人介紹面板。
    /// </summary>
    public void BeginHQFlow()
    {
        CurrentStep   = HQStep.Candidate;
        factionCursor = 0;

        // 進場直接對準候選人
        SwitchCamera(vcamCandidate);
        UIManager.Instance?.ShowHQCandidateStep();

        Debug.Log("[HQSceneController] 流程開始：Step.Candidate（候選人介紹）");
    }

    // ── 輸入回調（由 HQInputHandler 呼叫）────────────────────────────

    public void OnNavigateLeft()
    {
        // Candidate 步驟只有一位候選人，左右無效
        if (CurrentStep != HQStep.Faction) return;
        if (factions == null || factions.Length == 0) return;

        factionCursor = (factionCursor - 1 + factions.Length) % factions.Length;
        SwitchToFactionCamera(factionCursor);
        UIManager.Instance?.ShowHQFactionStep(factionCursor, factions);

        Debug.Log($"[HQSceneController] 派系游標 ← {factionCursor}: {factions[factionCursor]?.factionName}");
    }

    public void OnNavigateRight()
    {
        if (CurrentStep != HQStep.Faction) return;
        if (factions == null || factions.Length == 0) return;

        factionCursor = (factionCursor + 1) % factions.Length;
        SwitchToFactionCamera(factionCursor);
        UIManager.Instance?.ShowHQFactionStep(factionCursor, factions);

        Debug.Log($"[HQSceneController] 派系游標 → {factionCursor}: {factions[factionCursor]?.factionName}");
    }

    public void OnConfirm()
    {
        if (CurrentStep == HQStep.Candidate)
        {
            ConfirmCandidateAndGoFaction();
        }
        else if (CurrentStep == HQStep.Faction)
        {
            ConfirmFactionAndStart();
        }
    }

    // ── 內部步驟邏輯 ─────────────────────────────────────────────────

    private void ConfirmCandidateAndGoFaction()
    {
        CurrentStep   = HQStep.Faction;
        factionCursor = 0;

        // 切到第一個派系大佬的鏡頭
        SwitchToFactionCamera(factionCursor);
        UIManager.Instance?.ShowHQFactionStep(factionCursor, factions);

        Debug.Log("[HQSceneController] 確認候選人，進入 Step.Faction（選派系）");
    }

    private void ConfirmFactionAndStart()
    {
        if (factions == null || factions.Length == 0)
        {
            Debug.LogWarning("[HQSceneController] 沒有可選派系！請在 Inspector 填入 factions。");
            return;
        }

        FactionData chosen = factions[factionCursor];
        if (chosen == null)
        {
            Debug.LogWarning($"[HQSceneController] factions[{factionCursor}] 為 null！");
            return;
        }

        // 寫入 GameDB（同時自動裝備起始技能）
        GameDB.Instance?.Player.SelectFaction(chosen);

        Debug.Log($"[HQSceneController] 確認派系：{chosen.factionName}，出發！");

        UIManager.Instance?.FadeOut(1.0f, () =>
        {
            // 🔧 暫時跳過教學，直接進入第一關
            // GameFlowManager.Instance?.ChangeState(new TutorialState());
            
            if (GameDB.Instance?.Campaign.StartFormalCampaign() == true)
            {
                Debug.Log("[HQSceneController] 跳過教學，直接開始第一關戰役");
                GameFlowManager.Instance?.ChangeState(new GameplayState(GameDB.Instance.Campaign.CurrentNodeNumber));
            }
            else
            {
                Debug.LogError("[HQSceneController] 無法啟動戰役，請檢查 CampaignDefinition 和 MissionPool 配置");
            }
        });
    }

    // ── 鏡頭工具 ─────────────────────────────────────────────────────

    private void SetAllPriority(int p)
    {
        if (vcamCandidate != null) vcamCandidate.Priority.Value = p;
        if (vcamFactions == null) return;
        foreach (var cam in vcamFactions)
            if (cam != null) cam.Priority.Value = p;
    }

    private void SwitchCamera(CinemachineCamera target)
    {
        if (target == null) return;
        SetAllPriority(10);
        target.Priority.Value = 20;
    }

    private void SwitchToFactionCamera(int index)
    {
        if (vcamFactions == null || index < 0 || index >= vcamFactions.Length) return;
        SetAllPriority(10);
        if (vcamFactions[index] != null)
            vcamFactions[index].Priority.Value = 20;
    }
}
