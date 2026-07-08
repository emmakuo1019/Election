using UnityEngine;

/// <summary>
/// 理性流派技能：「放置人形立牌」的資料定義與執行邏輯
/// 放置型 (Fire and Forget) 技能，玩家施放後可自由移動。
/// </summary>
[CreateAssetMenu(fileName = "StandeeSkillData", menuName = "Election/Skills/StandeeSkillData")]
public class StandeeSkillData : SkillData
{
    [Header("立牌專屬設定")]
    [Tooltip("影響半徑")]
    public float attractionRadius = 5f;
    
    [Tooltip("轉化頻率 (秒)")]
    public float conversionInterval = 2f;
    
    [Tooltip("立牌存在場上的時間")]
    public float standeeDuration = 10f;
    
    [Tooltip("目標選民的 Layer")]
    public LayerMask targetLayer;

    [Tooltip("施放技能的 MP 消耗")]
    [SerializeField] private int mpCost = 15;

    public override void ExecuteSkill(GameObject caster)
    {
        // 1. 資源防呆檢查 (遵守 GameDB SSOT 規範)
        if (GameDB.Instance != null && GameDB.Instance.Run.CurrentMP < mpCost)
        {
            Debug.LogWarning($"[{skillName}] MP 不足！技能取消執行。");
            return;
        }

        // 扣除對應 MP
        GameDB.Instance?.Run.ModifyMP(-mpCost);

        // 2. 產生立牌並初始化數值
        // 由於我們需要對立牌進行 Initialize，不使用 base.ExecuteSkill 以免重複產生
        if (vfxPrefab != null && caster != null)
        {
            // 在施法者前方產生立牌 (例如前方 1.5 公尺處)
            Vector3 spawnPosition = caster.transform.position + caster.transform.forward * 1.5f;
            
            GameObject standeeInstance = null;
            if (PoolManager.HasInstance)
            {
                standeeInstance = PoolManager.Instance.Get(vfxPrefab, spawnPosition, caster.transform.rotation);
            }
            else
            {
                standeeInstance = Instantiate(vfxPrefab, spawnPosition, caster.transform.rotation);
            }

            if (standeeInstance != null)
            {
                // 若立牌掛有 PooledVFXInstance，我們也將 vfxDuration 賦值，避免被提早回收
                if (standeeInstance.TryGetComponent<PooledVFXInstance>(out var vfxInstance))
                {
                    vfxInstance.duration = standeeDuration;
                }

                // 取得並初始化 StandeeBehavior
                StandeeBehavior behavior = standeeInstance.GetComponent<StandeeBehavior>();
                if (behavior != null)
                {
                    behavior.Initialize(attractionRadius, conversionInterval, standeeDuration, targetLayer);
                }
                else
                {
                    Debug.LogWarning($"[{skillName}] 產生的立牌 Prefab 缺少 StandeeBehavior 元件！請確保掛載。");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[{skillName}] 缺少 vfxPrefab 或施法者不存在，無法產生立牌！");
        }

        Debug.Log($"[StandeeSkillData] 成功執行立牌技能邏輯：{skillName}");
    }
    
    // 此技能為放置型 (Fire and Forget)，不需要維持施法狀態，故不需要覆寫 UpdateSkill。
}
