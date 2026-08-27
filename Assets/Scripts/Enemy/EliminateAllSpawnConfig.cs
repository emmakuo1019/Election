using UnityEngine;

/// <summary>
/// 全滅任務的生成設定。
/// 一次性生成固定數量的敵人，全數陣亡後觸發過關。
///
/// 建立方式：右鍵 → Create / Mission / SpawnConfig / EliminateAll
/// </summary>
[CreateAssetMenu(fileName = "SpawnConfig_EliminateAll",
                 menuName = "Mission/SpawnConfig/EliminateAll")]
public class EliminateAllSpawnConfig : EnemySpawnConfig
{
    [Header("全滅任務設定")]
    [Tooltip("一次性生成的敵人總數")]
    [Min(1)] public int totalEnemyCount = 10;
}
