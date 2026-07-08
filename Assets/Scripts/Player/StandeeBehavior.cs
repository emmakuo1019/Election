using UnityEngine;
using System.Collections;

/// <summary>
/// 理性流派技能：「放置人形立牌」的具體行為邏輯
/// 負責掛載於立牌 Prefab 上，定時將範圍內的理性選民轉化為玩家陣營。
/// </summary>
public class StandeeBehavior : MonoBehaviour
{
    [Header("數值監控 (由 StandeeSkillData 傳入)")]
    public float attractionRadius;
    public float conversionInterval;
    public float standeeDuration;
    public LayerMask targetLayer;

    /// <summary>
    /// 初始化立牌數值並開始運作
    /// </summary>
    public void Initialize(float radius, float interval, float duration, LayerMask layer)
    {
        attractionRadius = radius;
        conversionInterval = interval;
        standeeDuration = duration;
        targetLayer = layer;

        StartCoroutine(StandeeRoutine());
    }

    private IEnumerator StandeeRoutine()
    {
        float elapsedTime = 0f;

        // 立牌存在期間持續運作
        while (elapsedTime < standeeDuration)
        {
            // 每次等待 interval 秒
            yield return new WaitForSeconds(conversionInterval);
            elapsedTime += conversionInterval;

            if (elapsedTime > standeeDuration) break;

            ConvertOneRationalVoter();
        }

        // 時間到後自動銷毀 (若有 PoolManager，也可以改為 ReturnToPool)
        if (PoolManager.HasInstance && gameObject.activeSelf)
        {
            // 如果此物件是透過 PoolManager 生成的，交給它回收較佳，
            // 但依據需求若無特別設定，可直接 Destroy。這裡先優先 Destroy 配合企劃要求。
        }
        
        Destroy(gameObject);
    }

    /// <summary>
    /// 使用 OverlapSphere 尋找並轉化 1 名尚未被玩家完全轉化的理性選民
    /// </summary>
    private void ConvertOneRationalVoter()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attractionRadius, targetLayer);
        foreach (Collider hit in hitColliders)
        {
            VoterLogic voterLogic = hit.GetComponentInParent<VoterLogic>();
            if (voterLogic != null && voterLogic.Data != null)
            {
                // 檢查是否為理性選民，且尚未完全被玩家轉化
                if (voterLogic.Data.Label == VoterLabel.Rational && !voterLogic.Data.IsPlayerAligned)
                {
                    // 強制轉化為玩家陣營
                    voterLogic.ForceConvertToPlayer();
                    Debug.Log($"[StandeeBehavior] 成功自動轉化理性選民：{hit.gameObject.name}");
                    break; // 每次只轉化 1 人
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 視覺化影響半徑 (僅在 Editor 中選取時可見)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attractionRadius);
    }
}
