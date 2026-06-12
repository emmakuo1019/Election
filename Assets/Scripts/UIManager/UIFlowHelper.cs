using UnityEngine;

/// <summary>
/// 協助將 UI Button 點擊事件轉發給 GameFlowManager 進行狀態切換。
/// 掛載在按鈕或 UI Root 上，並在 Button 的 OnClick 中呼叫對應的方法。
/// </summary>
public class UIFlowHelper : MonoBehaviour
{
    public void GoToMainMenu()
    {
        GameFlowManager.Instance.ChangeState(new MainMenuState());
    }

    public void GoToCharacterSelect()
    {
        GameFlowManager.Instance.ChangeState(new CharacterSelectState());
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
        GameFlowManager.Instance.ChangeState(new GameplayState(roomNumber));
    }

    /// <summary>
    /// 前往結算畫面 (小結算)
    /// </summary>
    public void GoToStageClear(int roomNumber = 1)
    {
        GameFlowManager.Instance.ChangeState(new StageClearState(roomNumber));
    }

    /// <summary>
    /// 前往遊戲結束畫面 (總結算)
    /// </summary>
    public void GoToGameEnd(bool isWin = true)
    {
        GameFlowManager.Instance.ChangeState(new GameEndState(isWin));
    }
}
