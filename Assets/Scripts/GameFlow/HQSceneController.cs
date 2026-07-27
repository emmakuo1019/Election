using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 總部場景控制器。
/// 管理鏡頭切換 + 內部流程步驟，並通知 UIManager 顯示對應面板。
///
/// 流程：
///   Step.Candidate  ── A/D 切換男女鏡頭，S 確認進入 Step.Skill
///   Step.Skill      ── A/D 切換技能選項，S 確認技能選擇（若已選則出發）
/// </summary>
public class HQSceneController : MonoBehaviour
{
    public static HQSceneController Instance { get; private set; }

    // ── 流程步驟 ──────────────────────────────────────────────────────
    public enum HQStep { Candidate, Skill }
    public HQStep CurrentStep { get; private set; } = HQStep.Candidate;

    // ── Cinemachine 鏡頭 ──────────────────────────────────────────────
    [Header("Cinemachine 鏡頭")]
    [SerializeField] private CinemachineCamera vcamMale;
    [SerializeField] private CinemachineCamera vcamFemale;
    [SerializeField] private CinemachineCamera vcamDesk;
    [SerializeField] private CinemachineCamera[] allCameras;

    // ── 技能資料 ──────────────────────────────────────────────────────
    [Header("可選技能（依序對應選項 0, 1, 2...）")]
    public SkillData[] availableSkills;

    // ── 內部狀態 ──────────────────────────────────────────────────────
    private bool isMaleSelected = true;         // 目前游標指向男(true)或女(false)
    private int  skillCursorIndex = 0;          // 技能游標
    private bool skillConfirmed   = false;      // 是否已按 S 確認技能

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (allCameras == null || allCameras.Length == 0)
            allCameras = new CinemachineCamera[] { vcamMale, vcamFemale, vcamDesk };
    }

    private void Start()
    {
        SetAllPriority(10);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── 外部進入點（由 HQState 呼叫）─────────────────────────────────

    /// <summary>
    /// 場景載入完成後，由 HQState 呼叫，正式啟動流程。
    /// </summary>
    public void BeginHQFlow()
    {
        isMaleSelected  = true;
        skillCursorIndex = 0;
        skillConfirmed   = false;
        CurrentStep      = HQStep.Candidate;

        FocusCandidate(true);
        UIManager.Instance?.ShowHQCandidateStep(isMaleSelected);

        Debug.Log("[HQSceneController] 流程開始：Step.Candidate");
    }

    // ── 輸入回調（由 HQInputHandler 呼叫）────────────────────────────

    public void OnNavigateLeft()
    {
        if (CurrentStep == HQStep.Candidate)
        {
            // 切換到男候選人
            isMaleSelected = true;
            FocusCandidate(true);
            UIManager.Instance?.ShowHQCandidateStep(isMaleSelected);
            Debug.Log("[HQSceneController] 選角游標 ← 男");
        }
        else if (CurrentStep == HQStep.Skill)
        {
            // 技能游標往左
            if (availableSkills == null || availableSkills.Length == 0) return;
            skillCursorIndex = (skillCursorIndex - 1 + availableSkills.Length) % availableSkills.Length;
            skillConfirmed   = false; // 移動游標後重置確認狀態
            UIManager.Instance?.ShowHQSkillStep(skillCursorIndex, skillConfirmed, availableSkills);
            Debug.Log($"[HQSceneController] 技能游標 ← index {skillCursorIndex}");
        }
    }

    public void OnNavigateRight()
    {
        if (CurrentStep == HQStep.Candidate)
        {
            // 切換到女候選人
            isMaleSelected = false;
            FocusCandidate(false);
            UIManager.Instance?.ShowHQCandidateStep(isMaleSelected);
            Debug.Log("[HQSceneController] 選角游標 → 女");
        }
        else if (CurrentStep == HQStep.Skill)
        {
            if (availableSkills == null || availableSkills.Length == 0) return;
            skillCursorIndex = (skillCursorIndex + 1) % availableSkills.Length;
            skillConfirmed   = false;
            UIManager.Instance?.ShowHQSkillStep(skillCursorIndex, skillConfirmed, availableSkills);
            Debug.Log($"[HQSceneController] 技能游標 → index {skillCursorIndex}");
        }
    }

    public void OnConfirm()
    {
        if (CurrentStep == HQStep.Candidate)
        {
            ConfirmCandidateAndGoSkill();
        }
        else if (CurrentStep == HQStep.Skill)
        {
            if (!skillConfirmed)
            {
                // 第一次按 S：確認選擇這個技能
                ConfirmSkill();
            }
            else
            {
                // 已確認技能，再按 S：出發！
                TryStartGame();
            }
        }
    }

    // ── 內部步驟邏輯 ─────────────────────────────────────────────────

    private void ConfirmCandidateAndGoSkill()
    {
        // 寫入候選人選擇（目前 GameDB 沒有候選人欄位，預留擴充點）
        // GameDB.Instance?.Player.SelectGender(isMaleSelected);

        CurrentStep      = HQStep.Skill;
        skillCursorIndex = 0;
        skillConfirmed   = false;

        FocusDesk();
        UIManager.Instance?.ShowHQSkillStep(skillCursorIndex, skillConfirmed, availableSkills);

        Debug.Log($"[HQSceneController] 確認選角（{(isMaleSelected ? "男" : "女")}），進入 Step.Skill");
    }

    private void ConfirmSkill()
    {
        if (availableSkills == null || availableSkills.Length == 0)
        {
            Debug.LogWarning("[HQSceneController] 沒有可選技能！請在 Inspector 填入 availableSkills。");
            return;
        }

        SkillData chosen = availableSkills[skillCursorIndex];
        if (chosen == null)
        {
            Debug.LogWarning($"[HQSceneController] availableSkills[{skillCursorIndex}] 為 null！");
            return;
        }

        // 寫入 GameDB
        GameDB.Instance?.Player.EquipBaseSkillJ(chosen);
        skillConfirmed = true;

        UIManager.Instance?.ShowHQSkillStep(skillCursorIndex, skillConfirmed, availableSkills);

        Debug.Log($"[HQSceneController] 技能確認：{chosen.skillName}（再按 S 出發）");
    }

    private void TryStartGame()
    {
        if (GameDB.Instance?.Player?.BaseSkillJ == null)
        {
            Debug.LogWarning("[HQSceneController] 尚未選擇技能！");
            return;
        }

        Debug.Log("[HQSceneController] 出發！");
        UIManager.Instance?.FadeOut(1.0f, () =>
        {
            GameFlowManager.Instance?.ChangeState(new GameplayState(1));
        });
    }

    // ── 鏡頭工具 ─────────────────────────────────────────────────────

    private void SetAllPriority(int p)
    {
        if (allCameras == null) return;
        foreach (var cam in allCameras)
            if (cam != null) cam.Priority.Value = p;
    }

    public void SwitchCamera(CinemachineCamera target)
    {
        if (target == null || allCameras == null) return;
        SetAllPriority(10);
        target.Priority.Value = 20;
    }

    public void FocusCandidate(bool isMale) => SwitchCamera(isMale ? vcamMale : vcamFemale);
    public void FocusDesk() => SwitchCamera(vcamDesk);
}
