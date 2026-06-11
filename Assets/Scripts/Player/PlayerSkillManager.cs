using UnityEngine;
using System.Collections.Generic;

public class PlayerSkillManager : MonoBehaviour
{
    private const string PendingMapSkillSelectionKey = "PendingMapSkillSelection";

    #region 技能槽區 (Skill Slots)
    [Header("一般戰鬥技能 (J, K)")]
    public SkillData baseSkillJ;
    public SkillData skillK;

    [Header("政黨大招 (L)")]
    [SerializeField] private SkillData currentPartySkill;
    
    // 供跨場景讀取的靜態變數
    private static SkillData equippedPartySkill;
    #endregion

    #region 屬性區 (Properties)
    public bool HasPartySkill => currentPartySkill != null;
    public SkillData CurrentPartySkill => currentPartySkill;
    public static bool HasEquippedPartySkill => equippedPartySkill != null;
    public static SkillData EquippedPartySkill => equippedPartySkill;
    #endregion

    #region 舊有攻擊保留區 (Legacy Attack)
    // 注意：目前一般攻擊已由 AttackState 與 PlayerAttack 處理，這裡僅暫作保留防編譯錯誤
    [Header("一般攻擊 (舊有保留)")]
    [SerializeField] private PlayerAttack speechAttack;

    public void UseSpeech()
    {
        if (speechAttack == null)
        {
            Debug.LogWarning("⚠️ speechAttack 未設定");
            return;
        }
        speechAttack.PerformAttack(Vector3.zero);
    }
    
    public void UnlockSpeech() { }
    #endregion

    #region 生命週期 (Lifecycle)
    private void Awake()
    {
        // 確保場景載入時，能正確讀取跨場景的裝備技能
        // 依照您的需求，將選單選擇的技能改為裝備到 J 鍵 (baseSkillJ)
        if (baseSkillJ == null && equippedPartySkill != null)
        {
            baseSkillJ = equippedPartySkill;
            Debug.Log($"[PlayerSkillManager] 場景載入：已將跨場景技能 {equippedPartySkill.skillName} 裝備至 J 鍵");
        }
    }
    #endregion

    #region CD 管理區 (Cooldown Management)
    // 儲存各個技能的上次施放時間，用來計算冷卻
    private Dictionary<SkillData, float> skillLastUseTime = new Dictionary<SkillData, float>();

    /// <summary>
    /// 檢查傳入的戰鬥技能或大招是否可以施放
    /// </summary>
    public bool CanCastSkill(SkillData skillData)
    {
        if (skillData == null) return false;

        // 注意：統一為 SkillData 後，資源扣除與條件檢查建議未來實作於 SkillData 內部
        // 這裡負責純粹的冷卻時間計算
        if (skillLastUseTime.TryGetValue(skillData, out float lastTime))
        {
            if (Time.time < lastTime + skillData.Cooldown)
            {
                Debug.LogWarning($"⏳ 技能 [{skillData.AnimationTriggerName}] 冷卻中...");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 紀錄技能施放時間以進入冷卻
    /// </summary>
    public void RecordSkillUse(SkillData skillData)
    {
        if (skillData == null) return;
        skillLastUseTime[skillData] = Time.time;
    }
    #endregion

    #region 裝備管理區 (Equipment Management)
    // 讓 UI 可以直接將技能裝備到 J 鍵，並保存至靜態變數以供跨場景讀取
    public void EquipSkillJ(SkillData skillData)
    {
        baseSkillJ = skillData;
        equippedPartySkill = skillData; // 同步儲存至跨場景變數
        Debug.Log($"[PlayerSkillManager] 已將 {skillData.skillName} 裝備至 J 鍵 (並寫入跨場景靜態變數)");
    }

    public void EquipPartySkill(SkillData skillData)
    {
        SetEquippedPartySkill(skillData);
        currentPartySkill = equippedPartySkill;
    }

    public void ClearPartySkill()
    {
        currentPartySkill = null;
        equippedPartySkill = null;
    }

    public static void SetEquippedPartySkill(SkillData skillData)
    {
        equippedPartySkill = skillData;
        ClearPendingMapSkillSelection();
    }

    public static void ResetSavedPartySkill()
    {
        equippedPartySkill = null;
        PlayerPrefs.DeleteKey(PendingMapSkillSelectionKey);
        PlayerPrefs.Save();
    }

    public static void MarkPendingMapSkillSelection()
    {
        PlayerPrefs.SetInt(PendingMapSkillSelectionKey, 1);
        PlayerPrefs.Save();
    }

    public static bool HasPendingMapSkillSelection() =>
        PlayerPrefs.GetInt(PendingMapSkillSelectionKey, 0) == 1;

    public static void ClearPendingMapSkillSelection()
    {
        PlayerPrefs.DeleteKey(PendingMapSkillSelectionKey);
        PlayerPrefs.Save();
    }
    #endregion
}
