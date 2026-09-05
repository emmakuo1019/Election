using UnityEngine;
using UnityEngine.AI;

// TODO: [BattleEventManager] Add TriggerOnPlayerStunned(float duration)
// and call it from EnemySkillData after ApplyStun(). Pending UI/SFX integration.

/// <summary>
/// 敵人的基礎控制器，負責持有組件參考、共用資料，並將生命週期委派給狀態機。
/// (已加入 GC 優化：預先實例化所有狀態)
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour, IAttackSource
{
    // ==========================================
    // 核心組件與狀態機
    // ==========================================
    public StateMachine StateMachine { get; private set; }
    public Animator Animator { get; private set; }
    public NavMeshAgent Agent { get; private set; }
    
    private Camera mainCamera;

    private float _skillCooldownTimer = 0f;
    private EnemySkillState _skillState;          // 預先實例化（GC 優化）
    private PlayerController _cachedPlayer;       // 快取玩家參考

    [Header("Visuals")]
    [Tooltip("用於控制翻面的 SpriteRenderer (建議放在子物件上)")]
    public SpriteRenderer spriteRenderer;
    [Tooltip("攻擊範圍的網格視覺化")]
    public AttackRangeMesh attackRangeMesh;

    // ==========================================
    // 狀態實例緩存 (GC 優化)
    // ==========================================
    public EnemyIdleState IdleState { get; private set; }
    public EnemyMoveState MoveState { get; private set; }
    public EnemyAttackState AttackState { get; private set; }
    public EnemyStunState StunState { get; private set; }
    public EnemyWanderState WanderState { get; private set; }

    // ==========================================
    // 共享數據與參考 (供各個 State 讀取/寫入)
    // ==========================================

    [Header("Targeting")]
    [Tooltip("選民的圖層，用於尋找選民目標與攻擊判定")]
    public LayerMask targetLayerMask;

    [Tooltip("玩家的圖層，用於偵測並攻擊玩家")]
    public LayerMask playerLayerMask;

    [Tooltip("是否將玩家也納入攻擊目標（優先度低於選民）")]
    public bool canTargetPlayer = true;

    [Tooltip("目前鎖定的目標。若一開始就拖曳指定，將不會進行範圍掃描。")]
    public Transform target;

    [Header("Faction")]
    [Tooltip("代表該敵人的陣營符號，預設為對應 VoterData 的敵方陣營 (-1)")]
    public int factionSign = -1;

    [Header("Wander")]
    [Tooltip("沒有目標時隨機遊蕩的半徑（0 = 使用偵測範圍的一半）")]
    public float wanderRadius = 6f;
    [Tooltip("抵達遊蕩點後停留最短時間（秒）")]
    public float wanderIntervalMin = 1f;
    [Tooltip("抵達遊蕩點後停留最長時間（秒）")]
    public float wanderIntervalMax = 3f;

    [Header("Combat Stats")]
    [Tooltip("移動速度 (會自動覆蓋 NavMeshAgent 的 Speed)")]
    public float moveSpeed = 4.5f;
    [Tooltip("攻擊距離 (決定何時停下腳步發動攻擊)")]
    public float attackRange = 2f;
    [Tooltip("每次拉票(攻擊)的影響力數值，對手方為負值")]
    public int attackInfluence = -1;
    [Tooltip("偵測(觸發追擊)範圍")]
    public float detectionRange = 10f;
    [Tooltip("脫戰距離(遲滯區間)，應大於 detectionRange")]
    public float escapeRange = 15f;

    [Header("HP")]
    [Tooltip("最大血量")]
    public int maxHP = 3;
    [Tooltip("只標記第 8 節點的主要對手。其生命歸零才會結束最終戰；護衛與召喚物不可勾選。")]
    [SerializeField] private bool isFinalBossOpponent;
    private int _currentHP;

    [Header("UI")]
    [SerializeField] private EnemyHPBarUI hpBarUI;

    [Header("Attack Hit Detection")]
    [Tooltip("攻擊判定的球體半徑 ")]
    public float attackHitRadius = 2f;
    [Tooltip("攻擊判定球體的本地位移")]
    public Vector3 attackHitOffset = new Vector3(0, 1f, 0f);

    [Header("Attack Timing")]
    [Tooltip("攻擊動畫總時長 (秒) - 決定攻擊頻率")]
    public float attackDuration = 1.0f;
    [Tooltip("傷害判定點 (秒) - 決定前搖有多長")]
    public float attackHitTime = 0.3f;

    // ==========================================
    // IAttackSource 實作
    // ==========================================
    public float AttackRange => attackHitRadius;
    public float AttackAngle => attackAngle;
    public Vector3 AttackDirection { get; private set; } = Vector3.forward;
    public event System.Action<float, float> OnAttackShapeChanged;

    [Header("Attack Shape")]
    [Tooltip("視覺上的攻擊扇形角度 (例如普通攻擊 360 度，大招 180 度)")]
    [SerializeField] private float attackAngle = 360f;

    [Header("暈眩特效")]
    public GameObject stunVfxPrefab;         // 暈眩時生成的 VFX Prefab
    public Vector3 stunVfxOffset = new Vector3(0f, 2f, 0f); // VFX 相對角色的偏移（預設頭頂）
    public EnemySkillData equippedSkill;          // 裝備的技能 ScriptableObject
    // 冷卻時間統一由 equippedSkill.cooldown 控制，不在此重複設定

    /// <summary>
    /// 供技能系統動態改變攻擊形狀 (半徑與角度)，並通知 AttackRangeMesh 重新生成網格。
    /// </summary>
    public void SetAttackShape(float range, float angle)
    {
        attackHitRadius = range;
        attackAngle = angle;
        OnAttackShapeChanged?.Invoke(AttackRange, AttackAngle);
    }

    // ==========================================
    // 受擊與中斷機制
    // ==========================================

    /// <summary>
    /// 敵人受到傷害或控制技能時呼叫。
    /// 扣除血量並強制切換至硬直狀態，中斷當前行為。
    /// </summary>
    public void TakeDamage(int damage, float stunTime = 0.5f)
    {
        _currentHP -= damage;
        hpBarUI?.Refresh(_currentHP, maxHP);
        Debug.Log($"Enemy: 受到 {damage} 點傷害，剩餘 HP {_currentHP}/{maxHP}");

        if (_currentHP <= 0)
        {
            Die();
            return;
        }

        if (stunTime > 0f)
        {
            StunState.SetStunDuration(stunTime);
            StateMachine.ChangeState(StunState);
        }
    }

    private void Die()
    {
        EnemySpawnTracker.NotifyEnemyDied();
        if (isFinalBossOpponent)
            BattleEventManager.TriggerFinalBossDefeated();
        Destroy(gameObject);
    }

    // ==========================================
    // Unity 生命週期
    // ==========================================

    private void Awake()
    {
        // 取得核心組件 (Animator 支援放在子物件)
        Animator = GetComponentInChildren<Animator>();
        Agent = GetComponent<NavMeshAgent>();
        mainCamera = Camera.main;

        // ── 自動補上 Capsule Collider（若 Prefab 上忘記加）──────────────
        // Physics.OverlapSphere 需要 Collider 才能偵測到此物件。
        // 若 Root 上還沒有任何 Collider，就自動建立一個與 NavMeshAgent 同尺寸的 CapsuleCollider。
        if (GetComponent<Collider>() == null)
        {
            float agentHeight = Agent != null ? Agent.height : 2f;
            float agentRadius = Agent != null ? Agent.radius : 0.5f;
            
            // 防呆：NavMeshAgent 有時在 Awake 初始幀數值尚未就緒，強制使用合理的最小值
            if (agentHeight <= 0f) agentHeight = 2f;
            if (agentRadius <= 0f) agentRadius = 0.5f;
            
            var cap = gameObject.AddComponent<CapsuleCollider>();
            cap.height = agentHeight;
            cap.radius = agentRadius;
            cap.center = new Vector3(0f, agentHeight * 0.5f, 0f);
            cap.isTrigger = false;
            Debug.Log($"[EnemyController] {name} 自動加入 CapsuleCollider (h={cap.height}, r={cap.radius}, layer={gameObject.layer})");
        }
        else
        {
            Debug.Log($"[EnemyController] {name} 已有 Collider，略過自動補建。(layer={gameObject.layer})");
        }
        
        // 防呆：避免之前編譯錯誤時 Inspector 把數值存成了 0，導致狀態機死循環
        if (attackDuration <= 0.1f) attackDuration = 1.0f;
        if (attackHitTime <= 0f) attackHitTime = 0.3f;
        if (moveSpeed <= 0f) moveSpeed = 4.5f;
        if (maxHP <= 0) maxHP = 3;

        // 關閉導航代理的自動旋轉，確保根節點不會因為尋路而轉向 (解決 Sprite 穿幫問題)
        if (Agent != null)
        {
            Agent.updateRotation = false;
            Agent.speed = moveSpeed; // 套用自定義的移動速度
        }

        StateMachine = new StateMachine();

        // 【GC 優化】在 Awake 時就將所有狀態實例化並快取起來
        IdleState = new EnemyIdleState(this, StateMachine);
        MoveState = new EnemyMoveState(this, StateMachine);
        AttackState = new EnemyAttackState(this, StateMachine);
        StunState = new EnemyStunState(this, StateMachine);
        WanderState = new EnemyWanderState(this, StateMachine);

        _skillState = new EnemySkillState(this, StateMachine);
        _cachedPlayer = FindObjectOfType<PlayerController>();
        
        _currentHP = maxHP;
        hpBarUI?.Refresh(_currentHP, maxHP);
    }

    private void Start()
    {
        // ── NavMesh 驗證與修正 ────────────────────────────────────────
        // 確保敵人在有效的 NavMesh 上，避免跨場景初始化時序問題導致無法被攻擊
        if (Agent != null && !Agent.isOnNavMesh)
        {
            // 嘗試將 Agent 放到最近的 NavMesh 表面
            if (UnityEngine.AI.NavMesh.SamplePosition(
                transform.position, out UnityEngine.AI.NavMeshHit hit, 
                5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                Agent.Warp(hit.position);
                Debug.LogWarning($"[EnemyController] {name} 初始不在 NavMesh 上，已自動修正至 {hit.position}");
            }
            else
            {
                Debug.LogError($"[EnemyController] {name} 附近 5 公尺內找不到 NavMesh！敵人可能無法正常移動。");
            }
        }
        
        // ── Collider 驗證 ─────────────────────────────────────────────
        // 確保 Collider 存在且啟用，避免無法被玩家攻擊偵測
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"[EnemyController] {name} 沒有 Collider！無法被攻擊！");
        }
        else
        {
            if (!col.enabled)
            {
                col.enabled = true;
                Debug.LogWarning($"[EnemyController] {name} Collider 被停用，已強制啟用。");
            }
            
            // 計算 Collider 的世界座標中心點（用於 Physics 查詢）
            Vector3 colliderWorldCenter = transform.position;
            if (col is CapsuleCollider capsule)
            {
                colliderWorldCenter += capsule.center;
            }
            else if (col is BoxCollider box)
            {
                colliderWorldCenter += box.center;
            }
            else if (col is SphereCollider sphere)
            {
                colliderWorldCenter += sphere.center;
            }
            
            Debug.Log($"[EnemyController] {name} Collider 狀態: enabled={col.enabled}, isTrigger={col.isTrigger}, layer={gameObject.layer}");
            Debug.Log($"[EnemyController] {name} 位置: GameObject={transform.position}, Collider中心={colliderWorldCenter}");
        }
        
        // 防呆：確保脫戰距離大於偵測距離，形成正確的遲滯區間 (Hysteresis)
        if (escapeRange <= detectionRange)
        {
            escapeRange = detectionRange + 2f;
        }

        if (attackRangeMesh != null)
        {
            OnAttackShapeChanged?.Invoke(AttackRange, AttackAngle);
            attackRangeMesh.ShowIdle();
        }

        // 啟動狀態機，直接傳入快取好的 IdleState
        StateMachine.Initialize(IdleState);
    }

    private void OnEnable()
    {
        // 防禦性 reset：若物件被 re-enable 時 HP 已歸零（例如物件池或 SetActive 誤用），
        // 強制完整初始化，確保第二關以後敵人狀態正確。
        if (_currentHP <= 0 && StateMachine != null)
        {
            ResetState();
        }

        // 重新抓場景內的 PlayerController，避免跨場景時持有已銷毀的舊參考
        if (_cachedPlayer == null)
            _cachedPlayer = FindObjectOfType<PlayerController>();

        // 通知追蹤器：此敵人已上場（Awake 後首次 OnEnable，或波次追加時）
        if (_currentHP > 0)
            EnemySpawnTracker.NotifyEnemySpawned();
    }

    private void OnDisable()
    {
        // 不需額外操作：Die() 已在 Destroy 前呼叫 NotifyEnemyDied()，不會重複扣數
    }

    /// <summary>
    /// 重置所有運行時狀態，用於物件 re-enable 或未來接入物件池時的防禦性保護。
    /// </summary>
    private void ResetState()
    {
        _currentHP = maxHP;
        _skillCooldownTimer = 0f;
        target = null;

        // 重新查找場景中的 PlayerController（跨場景後舊參考已失效）
        _cachedPlayer = FindObjectOfType<PlayerController>();

        hpBarUI?.Refresh(_currentHP, maxHP);

        // 重置狀態機回 Idle（避免從死亡/暈眩等中途狀態繼續執行）
        if (StateMachine != null && IdleState != null)
            StateMachine.Initialize(IdleState);

        if (attackRangeMesh != null)
            attackRangeMesh.ShowIdle();

        Debug.Log($"[EnemyController] {name} ResetState — HP 與狀態機已重置。");
    }    private void Update()
    {
        StateMachine.CurrentState?.Update();

        // 技能冷卻計時（僅在非技能狀態時累加）
        if (equippedSkill != null && 
            StateMachine.CurrentState is not EnemySkillState)
        {
            _skillCooldownTimer += Time.deltaTime;
            if (_skillCooldownTimer >= equippedSkill.cooldown)
            {
                _skillCooldownTimer = 0f;
                StateMachine.ChangeState(_skillState);
            }
        }
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentState?.PhysicsUpdate();
    }

    // ==========================================
    // 戰鬥邏輯與物理偵測
    // ==========================================
    
    public bool IsTargetValid(Transform t)
    {
        if (t == null) return false;

        // 玩家目標永遠有效（只要 GameObject 還活著）
        if (canTargetPlayer && t.GetComponentInParent<PlayerController>() != null)
            return true;

        VoterLogic voter = t.GetComponentInParent<VoterLogic>();
        if (voter == null || voter.Data == null) return false;
        
        // 1. 避開深色與冷感選民
        if (voter.Data.HasDarkAttribute || voter.Data.HasColdAttribute) return false;
        
        // 2. 避開已經被敵方轉化的選民 (已是自己人)
        if (voter.Data.ConvertedSide == factionSign) return false;
        
        return true;
    }

    /// <summary>
    /// 尋找偵測範圍內最近的目標（選民優先，沒有選民時鎖定玩家）。
    /// 供狀態機在閒置或攻擊結束後呼叫，重新鎖定目標。
    /// </summary>
    public void FindNearestTarget()
    {
        float minDistance = float.MaxValue;
        Transform nearest = null;

        // 1. 先在選民 Layer 找有效選民
        Collider[] voterHits = Physics.OverlapSphere(transform.position, detectionRange, targetLayerMask);
        foreach (var hit in voterHits)
        {
            VoterLogic voter = hit.GetComponentInParent<VoterLogic>();
            if (voter == null) continue;
            if (!IsTargetValid(voter.transform)) continue;

            float dist = Vector3.Distance(transform.position, voter.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = voter.transform;
            }
        }

        // 2. 找不到選民時，若允許攻擊玩家則改鎖定玩家
        if (nearest == null && canTargetPlayer && playerLayerMask != 0)
        {
            Collider[] playerHits = Physics.OverlapSphere(transform.position, detectionRange, playerLayerMask);
            foreach (var hit in playerHits)
            {
                PlayerController pc = hit.GetComponentInParent<PlayerController>();
                if (pc == null) continue;

                float dist = Vector3.Distance(transform.position, pc.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = pc.transform;
                }
            }
        }

        target = nearest;
    }

    /// <summary>舊版介面，保留供外部呼叫相容（內部轉發到 FindNearestTarget）。</summary>
    public void FindNearestVoter() => FindNearestTarget();

    /// <summary>
    /// 由攻擊狀態 (EnemyAttackState) 在特定時間點 (動畫前搖結束) 呼叫。
    /// 執行物理範圍偵測，對範圍內的選民發動拉票，對玩家造成誠信傷害。
    /// </summary>
    public void PerformAttackHit()
    {
        // 根據目標或移動方向，推算前方判定球的中心點
        Vector3 attackDir = transform.forward;

        if (target != null)
        {
            attackDir = (target.position - transform.position);
            attackDir.y = 0;
            attackDir.Normalize();
        }
        else if (Agent != null && Agent.velocity.sqrMagnitude > 0.01f)
        {
            attackDir = Agent.velocity.normalized;
        }

        // 假設 attackHitOffset.z 為前方距離，attackHitOffset.y 為高度
        Vector3 hitCenter = transform.position + attackDir * attackHitOffset.z + Vector3.up * attackHitOffset.y;

        // --- 對選民的判定 ---
        Collider[] voterHits = Physics.OverlapSphere(hitCenter, attackHitRadius, targetLayerMask);
        foreach (Collider hit in voterHits)
        {
            VoterLogic voter = hit.GetComponentInParent<VoterLogic>();
            if (voter == null) continue;

            // 【陣營防呆】如果該選民已經是自己人，直接跳過
            if (voter.Data != null && voter.Data.ConvertedSide == factionSign) continue;

            // 扇形角度過濾
            Vector3 dirToTarget = (voter.transform.position - transform.position);
            dirToTarget.y = 0;
            if (dirToTarget.sqrMagnitude > 0.001f)
            {
                dirToTarget.Normalize();
                float angleToTarget = Vector3.Angle(AttackDirection, dirToTarget);
                if (angleToTarget > AttackAngle / 2f) continue;
            }

            Debug.Log($"敵人對選民 {voter.name} 發動了拉票！");
            voter.OnInfluence(attackInfluence, false, transform.position);
        }

        // --- 對玩家的判定 ---
        if (canTargetPlayer && playerLayerMask != 0)
        {
            Collider[] playerHits = Physics.OverlapSphere(hitCenter, attackHitRadius, playerLayerMask);
            foreach (Collider hit in playerHits)
            {
                PlayerController pc = hit.GetComponentInParent<PlayerController>();
                if (pc == null) continue;

                // 扇形角度過濾
                Vector3 dirToPlayer = (pc.transform.position - transform.position);
                dirToPlayer.y = 0;
                if (dirToPlayer.sqrMagnitude > 0.001f)
                {
                    dirToPlayer.Normalize();
                    float angleToPlayer = Vector3.Angle(AttackDirection, dirToPlayer);
                    if (angleToPlayer > AttackAngle / 2f) continue;
                }

                Debug.Log($"敵人對玩家 {pc.name} 發動了攻擊，造成誠信傷害！");
                PlayerHealthSystem.Instance?.TakeDamage(1f);
            }
        }
    }

    /// <summary>
    /// 由 EnemySkillState 在前搖結束時呼叫，執行技能判定
    /// </summary>
    public void PerformSkillHit()
    {
        if (equippedSkill == null) return;
        equippedSkill.ExecuteSkill(gameObject);
    }

    /// <summary>
    /// 更新 Sprite 的朝向 (左右翻轉)。
    /// 供狀態機在 Update 時呼叫，確保 Sprite 面向目前的移動方向或目標。
    /// </summary>
    public void UpdateFacingDirection()
    {
        Vector3 currentDir = transform.forward;

        // 若有鎖定目標，優先根據目標相對位置翻轉與計算方向
        if (target != null)
        {
            currentDir = (target.position - transform.position);
            currentDir.y = 0;
            if (currentDir.sqrMagnitude > 0.001f)
            {
                currentDir.Normalize();
            }

            float dirX = currentDir.x;
            if (spriteRenderer != null && Mathf.Abs(dirX) > 0.05f)
            {
                spriteRenderer.flipX = dirX < 0; // 預設面向右方時，若目標在左方則翻轉
            }
        }
        // 若無目標，但正在移動，則根據速度方向翻轉與計算方向
        else if (Agent != null && Agent.velocity.sqrMagnitude > 0.01f)
        {
            currentDir = Agent.velocity.normalized;
            float dirX = currentDir.x;
            if (spriteRenderer != null && Mathf.Abs(dirX) > 0.05f)
            {
                spriteRenderer.flipX = dirX < 0;
            }
        }

        // 執行 Billboard (廣告牌) 效果，確保 Sprite 永遠面向攝影機
        if (spriteRenderer != null && mainCamera != null)
        {
            spriteRenderer.transform.forward = mainCamera.transform.forward;
        }

        // 將計算出的方向存入 AttackDirection 供 IAttackSource 讀取
        if (currentDir.sqrMagnitude > 0.001f)
        {
            AttackDirection = currentDir;
            
            // 讓攻擊視覺網格精準指向目前的攻擊方向
            if (attackRangeMesh != null)
            {
                attackRangeMesh.transform.rotation = Quaternion.LookRotation(currentDir);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 繪製攻擊判定範圍以便於在 Scene 視圖中調整
        Gizmos.color = Color.red;
        
        Vector3 attackDir = transform.forward;
        if (Application.isPlaying)
        {
            if (target != null)
            {
                attackDir = (target.position - transform.position);
                attackDir.y = 0;
                attackDir.Normalize();
            }
            else if (Agent != null && Agent.velocity.sqrMagnitude > 0.01f)
            {
                attackDir = Agent.velocity.normalized;
            }
        }

        Vector3 hitCenter = transform.position + attackDir * attackHitOffset.z + Vector3.up * attackHitOffset.y;
        Gizmos.DrawWireSphere(hitCenter, attackHitRadius);
    }
}
