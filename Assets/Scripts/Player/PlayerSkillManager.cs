using UnityEngine;
using System.Collections.Generic;

public class PlayerSkillManager : MonoBehaviour
{
    #region 技能槽區 (Skill Slots)
    [Header("一般戰鬥技能 (J, K)")]
    public SkillData baseSkillJ;
    public SkillData skillK;

    [Header("政黨大招 (L)")]
    [SerializeField] private SkillData currentPartySkill;
    
    // (已移除舊的靜態變數，全面改用 GameDB 進行跨場景存取)
    #endregion

    #region 屬性區 (Properties)
    public bool HasPartySkill => currentPartySkill != null;
    public SkillData CurrentPartySkill => currentPartySkill;
    #endregion

    #region 舊有攻擊保留區 (Legacy Attack)
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
        // 確保場景載入時，從 GameDB 讀取跨場景的裝備技能
        if (GameDB.Instance != null && GameDB.Instance.Player != null)
        {
            if (GameDB.Instance.Player.BaseSkillJ != null)
            {
                baseSkillJ = GameDB.Instance.Player.BaseSkillJ;
                Debug.Log($"[PlayerSkillManager] 場景載入：已從 GameDB 讀取技能 {baseSkillJ.skillName} 並裝備至 J 鍵");
            }
            if (GameDB.Instance.Player.EquippedPartySkill != null)
            {
                currentPartySkill = GameDB.Instance.Player.EquippedPartySkill;
            }
        }
    }

    private void Start()
    {
        // 讀取 GameDB.Instance.Campaign.UnlockedSkills 並自動掛載已解鎖的技能 (資料驅動)
        if (GameDB.Instance != null && GameDB.Instance.Campaign != null)
        {
            foreach (var skill in GameDB.Instance.Campaign.UnlockedSkills)
            {
                if (skill != null)
                {
                    // 若已解鎖了悲情土下座 (DogezaSkill/DogezaSkillData)，且目前未裝備任何戰鬥技能，則自動掛載
                    if (skill is DogezaSkill || skill is DogezaSkillData || skill.skillName == "悲情土下座")
                    {
                        if (baseSkillJ == null)
                        {
                            EquipSkillJ(skill);
                            Debug.Log($"[PlayerSkillManager] 偵測到已解鎖技能 {skill.skillName}，自動掛載至 J 鍵");
                        }
                    }
                }
            }
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
    public void EquipSkillJ(SkillData skillData)
    {
        baseSkillJ = skillData;
        if (GameDB.Instance != null && GameDB.Instance.Player != null)
        {
            GameDB.Instance.Player.EquipBaseSkillJ(skillData);
        }
        Debug.Log($"[PlayerSkillManager] 已將 {skillData.skillName} 裝備至 J 鍵 (並寫入 GameDB 跨場景靜態變數)");
    }

    public void EquipPartySkill(SkillData skillData)
    {
        currentPartySkill = skillData;
        if (GameDB.Instance != null && GameDB.Instance.Player != null)
        {
            GameDB.Instance.Player.EquipPartySkill(skillData);
        }
    }

    public void ClearPartySkill()
    {
        currentPartySkill = null;
        if (GameDB.Instance != null && GameDB.Instance.Player != null)
        {
            GameDB.Instance.Player.EquipPartySkill(null);
        }
    }

    // 改用 GameDB.Instance.Run.HasPendingSkillSelection 進行跨場景存取，消滅 PlayerPrefs
    public static void MarkPendingMapSkillSelection()
    {
        if (GameDB.Instance != null && GameDB.Instance.Run != null)
        {
            GameDB.Instance.Run.HasPendingSkillSelection = true;
        }
    }

    public static bool HasPendingMapSkillSelection()
    {
        return GameDB.Instance != null && GameDB.Instance.Run != null && GameDB.Instance.Run.HasPendingSkillSelection;
    }

    public static void ClearPendingMapSkillSelection()
    {
        if (GameDB.Instance != null && GameDB.Instance.Run != null)
        {
            GameDB.Instance.Run.HasPendingSkillSelection = false;
        }
    }
    #endregion
}
