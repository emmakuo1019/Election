using UnityEngine;

/// <summary>
/// 總部出口觸發器
/// 將此腳本掛載在總部出口的實體方塊上，並將方塊的 Collider 設為 IsTrigger = true
/// </summary>
public class HQExitTrigger : MonoBehaviour
{
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return; // 防呆，避免玩家重複碰撞觸發多次載入

        // 確認碰到方塊的是玩家
        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            Debug.Log("[HQExitTrigger] 玩家已觸碰出口方塊，準備前往戰鬥關卡...");

            if (UIManager.Instance != null && GameFlowManager.Instance != null)
            {
                var campaign = GameDB.Instance?.Campaign;
                if (campaign != null)
                {
                    // 初始化 Block 進度（設定 RoomSequence，供舊 fallback 使用）
                    campaign.StartNextCampaignBlock();

                    // 為第一關設定 ActiveRoom：從 MissionPool 抽一個任務直接設為選項 0
                    // ponytail: 第一關只有一間房，不需要岔路，直接 GenerateNextOptions + SelectOption(0)
                    var pool = GameDB.Instance.MissionPool;
                    if (pool != null)
                    {
                        var drawn = pool.DrawRandom(1);
                        campaign.GenerateNextOptions(drawn[0], null);
                    }
                    else
                    {
                        campaign.GenerateNextOptions(null, null);
                    }
                    campaign.SelectOption(0);
                }

                UIManager.Instance.FadeOut(1.0f, () => 
                {
                    GameFlowManager.Instance.ChangeState(new GameplayState(1));
                });
            }
            else
            {
                Debug.LogError("[HQExitTrigger] 找不到 UIManager 或 GameFlowManager，無法切換場景！");
            }
        }
    }
}
