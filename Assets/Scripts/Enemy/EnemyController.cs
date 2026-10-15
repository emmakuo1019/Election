using UnityEngine;
using UnityEngine.AI;

// TODO: [BattleEventManager] Add TriggerOnPlayerStunned(float duration)
// and call it from EnemySkillData after ApplyStun(). Pending UI/SFX integration.

/// <summary>
/// 敵人角色分類，供受擊規則、退場行為與任務計數分流使用。
/// 不引入繼承樹；以此欄位做最小分流。
/// </summary>
public enum EnemyRole
{
    Volunteer,  // 志工：耐久歸零暈眩循環，不計入死亡清場
    Reporter,   // 記者：普攻／指定技能可扣血；HP 歸零退場計入記者清場
    EliteBoss   // 精英／Boss：無敵，可被暈眩；搶票任務勝負
}

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
    private PlayerController _cachedPlayer;       // 快取玩家參考

    /// <summary>供 ReporterFollowState 查詢場上玩家（已在 Awake/OnEnable 快取）</summary>
    public PlayerController _cachedPlayerPublic => _cachedPlayer;

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
    public ReporterFollowState ReporterFollowState { get; private set; }

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
    [Tooltip("每次拉票(攻擊)的影響力數值，對選民立場的偏移量；對手方為負值")]
    public int attackInfluence = -1;
    [Tooltip("普攻命中玩家時扣除的誠信 HP；不影響選民拉票量（attackInfluence）")]
    [Min(0)] [SerializeField] private float playerHitDamage = 1f;
    [Tooltip("偵測(觸發追擊)範圍")]
    public float detectionRange = 10f;
    [Tooltip("脫戰距離(遲滯區間)，應大於 detectionRange")]
    public float escapeRange = 15f;

    [Header("HP")]
    [Tooltip("最大血量（志工語意：最大耐久）")]
    public int maxHP = 3;
    [Tooltip("耐久歸零後的長暈眩時間（秒）；志工破防後進入此暫時失能")]
    [SerializeField] private float neutralizeDuration = 5f;
    [Tooltip("一般命中的短硬直時間（秒）；設為 0 可關閉短硬直")]
    [SerializeField] private float hitStunDuration = 0.5f;
    [Tooltip("此敵人的角色類型，決定受擊規則與退場行為")]
    [SerializeField] public EnemyRole enemyRole = EnemyRole.Volunteer;
    [Tooltip("只標記第 8 節點的主要對手。其生命歸零才會結束最終戰；護衛與召喚物不可勾選。")]
    [SerializeField] private bool isFinalBossOpponent;
    
    [Header("Boss設定")]
    [Tooltip("Boss無敵模式：可被暈眩但不會死亡（用於Elite和FinalBoss關卡）")]
    [SerializeField] private bool isInvincible = false;
    
    [Header("Reporter 受擊白名單")]
    [Tooltip("只有在此清單內的玩家技能，才能對 Reporter 造成傷害。\n" +
             "玩家普通攻擊不需列入（永遠有效）。\n" +
             "此清單與 Task 06 的「監視技能清單」完全分開。")]
    [SerializeField] private SkillData[] reporterDamageWhitelist = new SkillData[0];

    [Header("Reporter 跟隨設定（只對 enemyRole = Reporter 有效）")]
    [Tooltip("掃描候選人的最大距離")]
    [Min(0f)] public float followDetectionRange = 20f;
    [Tooltip("與候選人保持的最小距離（停止前進）")]
    [Min(0f)] public float followStopDistance = 3f;
    [Tooltip("超過此距離才放棄目標")]
    [Min(0f)] public float followLoseDistance = 30f;
    [Tooltip("重新掃描候選人的間隔（秒）")]
    [Min(0.1f)] public float targetSearchInterval = 2f;

    [Header("技能排程（EliteBoss 用）")]
    [Tooltip("兩次技能之間的最短間隔（秒），防止連招無空檔。EliteBoss 才有效。")]
    [Min(0f)] public float sharedSkillInterval = 3f;
    [Tooltip("技能施放距離倍率：castRange = blastRadius × 此倍率。玩家超出此距離不出招。")]
    [Min(1f)] public float castRangeMultiplier = 1.5f;

    [Header("技能排程除錯（EliteBoss 用，請勿在正式 Build 開啟）")]
    [Tooltip("開啟後每 3 秒在 Console 輸出技能排程摘要（只對 EliteBoss 生效）")]
    public bool debugSkillScheduling = false;
    private float _debugLogTimer = 0f;
    private const float DebugLogInterval = 3f;

    private int _currentHP = 3;
    // Reporter 永久退場 guard：避免多 Collider 或連續傷害重複執行 Die()
    private bool _isDead = false;

    /// <summary>Reporter 是否已永久退場（供 ReporterFollowState 判斷停止）</summary>
    public bool IsDead => _isDead;

    // ── Task 07-Fix：技能排程計時模型 ────────────────────────────────
    // activeCombatTime：只在 Phase==Active 推進，所有狀態（含暈眩/普攻/施法）都推進。
    // 不受施法資格條件限制，確保冷卻進度與真實戰鬥時間一致。
    private float _activeCombatTime = 0f;
    // 每個技能「下次可用的 activeCombatTime 時間戳」：
    //   初始化為 initialDelay，首次施放只需等 initialDelay（不加 cooldown）。
    private float[] _nextSkillReadyAt;
    // 共用出招間隔：下次允許出招的 activeCombatTime 時間戳
    private float _nextSharedSkillAt = 0f;
    // Round-robin：下次優先檢查的技能索引（Problem C 修正）
    private int _nextSkillCheckIndex = 0;

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
    
    [Header("技能系統")]
    [Tooltip("裝備的技能陣列。普通敵人裝1個，Boss可裝2-3個技能並輪流釋放")]
    public EnemySkillData[] equippedSkills = new EnemySkillData[0];
    
    // 多技能系統私有變數
    private float[] _skillCooldownTimers;
    private EnemySkillState[] _skillStates;
    private int _currentSkillIndex = 0;

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
    /// 帶來源語意的受擊入口（新版，供玩家攻擊呼叫）。
    /// Reporter 白名單判斷從這裡進行；其他角色行為與舊 overload 相同。
    /// </summary>
    public void TakeDamage(int damage, float stunTime, EnemyHitInfo hitInfo)
    {
        // Reporter 白名單：只接受 BasicAttack 與白名單內的 PlayerSkill
        if (enemyRole == EnemyRole.Reporter)
        {
            bool allowed = hitInfo.sourceType == EnemyHitSourceType.BasicAttack;
            if (!allowed && hitInfo.sourceType == EnemyHitSourceType.PlayerSkill)
            {
                // 在白名單中找到對應技能才允許
                if (reporterDamageWhitelist != null)
                {
                    foreach (var s in reporterDamageWhitelist)
                    {
                        if (s != null && s == hitInfo.sourceSkill) { allowed = true; break; }
                    }
                }
            }
            if (!allowed) return;  // 來源不在白名單，靜默忽略
        }

        TakeDamageInternal(damage, stunTime);
    }

    /// <summary>
    /// 舊版受擊入口，保留供相容（內部技能、Debug 等）。
    /// Reporter 語意：視為 Other 來源，不在白名單 → 不扣血。
    /// Volunteer / Elite / Boss 行為不變。
    /// </summary>
    public void TakeDamage(int damage, float stunTime = -1f)
    {
        if (enemyRole == EnemyRole.Reporter)
        {
            // 舊入口不攜帶來源資訊，對 Reporter 語意為「Other」，靜默忽略
            return;
        }
        TakeDamageInternal(damage, stunTime);
    }

    /// <summary>
    /// 實際傷害處理邏輯，由兩個公開 overload 共用。
    /// </summary>
    private void TakeDamageInternal(int damage, float stunTime)
    {
        // 短硬直時間：優先用 Inspector 欄位，外部傳入值為相容保留
        float effectiveStunTime = (stunTime >= 0f) ? stunTime : hitStunDuration;

        // Boss / 精英 或 無敵模式：只受暈眩，不扣血
        bool isBossOrInvincible = isInvincible || enemyRole == EnemyRole.EliteBoss || isFinalBossOpponent;
        if (isBossOrInvincible)
        {
            // 定案規則：重複暈眩不相加、不重設正在進行的暈眩；本輪不做韌性條。
            // 若已在暈眩中，忽略此次受擊，讓原暈眩按第一次的時間正常結束。
            if (StateMachine?.CurrentState is EnemyStunState)
                return;

            // 防呆：若收到控制技能/攻擊（stunTime 或 hitStunDuration），確保硬直時間至少為 0.5 秒（除非顯式傳入 0 秒）
            if (effectiveStunTime <= 0f && stunTime < 0f)
            {
                effectiveStunTime = 0.5f;
            }

            if (effectiveStunTime > 0f)
            {
                StunState?.SetStunDuration(effectiveStunTime, restoreFullHPOnExit: false);
                StateMachine?.ChangeState(StunState);
            }
            return; // 不扣血，直接返回
        }
        
        // 普通敵人（志工、Reporter）的暈眩中保護：暈眩期間不接受新的重置，避免重複延長暈眩。
        // ponytail: Boss/Elite 的同等保護在上方 isBossOrInvincible 分支內處理。
        if (StateMachine?.CurrentState is EnemyStunState)
            return;
        
        // 普通敵人邏輯
        _currentHP -= damage;
        hpBarUI?.Refresh(_currentHP, maxHP, isInvincible);

        if (_currentHP <= 0)
        {
            if (enemyRole == EnemyRole.Reporter)
            {
                Die();
                return;
            }

            // 志工（以及預設耐久角色）：進入長暈眩循環（不呼叫 Die，不計清場）
            StunState?.SetStunDuration(neutralizeDuration, restoreFullHPOnExit: true);
            StateMachine?.ChangeState(StunState);
            
            // 播放志工中立化音效（HP 歸零後進入長暈眩循環的確立點，每次中立化一次）
            // 只對 Volunteer 播放，Reporter 走 Die() 分支不會到達這裡
            if (enemyRole == EnemyRole.Volunteer)
                AudioManager.Instance?.PlaySFX_EnemyVolunteerDeath();
            
            return;
        }

        if (effectiveStunTime > 0f)
        {
            StunState?.SetStunDuration(effectiveStunTime, restoreFullHPOnExit: false);
            StateMachine?.ChangeState(StunState);
        }
    }

    // 恢復滿血並刷新 UI，供中立化後使用
    public void RestoreFullHP()
    {
        _currentHP = maxHP;
        hpBarUI?.Refresh(_currentHP, maxHP, isInvincible);
    }

    // --- Die method re-added for permanent death cases ---
    /// <summary>
    /// 敵人永久退場（目前只有 Reporter 走此路徑）。
    /// _isDead guard 確保多 Collider 或連續傷害只執行一次。
    /// </summary>
    public void Die()
    {
        if (_isDead) return;
        _isDead = true;

        // 立即停止移動（Destroy 有 0.5s 延遲，避免死後還在滑行）
        if (Agent != null && Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
            Agent.ResetPath();
        }

        // Play death animation (fallback to Idle if no specific death animation)
        Animator?.CrossFade("Die", 0.1f);
        // Notify Reporter mission tracker（只有 Reporter 角色需要，走獨立清場路徑）
        if (enemyRole == EnemyRole.Reporter)
        {
            ReporterMissionTracker.NotifyReporterExited(this);
            // Reporter 不計入 EliminateAll 全敵人計數：
            // CheckEnemiesAfterFrame 等一幀，但 Destroy 有 0.5s 延遲，
            // 一幀後 Reporter 還存在，count 不降到 0，導致任務永遠不完成。
            // Reporter 走 ReporterMissionTracker 獨立路徑，不呼叫 NotifyEnemyDied。
        }
        else
        {
            // Notify general spawn tracker（EliminateAll 任務的全敵人計數，Volunteer/Volunteer 等）
            EnemySpawnTracker.NotifyEnemyDied();
        }
        // Destroy after short delay to allow animation to play
        Destroy(gameObject, 0.5f);
    }

    // ==========================================
    // 多技能系統
    // ==========================================

    /// <summary>
    /// 初始化多技能系統
    /// 支援向後兼容：如果只有 equippedSkill，自動轉換為陣列
    /// </summary>
    private void InitializeMultiSkillSystem()
    {
        if (equippedSkills != null && equippedSkills.Length > 0)
        {
            _skillCooldownTimers = new float[equippedSkills.Length];
            _nextSkillReadyAt    = new float[equippedSkills.Length];
            _skillStates         = new EnemySkillState[equippedSkills.Length];

            for (int i = 0; i < equippedSkills.Length; i++)
            {
                // Task 07-Fix Problem B：首次等待只用 initialDelay，不再額外加 cooldown
                EnemySkillData skill = equippedSkills[i] as EnemySkillData;
                _nextSkillReadyAt[i] = skill != null ? skill.initialDelay : 0f;
                _skillCooldownTimers[i] = 0f;  // 保留欄位（Inspector 顯示用），不再驅動排程
                _skillStates[i] = new EnemySkillState(this, StateMachine);
            }
        }
        else
        {
            _nextSkillReadyAt = new float[0];
        }

        _activeCombatTime  = 0f;
        _nextSharedSkillAt = 0f;
        _nextSkillCheckIndex = 0;
    }

    /// <summary>
    /// 取得當前要執行的技能
    /// 供 EnemySkillState 查詢使用
    /// </summary>
    public EnemySkillData GetCurrentSkill()
    {
        if (equippedSkills == null || _currentSkillIndex >= equippedSkills.Length)
            return null;
        return equippedSkills[_currentSkillIndex];
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
        }
        else
        {
            // Collider 已存在
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
        ReporterFollowState = new ReporterFollowState(this, StateMachine);

        _cachedPlayer = FindObjectOfType<PlayerController>();
        
        // ── 多技能系統初始化 ──────────────────────────────────────────
        InitializeMultiSkillSystem();
        
        _currentHP = maxHP;
        hpBarUI?.Refresh(_currentHP, maxHP, isInvincible);
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
                bool warped = Agent.Warp(hit.position);
                if (!warped || !Agent.isOnNavMesh)
                {
                    Debug.LogWarning($"[EnemyController] {name} @ {transform.position}：" +
                        $"SamplePosition 找到 {hit.position} 但 Warp 失敗（warped={warped}, isOnNavMesh={Agent.isOnNavMesh}）。" +
                        "敵人可能無法移動。");
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                else if (enemyRole == EnemyRole.EliteBoss)
                {
                    // Boss/Elite 的 NavMesh 修正結果診斷（只在成功時印，避免與 Warning 重複）
                    Debug.Log($"[EnemyController][NavMesh 診斷] {name}" +
                              $" | 原始位置={transform.position}" +
                              $" | SamplePosition 成功，命中={hit.position}（距離={Vector3.Distance(transform.position, hit.position):F2}m）" +
                              $" | Warp 成功，isOnNavMesh={Agent.isOnNavMesh}");
                }
#endif
            }
            else
            {
                Debug.LogError($"[EnemyController] {name} @ {transform.position}：" +
                    "附近 5 公尺內找不到有效 NavMesh！敵人將無法移動。" +
                    "請確認場景已烘焙 NavMesh 且生成點位於 NavMesh 上。");
            }
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        else if (enemyRole == EnemyRole.EliteBoss && Agent != null)
        {
            // Boss/Elite 正常在 NavMesh 上時也印一條確認訊息，確保能區分兩種情況
            Debug.Log($"[EnemyController][NavMesh 診斷] {name}" +
                      $" | 位置={transform.position}" +
                      $" | isOnNavMesh={Agent.isOnNavMesh}（無需 Warp）");
        }
#endif
        
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
        // Reporter 直接進入跟隨狀態，不走普通 Idle/Move/Attack 路徑
        StateMachine.Initialize(
            (enemyRole == EnemyRole.Reporter && ReporterFollowState != null)
                ? (IState)ReporterFollowState
                : IdleState);
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
        target = null;

        // 重新查找場景中的 PlayerController（跨場景後舊參考已失效）
        _cachedPlayer = FindObjectOfType<PlayerController>();

        hpBarUI?.Refresh(_currentHP, maxHP, isInvincible);

        // 重置狀態機回 Idle（避免從死亡/暈眩等中途狀態繼續執行）
        // Reporter 重置到 FollowState，不走 Idle（Idle 會尋找選民目標）
        if (StateMachine != null && IdleState != null)
        {
            IState resetTo = (enemyRole == EnemyRole.Reporter && ReporterFollowState != null)
                ? (IState)ReporterFollowState
                : IdleState;
            StateMachine.Initialize(resetTo);
        }

        // 重置技能排程計時器（跨場景或物件 re-enable 時使用）
        _activeCombatTime    = 0f;
        _nextSharedSkillAt   = 0f;
        _nextSkillCheckIndex = 0;
        if (_nextSkillReadyAt != null && equippedSkills != null)
        {
            for (int i = 0; i < Mathf.Min(_nextSkillReadyAt.Length, equippedSkills.Length); i++)
            {
                EnemySkillData sd = equippedSkills[i] as EnemySkillData;
                _nextSkillReadyAt[i] = sd != null ? sd.initialDelay : 0f;
            }
        }

        if (attackRangeMesh != null)
            attackRangeMesh.ShowIdle();
    }    private void Update()
    {
        // Fix-D：已死亡（Destroy 延遲期間）不再執行任何狀態邏輯
        if (_isDead) return;

        StateMachine.CurrentState?.Update();

        // ── 多技能系統：檢查所有技能冷卻 ────────────────────────────
        // 只在可行動狀態（非暈眩、非施法、非普攻）時推進冷卻並觸發技能
        // ponytail: 以 is 型別判斷做最小分流；若未來有更多不可行動狀態，統一改為 CanAct() 旗標
        bool isActing = StateMachine.CurrentState is EnemySkillState
                     || StateMachine.CurrentState is EnemyStunState
                     || StateMachine.CurrentState is EnemyAttackState;

        // Reporter 不使用技能排程（Task 06 才加入 Reporter 跟隨行為）
        bool canUseSkills = enemyRole != EnemyRole.Reporter;

        // ── Task 07-Fix：戰鬥計時推進（只在 Active 階段，所有狀態下均推進）──
        bool isActive = BattleEventManager.CurrentEncounterPhase == BattleEventManager.EncounterPhase.Active;
        if (canUseSkills && isActive && _nextSkillReadyAt != null)
        {
            _activeCombatTime += Time.deltaTime;
        }

        // ── 技能排程：施法資格 + Round-robin 選擇 ───────────────────
        if (canUseSkills && !isActing && isActive &&
            equippedSkills != null && equippedSkills.Length > 0 &&
            _nextSkillReadyAt != null)
        {
            // 玩家引用過期時重新查找（每次發現失效時做一次，不每幀掃）
            if (_cachedPlayer == null || !_cachedPlayer.gameObject.activeInHierarchy)
                _cachedPlayer = Object.FindFirstObjectByType<PlayerController>();

            bool playerOk    = _cachedPlayer != null && _cachedPlayer.gameObject.activeInHierarchy;
            bool sharedReady = _activeCombatTime >= _nextSharedSkillAt;

            if (playerOk && sharedReady)
            {
                int count = equippedSkills.Length;
                // Round-robin：從 _nextSkillCheckIndex 開始，最多檢查 count 次
                for (int k = 0; k < count; k++)
                {
                    int i = (_nextSkillCheckIndex + k) % count;

                    if (equippedSkills[i] == null) continue;
                    if (equippedSkills[i].cooldown <= 0f) continue;

                    // 個別技能是否 Ready（Problem A 修正：由 activeCombatTime 判斷，不在此累加）
                    bool skillReady = _activeCombatTime >= _nextSkillReadyAt[i];
                    if (!skillReady) continue;

                    // 玩家在 XZ 施放距離內
                    EnemySkillData skillData = equippedSkills[i] as EnemySkillData;
                    if (skillData != null)
                    {
                        float castRange = skillData.blastRadius * castRangeMultiplier;
                        Vector3 diff = _cachedPlayer.transform.position - transform.position;
                        diff.y = 0f;
                        if (diff.sqrMagnitude > castRange * castRange)
                        {
                            // 距離不足：跳過此招但繼續看下一招（不消耗 ready）
                            continue;
                        }
                    }

                    // 通過所有資格：設定下次可用時間，觸發技能
                    _nextSkillReadyAt[i]  = _activeCombatTime + equippedSkills[i].cooldown;
                    _nextSharedSkillAt    = _activeCombatTime + sharedSkillInterval;
                    _nextSkillCheckIndex  = (i + 1) % count;  // Round-robin 推進
                    _currentSkillIndex    = i;
                    StateMachine.ChangeState(_skillStates[i]);

                    if (debugSkillScheduling && enemyRole == EnemyRole.EliteBoss)
                        Debug.Log($"[SkillSchedule] {name} 施放技能[{i}] {equippedSkills[i].skillName}" +
                                  $" @ activeCombat={_activeCombatTime:F1}s，" +
                                  $"下次可用={_nextSkillReadyAt[i]:F1}s");
                    break;
                }
            }

            // 除錯摘要（每 3 秒一次，不洗版）
            if (debugSkillScheduling && enemyRole == EnemyRole.EliteBoss)
            {
                _debugLogTimer += Time.deltaTime;
                if (_debugLogTimer >= DebugLogInterval)
                {
                    _debugLogTimer = 0f;
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append($"[SkillSchedule] {name} | State={StateMachine.CurrentState?.GetType().Name}" +
                              $" | Phase=Active | CombatT={_activeCombatTime:F1}s" +
                              $" | SharedReady={sharedReady}({_nextSharedSkillAt:F1}s)");
                    for (int i = 0; i < equippedSkills.Length; i++)
                    {
                        if (equippedSkills[i] == null) continue;
                        float remain = _nextSkillReadyAt[i] - _activeCombatTime;
                        bool ready = remain <= 0f;
                        float castRange = (equippedSkills[i] is EnemySkillData sd)
                            ? sd.blastRadius * castRangeMultiplier : 0f;
                        float playerDist = playerOk
                            ? Vector3.Distance(transform.position, _cachedPlayer.transform.position)
                            : -1f;
                        sb.Append($"\n  [{i}]{equippedSkills[i].skillName}" +
                                  $" Ready={ready}(remain={Mathf.Max(0, remain):F1}s)" +
                                  $" castRange={castRange:F1} playerDist={playerDist:F1}");
                    }
                    Debug.Log(sb.ToString());
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (_isDead) return;
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
        // Reporter 不執行普攻命中（不拉票、不攻擊玩家）
        if (enemyRole == EnemyRole.Reporter) return;
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

        // 同次攻擊去重：以角色根物件元件為身分，避免多 Collider 重複結算同一角色。
        // ponytail: HashSet 每次 PerformAttackHit 建立一次（攻擊頻率低，GC 可接受）。
        var hitVoters  = new System.Collections.Generic.HashSet<VoterLogic>();
        var hitPlayers = new System.Collections.Generic.HashSet<PlayerController>();

        // --- 對選民的判定 ---
        Collider[] voterHits = Physics.OverlapSphere(hitCenter, attackHitRadius, targetLayerMask);
        foreach (Collider hit in voterHits)
        {
            VoterLogic voter = hit.GetComponentInParent<VoterLogic>();
            if (voter == null) continue;
            if (!hitVoters.Add(voter)) continue; // 同一選民已結算，跳過

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
                if (!hitPlayers.Add(pc)) continue; // 同一玩家已結算，跳過

                // 扇形角度過濾
                Vector3 dirToPlayer = (pc.transform.position - transform.position);
                dirToPlayer.y = 0;
                if (dirToPlayer.sqrMagnitude > 0.001f)
                {
                    dirToPlayer.Normalize();
                    float angleToPlayer = Vector3.Angle(AttackDirection, dirToPlayer);
                    if (angleToPlayer > AttackAngle / 2f) continue;
                }

                PlayerHealthSystem.Instance?.TakeDamage(playerHitDamage);
            }
        }
    }

    /// <summary>
    /// 由 EnemySkillState 在前搖結束時呼叫，執行技能判定
    /// </summary>
    public void PerformSkillHit()
    {
        EnemySkillData currentSkill = GetCurrentSkill();
        if (currentSkill == null)
        {
            Debug.LogWarning("[EnemyController] PerformSkillHit: 當前技能為空");
            return;
        }
        currentSkill.ExecuteSkill(gameObject);
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
