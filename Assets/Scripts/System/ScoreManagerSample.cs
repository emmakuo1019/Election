using UnityEngine;

/// <summary>
/// 這是示範如何訂閱 BattleEventManager.OnVoterConverted 事件的計分管理器範例。
/// 實際開發時可以將此邏輯整合進 GameDB.RunData 或是獨立的 ScoreSystem 中。
/// </summary>
public class ScoreManagerSample : MonoBehaviour
{
    [Header("分數統計")]
    public int playerScore = 0;
    public int enemyScore = 0;

    private void OnEnable()
    {
        // 訂閱全域轉化事件
        BattleEventManager.OnVoterConverted += HandleVoterConverted;
    }

    private void OnDisable()
    {
        // 務必解除訂閱，避免 Memory Leak 或 NullReferenceException
        BattleEventManager.OnVoterConverted -= HandleVoterConverted;
    }

    private void HandleVoterConverted(int side)
    {
        if (side == VoterData.PlayerSideSign)
        {
            playerScore += 10; // 玩家轉化成功加 10 分
            Debug.Log($"[ScoreManagerSample] 玩家成功拉攏一位選民！目前得分：{playerScore}");
        }
        else if (side == VoterData.EnemySideSign)
        {
            enemyScore += 10; // 敵人轉化成功加 10 分
            Debug.Log($"[ScoreManagerSample] 對手成功洗腦一位選民！對手得分：{enemyScore}");
        }
    }
}
