using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

public class HQState : IState
{
    private string hqSceneName = "headquarters";
    
    private enum HQPhase { CandidateSelect, SkillSelect }
    private HQPhase currentPhase = HQPhase.CandidateSelect;
    private bool isMaleSelected = true;
    private int selectedSkillIndex = 0;
    private bool isTransitioning = false;

    public void Enter()
    {
        Debug.Log("[HQState] Enter - 進入總部 (純鍵盤流程)");
        
        currentPhase = HQPhase.CandidateSelect;
        isMaleSelected = true;
        selectedSkillIndex = 0;
        isTransitioning = false;

        // 啟動非同步場景載入
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartCoroutine(LoadHQSceneRoutine());
        }
    }
    
    private PlayerController hqPlayer;

    private IEnumerator LoadHQSceneRoutine()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(hqSceneName);
        if (asyncLoad != null)
        {
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            
            if (UIManager.Instance != null) 
            {
                UIManager.Instance.ShowHQPanel();
                UIManager.Instance.ShowCandidateHint();
            }
            Debug.Log($"[HQState] 場景 {hqSceneName} 載入完成！");

            // 阻斷玩家操作：尋找場景中的 PlayerController 並將其停用
            // 使用 FindAnyObjectByType 是 Unity 6 的標準寫法
            hqPlayer = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (hqPlayer != null)
            {
                hqPlayer.enabled = false;
                Debug.Log("[HQState] 已成功停用 PlayerController，阻斷總部內的移動與攻擊。");
            }
        }
        else
        {
            Debug.LogWarning($"[HQState] 找不到場景 {hqSceneName}，請確認是否加入 Build Settings。");
        }
    }

    public void Exit()
    {
        Debug.Log("[HQState] Exit");
        if (UIManager.Instance != null) UIManager.Instance.HideHQPanel();

        // 恢復玩家操作：當離開總部 (例如進入 GameplayState) 時重新啟用
        if (hqPlayer != null)
        {
            hqPlayer.enabled = true;
            hqPlayer = null;
            Debug.Log("[HQState] 已恢復 PlayerController 操作。");
        }
    }
    
    public void Update() 
    { 
        // 若正在轉場黑畫面，或抓不到鍵盤，則不處理任何輸入
        if (isTransitioning || Keyboard.current == null) return;

        switch (currentPhase)
        {
            case HQPhase.CandidateSelect:
                // [A/D] 切換角色
                if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
                {
                    isMaleSelected = !isMaleSelected;
                    if (HQSceneController.Instance != null)
                    {
                        if (isMaleSelected) HQSceneController.Instance.FocusMale();
                        else HQSceneController.Instance.FocusFemale();
                    }
                }
                // [Enter] 確定角色 -> 進入選技能階段
                else if (Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    currentPhase = HQPhase.SkillSelect;
                    if (HQSceneController.Instance != null) HQSceneController.Instance.FocusDesk();
                    if (UIManager.Instance != null) UIManager.Instance.ShowSkillHint();
                }
                // [Esc] 取消 -> 回主選單
                else if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    isTransitioning = true;
                    if (UIManager.Instance != null && GameFlowManager.Instance != null)
                    {
                        UIManager.Instance.FadeOut(1.0f, () => GameFlowManager.Instance.ChangeState(new MainMenuState()));
                    }
                }
                break;

            case HQPhase.SkillSelect:
                // [W/S] 切換技能
                if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
                {
                    if (HQSceneController.Instance != null && HQSceneController.Instance.availableSkills != null && HQSceneController.Instance.availableSkills.Length > 0)
                    {
                        selectedSkillIndex = (selectedSkillIndex + 1) % HQSceneController.Instance.availableSkills.Length;
                        Debug.Log($"[HQState] 切換至技能：{HQSceneController.Instance.availableSkills[selectedSkillIndex].skillName}");
                        // 註：若畫面上有文字提示，可在此處通知 UIManager 更新技能名稱
                    }
                }
                // [Esc] 返回 -> 退回選角階段
                else if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    currentPhase = HQPhase.CandidateSelect;
                    if (HQSceneController.Instance != null) 
                    {
                        if (isMaleSelected) HQSceneController.Instance.FocusMale();
                        else HQSceneController.Instance.FocusFemale();
                    }
                    if (UIManager.Instance != null) UIManager.Instance.ShowCandidateHint();
                }
                // [Enter] 確認裝備技能並出發 -> 進入戰鬥關卡
                else if (Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    isTransitioning = true;

                    // 1. 裝備選擇的技能並寫入 GameDB
                    if (HQSceneController.Instance != null && 
                        HQSceneController.Instance.availableSkills != null && 
                        HQSceneController.Instance.availableSkills.Length > selectedSkillIndex)
                    {
                        SkillData selectedSkill = HQSceneController.Instance.availableSkills[selectedSkillIndex];
                        if (GameDB.Instance != null && GameDB.Instance.Player != null)
                        {
                            GameDB.Instance.Player.EquipBaseSkillJ(selectedSkill);
                            Debug.Log($"[HQState] 已成功裝備技能：{selectedSkill.skillName}，並寫入 GameDB！");
                        }
                    }

                    // 2. 黑畫面漸變出發
                    if (UIManager.Instance != null && GameFlowManager.Instance != null)
                    {
                        UIManager.Instance.FadeOut(1.0f, () => GameFlowManager.Instance.ChangeState(new GameplayState(1)));
                    }
                }
                break;
        }
    }

    public void PhysicsUpdate() { }
}
