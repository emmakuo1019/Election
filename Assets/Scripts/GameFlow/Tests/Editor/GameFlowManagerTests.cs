using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class GameFlowManagerTests
{
    private GameFlowManager _gameFlowManager;
    private UIManager _uiManager;

    [SetUp]
    public void SetUp()
    {
        // 建立測試用的 GameObject
        var go = new GameObject("TestGameFlowManager");
        _gameFlowManager = go.AddComponent<GameFlowManager>();

        var uiGo = new GameObject("TestUIManager");
        _uiManager = uiGo.AddComponent<UIManager>();

        // ⚠️ 關鍵修復：Edit Mode 下不能執行 DontDestroyOnLoad，否則會拋出 InvalidOperationException。
        // 所以我們不呼叫 Awake，而是直接利用反射 (Reflection) 把必要的欄位和單例設好。

        // 1. 手動設定 GameFlowManager.Instance
        var gameFlowInstanceProp = typeof(GameFlowManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        if (gameFlowInstanceProp != null)
        {
            gameFlowInstanceProp.GetSetMethod(true)?.Invoke(null, new object[] { _gameFlowManager });
        }

        // 2. 手動實例化 GameFlowManager 內部的 stateMachine
        var stateMachineField = typeof(GameFlowManager).GetField("stateMachine", BindingFlags.NonPublic | BindingFlags.Instance);
        if (stateMachineField != null)
        {
            // 由於 StateMachine 的建構子可能是預設的，我們透過 Activator 建立它
            var stateMachineInstance = System.Activator.CreateInstance(stateMachineField.FieldType);
            stateMachineField.SetValue(_gameFlowManager, stateMachineInstance);
        }

        // 3. 手動設定 UIManager.Instance
        var uiInstanceProp = typeof(UIManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        if (uiInstanceProp != null)
        {
            uiInstanceProp.GetSetMethod(true)?.Invoke(null, new object[] { _uiManager });
        }
    }

    [Test]
    public void GameplayState_OnRoomCleared_ShouldTransitionToStageClearState()
    {
        // ==========================================
        // Arrange (準備)
        // ==========================================
        int testRoomNumber = 1;
        var gameplayState = new GameplayState(testRoomNumber);
        
        // 預期在 Edit Mode 執行 GameplayState.Enter() 裡的 SceneManager.LoadSceneAsync 會拋出錯誤，我們主動忽略它
        UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Exception, new System.Text.RegularExpressions.Regex(".*EditorSceneManager\\.OpenScene.*"));

        // 強制將當前狀態設置為 GameplayState
        _gameFlowManager.ChangeState(gameplayState);

        // 斷言初始狀態確實為 GameplayState，且狀態機初始化成功
        Assert.IsNotNull(_gameFlowManager.CurrentState, "狀態機的 CurrentState 不應為 null。");
        Assert.IsInstanceOf<GameplayState>(_gameFlowManager.CurrentState, "初始狀態切換失敗，當前狀態並非 GameplayState。");

        // ==========================================
        // Act (執行)
        // ==========================================
        // 模擬玩家踩到出口，直接呼叫廣播，觸發房間過關事件
        BattleEventManager.TriggerRoomCleared();

        // ==========================================
        // Assert (驗證)
        // ==========================================
        // 斷言 GameFlowManager 的當前狀態是否已成功切換為 StageClearState
        Assert.IsInstanceOf<StageClearState>(_gameFlowManager.CurrentState, "觸發過關事件後，狀態機未能正確切換到 StageClearState。");
    }

    [TearDown]
    public void TearDown()
    {
        // ==========================================
        // 清理 (防止 Memory Leak 污染其他測試)
        // ==========================================

        // 1. 銷毀測試用的 GameObject，釋放資源 (Edit Mode 需使用 DestroyImmediate)
        if (_gameFlowManager != null && _gameFlowManager.gameObject != null)
        {
            Object.DestroyImmediate(_gameFlowManager.gameObject);
        }
        if (_uiManager != null && _uiManager.gameObject != null)
        {
            Object.DestroyImmediate(_uiManager.gameObject);
        }

        // 2. 利用反射清空 單例 Instance，確保下次測試能擁有乾淨的單例環境
        var instanceProperty = typeof(GameFlowManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        if (instanceProperty != null)
        {
            var setter = instanceProperty.GetSetMethod(true); // true 表示包含 private set
            setter?.Invoke(null, new object[] { null });
        }

        var uiInstanceProperty = typeof(UIManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        if (uiInstanceProperty != null)
        {
            var setter = uiInstanceProperty.GetSetMethod(true);
            setter?.Invoke(null, new object[] { null });
        }

        // 3. 確保靜態事件沒有殘留的訂閱者，防止記憶體洩漏與幽靈呼叫
        ClearStaticEvent(typeof(BattleEventManager), "OnRoomCleared");
        ClearStaticEvent(typeof(BattleEventManager), "OnPlayerDied");
    }

    /// <summary>
    /// 輔助方法：透過反射清空靜態事件的所有訂閱者
    /// </summary>
    private void ClearStaticEvent(System.Type type, string eventName)
    {
        // C# 編譯器在處理 event 時，會在背景產生一個與事件同名的 private 委派欄位
        var fieldInfo = type.GetField(eventName, BindingFlags.Static | BindingFlags.NonPublic);
        if (fieldInfo != null)
        {
            fieldInfo.SetValue(null, null);
        }
    }
}
