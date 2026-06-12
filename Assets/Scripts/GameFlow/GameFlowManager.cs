using UnityEngine;

/// <summary>
/// 全域遊戲流程狀態機管理器
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    private StateMachine stateMachine;

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

        // 測試機制：按下 1~7 切換對應狀態
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeState(new BootState());
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeState(new MainMenuState());
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeState(new CharacterSelectState());
        if (Input.GetKeyDown(KeyCode.Alpha4)) ChangeState(new HQState());
        if (Input.GetKeyDown(KeyCode.Alpha5)) ChangeState(new GameplayState(1)); // 測試假定房間為 1
        if (Input.GetKeyDown(KeyCode.Alpha6)) ChangeState(new StageClearState(1)); // 測試假定房間為 1
        if (Input.GetKeyDown(KeyCode.Alpha7)) ChangeState(new GameEndState(true)); // 測試假定為勝利
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
