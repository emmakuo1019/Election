using UnityEngine;

public class GameDBTest : MonoBehaviour
{
    // 每幀檢查並印出 GameDB 中的核心數值
    private void Update()
    {
        if (GameDB.Instance == null || GameDB.Instance.Run == null) 
        {
            return;
        }
        
        //Debug.Log($"[GameDB Test] HP: {GameDB.Instance.Run.CurrentHP}/{GameDB.Instance.Run.MaxHP} | " +
                 // $"MP: {GameDB.Instance.Run.CurrentMP}/{GameDB.Instance.Run.MaxMP} | " +
                 // $"Votes: Player {GameDB.Instance.Run.PlayerVotes} / Opponent {GameDB.Instance.Run.OpponentVotes}");
    }
}
