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
        BattleEventManager.OnObjectiveResolved += HandleObjectiveResolved;
    }

    private void OnDisable()
    {
        BattleEventManager.OnObjectiveResolved -= HandleObjectiveResolved;
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
        // Reporter 任務的跨場景殘留狀態也一起清除
        ReporterMissionTracker.Instance?.Reset();

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
                }
                break;

            default:
                break;
        }
    }

    // ── 生成協程 ─────────────────────────────────────────────────────

    /// <summary>EliminateAll：一次性生成所有敵人，完成後啟動追蹤器</summary>
    private IEnumerator SpawnEliminateAll(EliminateAllSpawnConfig config)
    {
        int count = config.totalEnemyCount;
        bool isReporterMission = GameDB.Instance?.Campaign.ActiveRoom.mission?.isReporterMission ?? false;

        for (int i = 0; i < count; i++)
        {
            GameObject obj = SpawnEnemy(config.GetRandomPrefab());

            // Reporter 清場任務：將生成的 Reporter 登記到 ReporterMissionTracker
            if (isReporterMission && obj != null)
            {
                EnemyController ec = obj.GetComponent<EnemyController>();
                if (ec != null && ec.enemyRole == EnemyRole.Reporter)
                {
                    if (ReporterMissionTracker.Instance == null)
                    {
                        Debug.LogError("[EnemySpawner] Reporter 清場任務：場景中找不到 ReporterMissionTracker！" +
                            "請在場景的 MissionTracker 同層 GameObject 上掛載 ReporterMissionTracker。");
                    }
                    else
                    {
                        ReporterMissionTracker.Instance.Register(ec);
                    }
                }
                else if (ec == null)
                {
                    Debug.LogWarning($"[EnemySpawner] 生成的物件 {obj.name} 缺少 EnemyController，已跳過登記。");
                }
                else
                {
                    // 生成的不是 Reporter（可能是混合 config 中的 Volunteer），不登記
                }
            }
            
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }

        if (isReporterMission)
        {
            // Reporter 任務：通知 registration 完成，由 ReporterMissionTracker 負責判斷完成。
            // 不啟動 EnemySpawnTracker：Reporter 排除後 count=0 會立即誤觸 AllEnemiesDefeated。
            ReporterMissionTracker.Instance?.FinishRegistration();
        }
        else
        {
            // 一般 EliminateAll：啟動全敵人計數追蹤器
            EnemySpawnTracker.StartTracking();
        }
    }

    /// <summary>Survive：週期性湧入，不追蹤全滅條件</summary>
    private IEnumerator SpawnSurviveWaves(SurviveSpawnConfig config)
    {
        if (config.initialDelay > 0f)
            yield return new WaitForSeconds(config.initialDelay);

        int waveIndex = 0;
        while (true)
        {
            waveIndex++;
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
        bool isReporterMission = GameDB.Instance?.Campaign.ActiveRoom.mission?.isReporterMission ?? false;
        if (isReporterMission)
        {
            // Reporter 任務不得走 fallback：會繞過 Register/FinishRegistration 流程
            Debug.LogError("[EnemySpawner] Reporter 清場任務走到 fallback！" +
                "請確認 RoomMissionData 有掛載 EliminateAllSpawnConfig，且 isReporterMission=true 的任務不使用 fallback。" +
                "任務將保持未完成。");
            yield break;
        }

        for (int i = 0; i < fallbackCount; i++)
        {
            SpawnEnemy(fallbackEnemyPrefab);
            yield return new WaitForFixedUpdate();
        }
        EnemySpawnTracker.StartTracking();
    }

    // ── 單隻生成 ─────────────────────────────────────────────────────

    private GameObject SpawnEnemy(GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        Transform spawnPoint = GetRandomSpawnPoint();
        Vector3 spawnPos = spawnPoint.position + new Vector3(
            Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f));

        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        return obj;
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

    private void HandleObjectiveResolved(EncounterOutcome _)
    {
        StopSpawning();
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
            Object.Destroy(e.gameObject);
    }
}
