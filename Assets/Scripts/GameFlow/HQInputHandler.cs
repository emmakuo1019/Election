using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 總部場景專用輸入處理器。
/// 掛載在 HQSceneController 同一個 GameObject 上。
///
/// 操作：
///   A / ←   瀏覽上一個派系
///   D / →   瀏覽下一個派系
///   Space / Enter / J   確認選擇並出發
/// </summary>
public class HQInputHandler : MonoBehaviour
{
    private Keyboard kb => Keyboard.current;

    // 防連按：按鍵需先放開才能再觸發
    private bool leftHeld;
    private bool rightHeld;
    private bool confirmHeld;

    private void Update()
    {
        if (kb == null || HQSceneController.Instance == null) return;

        HandleLeftRight();
        HandleConfirm();
    }

    private void HandleLeftRight()
    {
        bool leftDown  = kb.aKey.isPressed || kb.leftArrowKey.isPressed;
        bool rightDown = kb.dKey.isPressed || kb.rightArrowKey.isPressed;

        if (leftDown && !leftHeld)
        {
            leftHeld = true;
            HQSceneController.Instance.OnNavigateLeft();
        }
        else if (!leftDown)
        {
            leftHeld = false;
        }

        if (rightDown && !rightHeld)
        {
            rightHeld = true;
            HQSceneController.Instance.OnNavigateRight();
        }
        else if (!rightDown)
        {
            rightHeld = false;
        }
    }

    private void HandleConfirm()
    {
        // Space、Enter、J 三鍵都可確認——符合玩家直覺，J 同時建立「J = 執行」的肌肉記憶
        bool confirmDown = kb.spaceKey.isPressed
                        || kb.enterKey.isPressed
                        || kb.jKey.isPressed;

        if (confirmDown && !confirmHeld)
        {
            confirmHeld = true;
            HQSceneController.Instance.OnConfirm();
        }
        else if (!confirmDown)
        {
            confirmHeld = false;
        }
    }
}
