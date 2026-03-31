using UnityEngine;

public class BattleSceneController : MonoBehaviour
{
    private void Start()
    {
        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.StartTimer();
        }
        else
        {
            Debug.LogError("❌ 場景中找不到 LevelTimer，無法開始關卡計時");
        }
    }
}