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

    // ==========================================
    // 共享數據與參考 (供各個 State 讀取/寫入)
    // ==========================================

    [Header("Targeting")]
    [Tooltip("目標與拉票判定的圖層 (請統一設定為選民 Voter 的 Layer)")]
    public LayerMask targetLayerMask;

    [Tooltip("目前鎖定的目標。若一開始就拖曳指定，將不會進行範圍掃描。")]
    public Transform target;

    [Header("Faction")]
    [Tooltip("代表該敵人的陣營符號，預設為對應 VoterData 的敵方陣營 (-1)")]
    public int factionSign = -1;

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
    [SerializeField] private LayerMask playerLayerMask; // 玩家偵測 Layer
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
        gameObject.SetActive(false);
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

        _skillState = new EnemySkillState(this, StateMachine);
        _cachedPlayer = FindObjectOfType<PlayerController>();
        
        _currentHP = maxHP;
        hpBarUI?.Refresh(_currentHP, maxHP);
    }

    private void Start()
    {
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
        // 復活或波次追加時通知追蹤器
        if (_currentHP > 0)
            EnemySpawnTracker.NotifyEnemySpawned();
    }

    private void OnDisable()
    {
        // SetActive(false) 時確保不重複扣計數（Die() 已呼叫過 NotifyEnemyDied）
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
        VoterLogic voter = t.GetComponentInParent<VoterLogic>();
        if (voter == null || voter.Data == null) return false;
        
        // 1. 避開深色與冷感選民
        if (voter.Data.HasDarkAttribute || voter.Data.HasColdAttribute) return false;
        
        // 2. 避開已經被敵方轉化的選民 (已是自己人)
        if (voter.Data.ConvertedSide == factionSign) return false;
        
        return true;
    }

    /// <summary>
    /// 尋找偵測範圍內最近的選民 (Voter)。
    /// 供狀態機在閒置或攻擊結束後呼叫，重新鎖定目標。
    /// </summary>
    public void FindNearestVoter()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, targetLayerMask);
        float minDistance = float.MaxValue;
        Transform nearest = null;

        foreach (var hit in hits)
        {
            if (!IsTargetValid(hit.transform)) continue;

            VoterLogic voter = hit.GetComponentInParent<VoterLogic>();
            if (voter != null)
            {
                float dist = Vector3.Distance(transform.position, voter.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = voter.transform;
                }
            }
        }
        target = nearest;
    }

    /// <summary>
    /// 由攻擊狀態 (EnemyAttackState) 在特定時間點 (動畫前搖結束) 呼叫。
    /// 執行物理範圍偵測，並對範圍內的選民發動拉票 (Influence)。
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
        
        Collider[] hits = Physics.OverlapSphere(hitCenter, attackHitRadius, targetLayerMask);
        foreach (Collider hit in hits)
        {
            // 取得目標身上的 VoterLogic 組件
            VoterLogic voter = hit.GetComponentInParent<VoterLogic>();
            if (voter != null)
            {
                // 【陣營防呆】如果該選民已經是自己人，直接跳過，不進行拉票與干擾
                if (voter.Data != null && voter.Data.ConvertedSide == factionSign)
                {
                    continue;
                }

                // 計算敵人指向該選民的水平向量
                Vector3 dirToTarget = (voter.transform.position - transform.position);
                dirToTarget.y = 0;
                
                if (dirToTarget.sqrMagnitude > 0.001f)
                {
                    dirToTarget.Normalize();
                    
                    // 計算與當前攻擊面向的夾角
                    float angleToTarget = Vector3.Angle(AttackDirection, dirToTarget);
                    
                    // 若角度大於扇形的一半，代表在扇形範圍外，忽略該選民
                    if (angleToTarget > AttackAngle / 2f)
                    {
                        continue;
                    }
                }

                Debug.Log($"敵人對選民 {voter.name} 發動了拉票！");
                // 呼叫選民改變支持度的方法 (使用 attackInfluence)
                voter.OnInfluence(attackInfluence, false, transform.position);
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
