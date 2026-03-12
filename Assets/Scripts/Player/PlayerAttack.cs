using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("演說設定")]
    public float attackRange = 3.0f;
    public float attackAngle = 60.0f;
    public int attackInfluence = 1;    // 每次攻擊的影響力值
    public int fundingCost = 10;       // 消耗資金 (預留)

    [Header("攻擊對話框特效")]
    public GameObject attackBubblePrefab;        // 攻擊時顯示在玩家身上的對話框 Prefab
    //public string[] attackDialogues = { "支持我！", "一起來！", "讓我們改變！" };
    public float bubbleDisplayDuration = 1.0f;   // 對話框顯示時間

    private GameObject activeBubble;

    /// <summary>
    /// 發動演說攻擊，影響扇形範圍內的選民並觸發攻擊對話框特效。
    /// </summary>
    public void PerformSpeech()
    {
        Debug.Log("發動：現場演說！");

        // 顯示玩家攻擊對話框特效
        ShowAttackBubble();

        // 偵測範圍內的選民
        bool hitAny = false;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
        foreach (var hit in hitColliders)
        {
            if (hit.TryGetComponent<VoterLogic>(out var voterLogic))
            {
                Vector3 dirToVoter = (hit.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dirToVoter);
                if (angle < attackAngle / 2f)
                {
                    voterLogic.OnInfluence(attackInfluence, isSkill: false, transform.position);
                    hitAny = true;
                    Debug.Log($"演說命中選民：{hit.gameObject.name}");
                }
            }
        }

        if (!hitAny)
            Debug.Log("演說範圍內沒有選民。");
    }

    private void ShowAttackBubble()
    {
        if (attackBubblePrefab == null) return;

        if (activeBubble != null)
        {
            StopAllCoroutines();
            Destroy(activeBubble);
        }

        activeBubble = Instantiate(attackBubblePrefab, transform.position + Vector3.up * 2f, Quaternion.identity, transform);

        //if (attackDialogues.Length > 0)
        //{
        //    string dialogue = attackDialogues[Random.Range(0, attackDialogues.Length)];
        //    var tmp = activeBubble.GetComponentInChildren<TMPro.TextMeshPro>();
        //    if (tmp != null) tmp.text = dialogue;
       // }

        StartCoroutine(HideBubbleAfterDelay());
    }

    private IEnumerator HideBubbleAfterDelay()
    {
        yield return new WaitForSeconds(bubbleDisplayDuration);
        if (activeBubble != null)
        {
            Destroy(activeBubble);
            activeBubble = null;
        }
    }
}
