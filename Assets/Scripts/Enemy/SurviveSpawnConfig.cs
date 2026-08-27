using UnityEngine;

/// <summary>
/// 生存任務的生成設定。
/// 每隔固定秒數持續湧入一批敵人，直到任務結束。
///
/// 建立方式：右鍵 → Create / Mission / SpawnConfig / Survive
/// </summary>
[CreateAssetMenu(fileName = "SpawnConfig_Survive",
                 menuName = "Mission/SpawnConfig/Survive")]
public class SurviveSpawnConfig : EnemySpawnConfig
{
    [Header("生存任務設定")]
    [Tooltip("第一波延遲秒數（場景載入後等待多久生成第一波）")]
    [Min(0f)] public float initialDelay = 1f;

    [Tooltip("每波之間的間隔秒數")]
    [Min(0.5f)] public float waveInterval = 8f;

    [Tooltip("每波生成數量")]
    [Min(1)] public int enemiesPerWave = 4;
}
