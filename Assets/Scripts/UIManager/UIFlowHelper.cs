using UnityEngine;

/// <summary>
/// 協助將 UI Button 點擊事件轉發給 GameFlowManager 進行狀態切換。
/// 
/// 2026-09-11 盤點結論：
/// - GoToHQ() 確認被 S0.unity（主選單場景）的按鈕 OnClick 使用
/// - StartGameplay()、GoToStageClear()、GoToGameEnd()、GoToMainMenu() 目前無場景綁定
/// - 全專案掃描（所有 .unity 和 .prefab）確認無其他 UnityEvent 綁定
/// - 保留所有方法不刪除，避免場景引用丟失
/// </summary>
public class UIFlowHelper : MonoBehaviour
{
    public void GoToMainMenu()
    {
        GameFlowManager.Instance.ChangeState(new MainMenuState());
    }

    public void GoToHQ()
    {
        GameFlowManager.Instance.ChangeState(new HQState());
    }

    /// <summary>
    /// 前往遊戲關卡。可以在此傳入房號，這裡示範預設為第一關。
    /// </summary>
    public void StartGameplay(int roomNumber = 1)
    {
        GameFlowManager.Instance.ChangeState(new TutorialState());
    }

    /// <summary>
    /// 前往結算畫面 (小結算)
    /// </summary>
    public void GoToStageClear(int roomNumber = 1)
    {
        GameFlowManager.Instance.ChangeState(new StageClearState(EncounterOutcome.Success));
    }

    /// <summary>
    /// 前往遊戲結束畫面 (總結算)
    /// </summary>
    public void GoToGameEnd(bool isWin = true)
    {
        GameFlowManager.Instance.ChangeState(new GameEndState(isWin));
    }
}
