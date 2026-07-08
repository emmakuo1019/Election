using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 總部 (HQ) 技能選擇 UI，負責將玩家選擇的技能裝備至 GameDB 跨場景資料庫中。
/// 符合單一職責原則 (SRP)，純粹處理 UI 事件與資料寫入。
/// </summary>
public class HQSkillSelectionUI : MonoBehaviour
{
    [Header("UI 參考")]
    [Tooltip("用於顯示當前裝備技能的文字 (可選)")]
    public Text equippedSkillText;

    private void Start()
    {
        RefreshUI();
    }

    /// <summary>
    /// 提供給 UI Button 的 OnClick 事件綁定。
    /// 請在 Inspector 的 Button OnClick 中新增事件，拖曳此腳本所在的物件，
    /// 選擇 HQSkillSelectionUI.OnSelectSkillButtonClicked，並將對應的 SkillData 拖入參數格中。
    /// </summary>
    /// <param name="skillToEquip">要裝備的技能資料</param>
    public void OnSelectSkillButtonClicked(SkillData skillToEquip)
    {
        if (skillToEquip == null)
        {
            Debug.LogWarning("[HQSkillSelectionUI] 嘗試裝備空的技能資料！");
            return;
        }

        // 將資料寫入 GameDB 跨場景大腦
        if (GameDB.Instance != null && GameDB.Instance.Player != null)
        {
            GameDB.Instance.Player.EquipBaseSkillJ(skillToEquip);
            Debug.Log($"[HQSkillSelectionUI] 已成功裝備技能：{skillToEquip.skillName} (已寫入 GameDB)");
            RefreshUI();
        }
        else
        {
            Debug.LogError("[HQSkillSelectionUI] 找不到 GameDB 實例，裝備失敗！請確認場景中是否有初始化 GameDB。");
        }
    }

    /// <summary>
    /// 更新畫面上的文字顯示
    /// </summary>
    private void RefreshUI()
    {
        if (equippedSkillText != null && GameDB.Instance != null && GameDB.Instance.Player != null)
        {
            SkillData currentSkill = GameDB.Instance.Player.BaseSkillJ;
            equippedSkillText.text = currentSkill != null 
                ? $"已裝備技能：{currentSkill.skillName}" 
                : "尚未裝備技能";
        }
    }
}
