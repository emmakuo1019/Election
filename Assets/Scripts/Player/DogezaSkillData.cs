using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DogezaSkillData", menuName = "Election/Skills/DogezaSkillData")]
public class DogezaSkillData : SkillData
{
    [Header("土下座設定")]
    [SerializeField] private float dashSpeed = 15f; // 衝刺速度
    [SerializeField] private float aoeRadius = 2.5f; // 影響半徑
    [SerializeField] private LayerMask targetLayer; // 選民圖層
    [SerializeField] private int mpCost = 10; // MP 消耗

    // 多型：覆寫執行邏輯 (瞬間AOE與扣資源)
    public override void ExecuteSkill(GameObject caster)
    {
        // 1. 扣除 MP 資源 (若 MP 不足，阻止技能繼續執行)
        PlayerMPSystem mpSystem = PlayerMPSystem.Instance != null 
            ? PlayerMPSystem.Instance 
            : caster.GetComponent<PlayerMPSystem>();
            
        if (mpSystem != null && !mpSystem.UseMP(mpCost))
        {
            Debug.LogWarning("[DogezaSkillData] MP 不足！技能取消執行。");
            return;
        }

        // 2. 執行基底邏輯 (播放設定好的特效等)
        base.ExecuteSkill(caster);

        // 3. 瞬間 AOE 邏輯：尋找周遭選民
        Collider[] hitColliders = Physics.OverlapSphere(caster.transform.position, aoeRadius, targetLayer);
        foreach (Collider hit in hitColliders)
        {
            Debug.Log($"[DogezaSkillData] 悲情土下座影響到了物件: {hit.gameObject.name}");
            
            // 如果選民身上有 VoterLogic
            VoterLogic voterLogic = hit.GetComponentInParent<VoterLogic>();
            if (voterLogic != null)
            {
                // 這裡可以套用具體的土下座效果 (暫時以 Log 表示)
                Debug.Log($"[DogezaSkillData] 對 {hit.gameObject.name} 施加土下座效果！");
            }
        }
    }

    // 多型：覆寫每幀更新邏輯 (持續衝刺)
    public override void UpdateSkill(GameObject caster, float deltaTime)
    {
        if (caster == null) return;

        // 取得 CharacterController 進行移動
        CharacterController charCon = caster.GetComponent<CharacterController>();
        if (charCon != null)
        {
            // 將施放者往前 (transform.forward) 移動，速度為 dashSpeed
            // 注意：CharacterController.Move 的參數是位移量，所以要乘上 deltaTime
            Vector3 dashVelocity = caster.transform.forward * dashSpeed * deltaTime;
            charCon.Move(dashVelocity);
        }
    }
}
