using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 根據當前房間任務類型自動生成敵人。
/// 放置在戰鬥場景的 GameObject 上（建議和 BattleFlowController 同層）。
///
/// 運作流程：
///   Start() → 從 GameDB 讀取 ActiveRoom.mission.spawnConfig
///   → EliminateAllSpawnConfig : 一次性生成 totalEnemyCount 隻，並呼叫 EnemySpawnTracker.StartTracking()
///   → SurviveSpawnConfig      : 等 initialDelay 後開始每 waveInterval 秒生成一波，不追蹤全滅
///
/// 生成點優先序：
///   1. spawnConfig.spawnPointOverrides（若非空）
///   2. 場景中所有 Tag 為 "EnemySpawnPoint" 的 GameObject
///   3. 自身 transform（保底）
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("生成點（優先使用；留空則自動找 Tag=EnemySpawnPoint 的物件）")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Fallback 設定（無任務 Config 時使用）")]
    [SerializeField] private GameObject fallbackEnemyPrefab;
    [SerializeField] private int fallbackCount = 5;

    // ── 內部狀態 ─────────────────────────────────────────────────────
    private Coroutine _spawnCoroutine;
    private Transform[] _resolvedSpawnPoints;

    // ── Unity 生命週期 ────────────────────────────────────────────────

    private void OnEnable()
    {
        BattleEventManager.OnRoomCleared += StopSpawning;
    }

    private void OnDisable()
    {
        BattleEventManager.OnRoomCleared -= StopSpawning;
    }

    private void Start()
    {
        EnemySpawnConfig config = GameDB.Instance?.Campaign.ActiveRoom.mission?.spawnConfig;

        // 解析生成點（供所有子流程共用）
        _resolvedSpawnPoints = ResolveSpawnPoints();

        // 生成敵人前先重置追蹤器，清除上一關的殘留狀態（_trackingActive / _aliveCount）
        // 若不在這裡重置，上一關正常結束後 _trackingActive 仍為 true，
        // 新場景敵人 OnEnable → NotifyEnemySpawned 會在 StartTracking() 前就累加 count，
        // 甚至可能讓 count 意外歸零並提前觸發全滅事件。
        EnemySpawnTracker.StopTracking();

        switch (config)
        {
            case EliminateAllSpawnConfig eliminateConfig:
                _spawnCoroutine = StartCoroutine(SpawnEliminateAll(eliminateConfig));
                break;

            case SurviveSpawnConfig surviveConfig:
                _spawnCoroutine = StartCoroutine(SpawnSurviveWaves(surviveConfig));
                break;

            case null:
                // 無任務或任務未掛 SpawnConfig → fallback
                if (fallbackEnemyPrefab != null)
                {
                    _spawnCoroutine = StartCoroutine(SpawnFallback());
                }
                else
                {
                    Debug.LogWarning("[EnemySpawner] 無 SpawnConfig 且無 fallbackEnemyPrefab，不生成敵人。");
                }
                break;

            default:
                Debug.LogWarning($"[EnemySpawner] 未知的 SpawnConfig 類型：{config.GetType().Name}");
                break;
        }
    }

    // ── 生成協程 ─────────────────────────────────────────────────────

    /// <summary>EliminateAll：一次性生成所有敵人，完成後啟動追蹤器</summary>
    private IEnumerator SpawnEliminateAll(EliminateAllSpawnConfig config)
    {
        int count = config.totalEnemyCount;
        Debug.Log($"[EnemySpawner] EliminateAll — 開始生成 {count} 隻敵人");

        for (int i = 0; i < count; i++)
        {
            SpawnEnemy(config.GetRandomPrefab());
            // 每 5 隻讓出一幀，避免生成卡頓
            if (i > 0 && i % 5 == 0) yield return null;
        }

        // 所有敵人已放置完畢，開始追蹤存活數
        EnemySpawnTracker.StartTracking();
        Debug.Log("[EnemySpawner] EliminateAll — 生成完畢，追蹤器啟動");
    }

    /// <summary>Survive：週期性湧入，不追蹤全滅條件</summary>
    private IEnumerator SpawnSurviveWaves(SurviveSpawnConfig config)
    {
        Debug.Log($"[EnemySpawner] Survive — 等待初始延遲 {config.initialDelay}s");
        if (config.initialDelay > 0f)
            yield return new WaitForSeconds(config.initialDelay);

        int waveIndex = 0;
        while (true)
        {
            waveIndex++;
            Debug.Log($"[EnemySpawner] Survive — 第 {waveIndex} 波，生成 {config.enemiesPerWave} 隻");
            for (int i = 0; i < config.enemiesPerWave; i++)
            {
                SpawnEnemy(config.GetRandomPrefab());
            }
            yield return new WaitForSeconds(config.waveInterval);
        }
    }

    /// <summary>Fallback：無 config 時的保底流程（EliminateAll 邏輯）</summary>
    private IEnumerator SpawnFallback()
    {
        Debug.Log($"[EnemySpawner] Fallback — 生成 {fallbackCount} 隻");
        for (int i = 0; i < fallbackCount; i++)
        {
            SpawnEnemy(fallbackEnemyPrefab);
            if (i > 0 && i % 5 == 0) yield return null;
        }
        EnemySpawnTracker.StartTracking();
    }

    // ── 單隻生成 ─────────────────────────────────────────────────────

    private void SpawnEnemy(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[EnemySpawner] SpawnEnemy 收到 null prefab，跳過。");
            return;
        }

        Transform spawnPoint = GetRandomSpawnPoint();
        Vector3 spawnPos = spawnPoint.position + new Vector3(
            Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f));

        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        Debug.Log($"[EnemySpawner] 生成 {prefab.name} at {spawnPos}");
    }

    // ── 生成點解析 ────────────────────────────────────────────────────

    private Transform[] ResolveSpawnPoints()
    {
        // 1. Inspector 指定
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints;

        // 2. 場景 Tag
        GameObject[] tagged = GameObject.FindGameObjectsWithTag("EnemySpawnPoint");
        if (tagged != null && tagged.Length > 0)
        {
            var transforms = new Transform[tagged.Length];
            for (int i = 0; i < tagged.Length; i++) transforms[i] = tagged[i].transform;
            return transforms;
        }

        // 3. 自身保底
        Debug.LogWarning("[EnemySpawner] 找不到生成點，使用自身位置。");
        return new Transform[] { transform };
    }

    private Transform GetRandomSpawnPoint()
    {
        if (_resolvedSpawnPoints == null || _resolvedSpawnPoints.Length == 0)
            return transform;
        return _resolvedSpawnPoints[Random.Range(0, _resolvedSpawnPoints.Length)];
    }

    // ── 停止 ─────────────────────────────────────────────────────────

    public void StopSpawning()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }

    /// <summary>
    /// 停止生成並立即清除場上所有敵人。
    /// Survive 任務計時結束時由 BattleFlowController 呼叫。
    /// </summary>
    public void ClearAllEnemies()
    {
        StopSpawning();
        var enemies = Object.FindObjectsByType<EnemyController>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var e in enemies)
            e.gameObject.SetActive(false);
        Debug.Log($"[EnemySpawner] ClearAllEnemies — 清除 {enemies.Length} 隻敵人");
    }
}
