using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 總部場景專用輸入處理器。
/// 掛載在 HQ 場景的任意 GameObject 上（建議掛在 HQSceneController 同一物件）。
///
/// 流程：
///   Step 1 選角  ─ A/D 切換男女，S 確認選角
///   Step 2 選技能 ─ A/D 切換技能選項，S 確認技能並出發
/// </summary>
public class HQInputHandler : MonoBehaviour
{
    // 讀取鍵盤狀態，直接用 Keyboard API 避免與 PlayerController 的 InputAction 衝突
    private Keyboard kb => Keyboard.current;

    // 防連按：每次輸入需要先放開再按
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

        // 左：只在剛按下時觸發一次
        if (leftDown && !leftHeld)
        {
            leftHeld = true;
            HQSceneController.Instance.OnNavigateLeft();
        }
        else if (!leftDown)
        {
            leftHeld = false;
        }

        // 右
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
        bool confirmDown = kb.sKey.isPressed || kb.downArrowKey.isPressed || kb.enterKey.isPressed;

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
