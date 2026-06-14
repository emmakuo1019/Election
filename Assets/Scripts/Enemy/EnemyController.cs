using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敵人的基礎控制器，負責持有組件參考、共用資料，並將生命週期委派給狀態機。
/// (已加入 GC 優化：預先實例化所有狀態)
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    // ==========================================
    // 核心組件與狀態機
    // ==========================================
    public StateMachine StateMachine { get; private set; }
    public Animator Animator { get; private set; }
    public NavMeshAgent Agent { get; private set; }

    // ==========================================
    // 狀態實例緩存 (GC 優化)
    // ==========================================
    public EnemyIdleState IdleState { get; private set; }
    public EnemyMoveState MoveState { get; private set; }
    public EnemyAttackState AttackState { get; private set; }

    // ==========================================
    // 共享數據與參考 (供各個 State 讀取/寫入)
    // ==========================================

    [Header("Targeting")]
    [Tooltip("目標的圖層 (例如：設定為選民的 Layer)")]
    public LayerMask targetLayer;

    [Tooltip("目前鎖定的目標。若一開始就拖曳指定，將不會進行範圍掃描。")]
    public Transform target;

    [Header("Combat Stats")]
    [Tooltip("攻擊距離")]
    public float attackRange = 2f;
    [Tooltip("偵測(觸發追擊)範圍")]
    public float detectionRange = 10f;
    [Tooltip("脫戰距離(遲滯區間)，應大於 detectionRange")]
    public float escapeRange = 15f;

    // ==========================================
    // Unity 生命週期
    // ==========================================

    private void Awake()
    {
        // 取得核心組件 (Animator 支援放在子物件)
        Animator = GetComponentInChildren<Animator>();
        Agent = GetComponent<NavMeshAgent>();
        StateMachine = new StateMachine();

        // 【GC 優化】在 Awake 時就將所有狀態實例化並快取起來
        IdleState = new EnemyIdleState(this, StateMachine);
        MoveState = new EnemyMoveState(this, StateMachine);
        AttackState = new EnemyAttackState(this, StateMachine);
    }

    private void Start()
    {
        // 防呆：確保脫戰距離大於偵測距離，形成正確的遲滯區間 (Hysteresis)
        if (escapeRange <= detectionRange)
        {
            escapeRange = detectionRange + 2f;
        }

        // 啟動狀態機，直接傳入快取好的 IdleState
        StateMachine.Initialize(IdleState);
    }

    private void Update()
    {
        StateMachine.CurrentState?.Update();
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentState?.PhysicsUpdate();
    }
}
