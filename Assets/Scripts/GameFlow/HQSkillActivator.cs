using UnityEngine;

/// <summary>
/// 總部技能解鎖觸發器基底類別 (Data-Driven Activator)
/// 負責將解鎖技能與 GameDB 數據狀態綁定，解耦具體的場景物件。
/// </summary>
public class HQSkillActivator : MonoBehaviour
{
    [Header("要解鎖的技能資料")]
    [Tooltip("將 Project 中的 SkillData 拖曳至此以與資料狀態對齊")]
    [SerializeField] protected SkillData skillToUnlock;

    public SkillData SkillToUnlock => skillToUnlock;

    /// <summary>
    /// 資料驅動解鎖：將技能解鎖狀態記錄至 GameDB
    /// </summary>
    public virtual void UnlockSkill()
    {
        if (skillToUnlock == null)
        {
            Debug.LogWarning("[HQSkillActivator] 尚未指定 skillToUnlock，無法執行解鎖資料寫入。");
            return;
        }

        if (GameDB.Instance != null && GameDB.Instance.Campaign != null)
        {
            GameDB.Instance.Campaign.UnlockSkill(skillToUnlock);
        }
        else
        {
            Debug.LogError("[HQSkillActivator] 找不到 GameDB 或 Campaign 資料，解鎖失敗！");
        }
    }
}
