using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour, IAttackSource
{
    [Header("攻擊設定")]
    public float attackRange = 3f;
    public float attackAngle = 60f;
    public int attackInfluence = 1;
    [SerializeField] private float attackCooldown = 0f;
    [SerializeField] private float convertChance = 0.3f;

    [Header("顯示")]
    public AttackRangeMesh attackRangeMesh;
    private CinemachineImpulseSource impulseSource;
    private PlayerController playerController;

    public event Action<float, float> OnAttackShapeChanged;
    public event Action OnAttackPerformed;

    private const int HitBufferSize = 64;
    private readonly Collider[] hitBuffer = new Collider[HitBufferSize];
    private float lastAttackTime = -999f;
    private float baseAttackRange;
    private float currentAttackRange;
    private float currentAttackCooldown;
    private float temporaryAttackRangeMultiplier = 1f;
    private Coroutine rangeBoostCoroutine;

    [Header("Layer")]
    public LayerMask voterLayer;
    [Tooltip("敵人的圖層，供玩家攻擊與中斷")]
    public LayerMask enemyLayer;

    void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        playerController = GetComponent<PlayerController>();
        
        // ── LayerMask 驗證與自動修正 ─────────────────────────────────
        // 防止第二關場景的 Player prefab instance 遺失 LayerMask 設定
        if (voterLayer.value == 0)
        {
            voterLayer = LayerMask.GetMask("Voter");
            Debug.LogWarning("[PlayerAttack] voterLayer 未設定，已自動修正為 Voter 層。");
        }
        
        if (enemyLayer.value == 0)
        {
            enemyLayer = LayerMask.GetMask("Enemy");
            Debug.LogWarning("[PlayerAttack] enemyLayer 未設定，已自動修正為 Enemy 層。");
        }
        
        baseAttackRange = attackRange;
        currentAttackRange = attackRange;
        currentAttackCooldown = attackCooldown;
    }

    void OnEnable()
    {
        RefreshAttackStats();
        SyncAttackRangeMeshRotation();

        if (GameDB.Instance != null && GameDB.Instance.Run != null && GameDB.Instance.Run.Stats != null)
            GameDB.Instance.Run.Stats.OnStatsChanged += RefreshAttackStats;

        if (playerController != null)
            playerController.OnDirectionChanged += OnDirectionChanged;
    }

    void OnDisable()
    {
        if (GameDB.Instance != null && GameDB.Instance.Run != null && GameDB.Instance.Run.Stats != null)
            GameDB.Instance.Run.Stats.OnStatsChanged -= RefreshAttackStats;

        if (playerController != null)
            playerController.OnDirectionChanged -= OnDirectionChanged;
    }

    void Start()
    {
        SyncAttackRangeMeshRotation();
        if (attackRangeMesh != null)
            attackRangeMesh.ShowIdle();
    }

    public float AttackRange => attackRange;
    public float AttackAngle => attackAngle;
    public Vector3 AttackDirection => GetAttackDirection();

    public void UpdateAttackShape(float range, float angle)
    {
        baseAttackRange = range;
        attackRange = range;
        attackAngle = angle;
        RefreshAttackStats();
    }

    private void RefreshAttackStats()
    {
        var stats = GameDB.Instance?.Run?.Stats;
        
        // 取得修改後的基礎攻擊範圍與冷卻 (從 Stats 讀取，若無則使用原始預設值)
        float policyAdjustedRange = stats != null ? stats.ModifiedAttackRange : baseAttackRange;
        currentAttackRange = policyAdjustedRange * temporaryAttackRangeMultiplier;
        
        currentAttackCooldown = stats != null ? stats.ModifiedAttackCooldown : attackCooldown;
        attackRange = currentAttackRange;
        
        OnAttackShapeChanged?.Invoke(currentAttackRange, attackAngle);
    }

    public void ApplyTemporaryRangeBoost(float multiplier, float duration)
    {
        if (multiplier <= 1f || duration <= 0f)
        {
            return;
        }

        if (rangeBoostCoroutine != null)
        {
            StopCoroutine(rangeBoostCoroutine);
        }

        rangeBoostCoroutine = StartCoroutine(TemporaryRangeBoostRoutine(multiplier, duration));
    }

    /// <summary>
    /// 檢查攻擊是否在冷卻中
    /// </summary>
    public bool CanAttack()
    {
        // [TEMP FIX] 暫時停用場景檢查，允許所有場景攻擊
        // if (!SceneContext.IsLevelScene())
        // {
        //     Debug.LogWarning($"⚠️ 只能在關卡中進行攻擊！(SceneContext.CurrentScene={SceneContext.CurrentScene}, IsLevelScene()=false)");
        //     return false;
        // }
        
        if (GameDB.Instance == null)
        {
            Debug.LogWarning("⚠️ 找不到 GameDB，無法施放演說！");
            return false;
        }
        
        if (Time.time < lastAttackTime + currentAttackCooldown)
        {
            // 冷卻中不印 log，避免洗版
            return false;
        }

        return true;
    }

    /// <summary>
    /// 執行物理攻擊判定與數值處理
    /// </summary>
    public void PerformAttack(Vector3 facingDirection)
    {
        Debug.Log("[PlayerAttack] PerformAttack 被呼叫！");
        
        if (!CanAttack())
        {
            Debug.LogWarning($"[PlayerAttack] CanAttack() 返回 false，中止攻擊。lastAttackTime={lastAttackTime}, currentTime={Time.time}, cooldown={currentAttackCooldown}");
            return;
        }

        lastAttackTime = Time.time;

        OnAttackPerformed?.Invoke();
        attackRangeMesh?.Show();

        Vector3 attackDir = facingDirection.sqrMagnitude > 0.001f ? facingDirection.normalized : transform.forward;
        SyncAttackRangeMeshRotation(attackDir);
        bool hitAny = false;

        Debug.Log($"[PlayerAttack] ═══ 攻擊偵測開始 ═══");
        Debug.Log($"[PlayerAttack] 玩家位置: {transform.position}");
        Debug.Log($"[PlayerAttack] 玩家 Y 軸高度: {transform.position.y}");
        Debug.Log($"[PlayerAttack] 攻擊範圍（球體半徑）: {currentAttackRange}");
        Debug.Log($"[PlayerAttack] voterLayer: {voterLayer.value}");
        Debug.Log($"[PlayerAttack] enemyLayer: {enemyLayer.value}");
        Debug.Log($"[PlayerAttack] 合併LayerMask: {(voterLayer.value | enemyLayer.value)}");

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            currentAttackRange,
            hitBuffer,
            voterLayer | enemyLayer
        );

        Debug.Log($"[PlayerAttack] Physics.OverlapSphereNonAlloc 返回 {hitCount} 個碰撞體");
        
        // Debug：手動列出附近所有 layer=7 的物件
        Collider[] allEnemies = Physics.OverlapSphere(transform.position, 20f, enemyLayer);
        Debug.Log($"[PlayerAttack] DEBUG: 20公尺內所有 layer=7 物件共 {allEnemies.Length} 個");
        foreach (var e in allEnemies)
        {
            float dist = Vector3.Distance(transform.position, e.transform.position);
            Debug.Log($"[PlayerAttack]   - {e.name} at {e.transform.position}, 距離={dist:F2}");
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hitBuffer[i];
            if (hit == null) continue;

            VoterLogic voter = hit.GetComponentInParent<VoterLogic>();
            EnemyController enemy = hit.GetComponentInParent<EnemyController>();

            Debug.Log($"[PlayerAttack]  [{i}] 碰撞體={hit.name}, layer={hit.gameObject.layer}, tag={hit.gameObject.tag}");
            Debug.Log($"[PlayerAttack]      GetComponentInParent<VoterLogic>()={voter != null}");
            Debug.Log($"[PlayerAttack]      GetComponentInParent<EnemyController>()={enemy != null}");

            Transform targetTransform = null;
            if (voter != null) targetTransform = voter.transform;
            else if (enemy != null) targetTransform = enemy.transform;
            else continue; // 既不是選民也不是敵人，略過

            // 計算向量與距離防呆
            Vector3 toTarget = targetTransform.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                continue;
            }

            Vector3 dirToTarget = toTarget.normalized;

            // 判斷是否在攻擊扇形範圍內
            float angle = Vector3.Angle(attackDir, dirToTarget);
            Debug.Log($"[PlayerAttack]      攻擊方向角度={angle}°, 扇形半角={attackAngle / 2f}°");
            
            if (angle < attackAngle / 2f)
            {
                Debug.Log($"[PlayerAttack]      ✓ 在攻擊範圍內！");
                
                // 如果是敵人，直接打斷並造成硬直
                if (enemy != null)
                {
                    Debug.Log($"[PlayerAttack]      🎯 對敵人 {enemy.name} 造成傷害！");
                    enemy.TakeDamage(10, 1.0f);
                    hitAny = true;
                }
                // 如果是選民，則進行原本的拉票邏輯
                else if (voter != null)
                {
                    VoterData voterData = voter.Data;
                    
                    // 深色選民免疫玩家普攻
                    if (voterData != null && voterData.HasDarkAttribute)
                    {
                        continue;
                    }

                    // 從 Stats 讀取動態攻擊力/說服力
                    var stats = GameDB.Instance?.Run?.Stats;
                    int currentInfluence = stats != null 
                        ? Mathf.RoundToInt(attackInfluence * stats.ModifiedAttackInfluence) 
                        : attackInfluence;

                    voter.OnInfluence(currentInfluence, false, transform.position);
                    TryConvert(voterData);
                    hitAny = true;
                }
            }
            else
            {
                Debug.Log($"[PlayerAttack]      ✗ 不在攻擊範圍（角度過大）");
            }
        }

        Debug.Log($"[PlayerAttack] ═══ 攻擊偵測結束，hitAny={hitAny} ═══\n");

        if (hitAny)
        {
            impulseSource?.GenerateImpulse();
        }
    }

    private Vector3 GetAttackDirection()
    {
        if (playerController != null && playerController.LastMoveDirection.sqrMagnitude > 0.001f)
            return playerController.LastMoveDirection.normalized;

        return transform.forward;
    }

    private void OnDirectionChanged(Vector3 direction)
    {
        SyncAttackRangeMeshRotation(direction);
    }

    private void SyncAttackRangeMeshRotation()
    {
        SyncAttackRangeMeshRotation(GetAttackDirection());
    }

    private void SyncAttackRangeMeshRotation(Vector3 direction)
    {
        if (attackRangeMesh == null || direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        attackRangeMesh.transform.rotation = Quaternion.LookRotation(direction.normalized);
    }
    
    public void TryConvert(VoterData voter)
    {
        if (voter == null)
            return;

        // 深色選民在 PerformAttack 已被 continue 跳過，此處不會收到深色選民
        // ponytail: 若未來深色選民需要特殊轉化率，在此補充 HasDarkAttribute 分支
        var stats = GameDB.Instance?.Run?.Stats;
        float chance = stats != null ? stats.ModifiedConvertChance : convertChance;

        if (UnityEngine.Random.value < chance)
        {
            ForceConvert(voter);
        }
    }

    private void ForceConvert(VoterData voter)
    {
        if (voter.TryGetComponent<VoterLogic>(out var logic))
        {
            int requiredInfluence = VoterConfig.MAX_POS - voter.CurrentPosition;
            if (requiredInfluence > 0)
            {
                logic.OnInfluence(requiredInfluence, true, transform.position);
            }
        }
    }

    private IEnumerator TemporaryRangeBoostRoutine(float multiplier, float duration)
    {
        temporaryAttackRangeMultiplier = multiplier;
        RefreshAttackStats();

        yield return new WaitForSeconds(duration);

        temporaryAttackRangeMultiplier = 1f;
        RefreshAttackStats();
        rangeBoostCoroutine = null;
    }
}
