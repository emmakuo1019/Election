using UnityEngine;
using System;
using UnityEngine.Serialization;

public class HQSkillTrigger : HQSkillActivator
{
    // skillToUnlock 已繼承自 HQSkillActivator

    [Header("視覺回饋 (可選)")]
    [Tooltip("未被選中時的外觀 (例如原本普通的方塊)")]
    [SerializeField] private GameObject unselectedVisual;
    
    [Tooltip("被選中時的外觀 (觸發後開啟發光特效或不同顏色的模型)")]
    [FormerlySerializedAs("visualFeedback")]
    [SerializeField] private GameObject selectedVisual;

    // 靜態全域事件：當任何一個技能方塊被選擇時，通知所有其他方塊
    public static event Action<HQSkillTrigger> OnAnySkillSelected;

    private void Start()
    {
        // 遊戲一開始，確保大家都處於「未被碰過」的狀態
        SetSelectedState(false);
    }

    private void OnEnable()
    {
        OnAnySkillSelected += HandleSkillSelected;
    }

    private void OnDisable()
    {
        OnAnySkillSelected -= HandleSkillSelected;
    }

    /// <summary>
    /// 當有別的方塊被踩到時，這段會被觸發
    /// </summary>
    private void HandleSkillSelected(HQSkillTrigger selectedTrigger)
    {
        // 如果主角碰的不是我，那我就要乖乖變回「未被碰的樣子」
        if (selectedTrigger != this)
        {
            SetSelectedState(false);
        }
    }

    /// <summary>
    /// 切換視覺狀態
    /// </summary>
    private void SetSelectedState(bool isSelected)
    {
        SetVisualActive(unselectedVisual, !isSelected);
        SetVisualActive(selectedVisual, isSelected);
    }

    private void SetVisualActive(GameObject visual, bool isActive)
    {
        if (visual == null) return;

        // 【關鍵防呆】如果使用者把掛載腳本的本體 (this.gameObject) 當作視覺物件，
        // 絕對不能呼叫 SetActive(false)，否則腳本會直接死亡，碰撞器也會消失，無法接收復原事件！
        if (visual == this.gameObject)
        {
            Renderer[] renderers = visual.GetComponents<Renderer>();
            foreach (var r in renderers)
            {
                r.enabled = isActive;
            }
        }
        else
        {
            visual.SetActive(isActive);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 確認碰到方塊的是玩家
        if (other.CompareTag("Player"))
        {
            if (skillToUnlock != null && GameDB.Instance != null && GameDB.Instance.Player != null)
            {
                // 1. 呼叫基底類別解鎖技能 (資料驅動)
                UnlockSkill();

                // 2. 將該技能寫入 GameDB 進行裝備
                GameDB.Instance.Player.EquipBaseSkillJ(skillToUnlock);
                Debug.Log($"[HQSkillTrigger] 玩家撞擊方塊！已成功解鎖並裝備技能：{skillToUnlock.skillName}");

                // 3. 先把自己變成「已選擇」的外觀
                SetSelectedState(true);

                // 4. 廣播告訴全世界：「我被選了！其他人請退回原本的樣子！」
                OnAnySkillSelected?.Invoke(this);
            }
            else
            {
                Debug.LogWarning("[HQSkillTrigger] 尚未指定 skillToUnlock，或是 GameDB 尚未初始化！");
            }
        }
    }
}
