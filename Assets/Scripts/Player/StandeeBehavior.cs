using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 理性流派技能：「放置人形立牌」的具體行為邏輯
/// 負責掛載於立牌 Prefab 上，持續吸引範圍內的理性選民，並給予靠近的選民單次說服。
/// </summary>
public class StandeeBehavior : MonoBehaviour
{
    [Header("立牌設定")]
    public float attractRadius = 5f;
    public float damageRadius = 1.5f;
    public float damage = 10f;
    public float lifetime = 5f;
    public LayerMask voterLayer;

    // 紀錄已受傷/受說服的選民，避免重複扣血
    private HashSet<VoterLogic> _damagedVoters = new HashSet<VoterLogic>();
    
    // 紀錄已受吸引的選民，避免每幀重複切換打斷其他狀態 (如受擊)
    private HashSet<VoterLogic> _attractedVoters = new HashSet<VoterLogic>();

    /// <summary>
    /// 初始化立牌數值並開始運作
    /// </summary>
    public void Initialize(float radius, float interval, float duration, LayerMask layer)
    {
        attractRadius = radius;
        lifetime = duration;
        voterLayer = layer;
        
        // 每次生成時清空紀錄
        _damagedVoters.Clear();
        _attractedVoters.Clear();

        // 【自動防呆與除錯】檢查 LayerMask 是否為 0 (Nothing)
        if (voterLayer.value == 0)
        {
            Debug.LogWarning("[StandeeBehavior] 警告：傳入的 LayerMask 為 Nothing (0)！這會導致完全掃描不到選民。系統已暫時自動將其設為 AllLayers (~0) 進行測試。請回到 StandeeSkillData 將 Target Layer 設好！");
            voterLayer.value = ~0; // 設定為所有圖層
        }
        else
        {
            Debug.Log($"[StandeeBehavior] 立牌初始化成功！attractRadius: {attractRadius}, voterLayer.value: {voterLayer.value}");
        }

        StartCoroutine(StandeeRoutine());
    }

    private IEnumerator StandeeRoutine()
    {
        // 立牌存在指定時間後自動銷毀
        yield return new WaitForSeconds(lifetime);

        if (PoolManager.HasInstance && gameObject.activeSelf)
        {
            // 若未來改為完整物件池回收，可在此呼叫 Release
            // 配合企劃需求與預設行為，目前直接 Destroy
        }
        
        Destroy(gameObject);
    }

    private void Update()
    {
        // 使用 OverlapSphere 掃描周圍選民
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attractRadius, voterLayer);
        foreach (Collider hit in hitColliders)
        {
            VoterLogic voterLogic = hit.GetComponentInParent<VoterLogic>();
            if (voterLogic != null && voterLogic.Data != null)
            {
                bool isRational = voterLogic.Data.Label == VoterLabel.Rational;
                
                // [Task 1: 除錯日誌] 輸出掃描到的選民資訊
                Debug.Log($"[StandeeBehavior] 掃描到選民: {hit.gameObject.name} | 是否為理性標籤: {isRational}");

                // 1. 標籤篩選：必須為「理性 (Rational)」
                if (isRational)
                {
                    // [Task 3: 狀態轉換與 HashSet 確認]
                    // 只要是理性選民且尚未被加入吸引名單，就強制切換到 VoterAttractedState
                    if (!_attractedVoters.Contains(voterLogic))
                    {
                        Debug.Log($"[StandeeBehavior] 強制將 {hit.gameObject.name} 切換至 VoterAttractedState");
                        voterLogic.StateMachine.ChangeState(new VoterAttractedState(voterLogic, this.transform));
                        _attractedVoters.Add(voterLogic);
                    }
                    else
                    {
                        // 如果已經在吸引名單中，但因為某種原因狀態跑掉了 (例如在圈內待太久回到 Idle)，
                        // 且目前不是吸引狀態，也可根據需求再次強制拉回 (此處先用 HashSet 避免每幀強制打斷 Hit/Stun 狀態)
                        if (!(voterLogic.StateMachine.CurrentState is VoterAttractedState))
                        {
                            // 若需強制拉回，可解除下方註解：
                            // voterLogic.StateMachine.ChangeState(new VoterAttractedState(voterLogic, this.transform));
                        }
                    }

                    // 3. 傷害判定：計算距離，若小於判定半徑則給予單次說服
                    float distance = Vector3.Distance(transform.position, voterLogic.transform.position);
                    if (distance <= damageRadius)
                    {
                        if (!_damagedVoters.Contains(voterLogic))
                        {
                            // 呼叫選民的受擊/影響方法，並標記為已說服
                            voterLogic.OnInfluence((int)damage, true, transform.position, false);
                            _damagedVoters.Add(voterLogic);
                            Debug.Log($"[StandeeBehavior] 對 {hit.gameObject.name} 造成立牌說服傷害");
                        }
                    }
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 視覺化影響半徑 (僅在 Editor 中選取時可見)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attractRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}
