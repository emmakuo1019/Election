using UnityEngine;

/// <summary>
/// 敵人生成設定基底。
/// 子類別各自對應一種任務類型，由 RoomMissionData 持有參考。
///
/// 建立路徑：Assets/Data/Mission/SpawnConfigs/
/// </summary>
public abstract class EnemySpawnConfig : ScriptableObject
{
    [Header("敵人 Prefab 組合")]
    [Tooltip("本關可生成的敵人 Prefab 清單，生成時隨機從中挑選")]
    public GameObject[] enemyPrefabs;

    public GameObject GetRandomPrefab()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return null;
        return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
    }
}
