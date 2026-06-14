using UnityEngine;

/// <summary>
/// 全域遊戲流程狀態機管理器
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    private StateMachine stateMachine;
    public IState CurrentState => stateMachine?.CurrentState;

    // 安全房出現機率 (0 = 0%, 1 = 100%)
    [SerializeField] [Range(0f, 1f)] private float safeRoomSpawnChance = 0.2f;
    public float SafeRoomSpawnChance => safeRoomSpawnChance;

    private void Awake()
    {
        // Singleton 實作，確保整個遊戲生命週期只有一個 GameFlowManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 實例化狀態機
        stateMachine = new StateMachine();
    }

    private void Start()
    {
        // 啟動第一階段狀態
        stateMachine.Initialize(new BootState());
    }

    private void Update()
    {
        // 驅動當前狀態的 Update
        stateMachine.CurrentState?.Update();
        
    }

    private void FixedUpdate()
    {
        // 驅動當前狀態的 PhysicsUpdate
        stateMachine.CurrentState?.PhysicsUpdate();
    }

    /// <summary>
    /// 全局呼叫此方法切換遊戲流程狀態
    /// 範例: GameFlowManager.Instance.ChangeState(new GameplayState(2));
    /// </summary>
    public void ChangeState(IState newState)
    {
        stateMachine.ChangeState(newState);
    }
}
