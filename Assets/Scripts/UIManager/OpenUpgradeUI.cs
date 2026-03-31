using UnityEngine;

public class OpenUpgradeUI : MonoBehaviour
{
    [Header("互動提示")]
    [SerializeField] private GameObject tips;

    [Header("升級 UI")]
    [SerializeField] private UpgradePanelUI upgradePanelUI;

    [Header("互動設定")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private bool isPlayerNearSwitch;

    void Start()
    {
        if (tips == null)
        {
            Debug.LogError("tips 未指定！");
            enabled = false;
            return;
        }

        if (upgradePanelUI == null)
        {
            Debug.LogError("upgradePanelUI 未指定！");
            enabled = false;
            return;
        }

        tips.SetActive(false);
    }

    void Update()
    {
        if (isPlayerNearSwitch &&
            tips.activeSelf &&
            Input.GetKeyDown(interactKey) &&
            !upgradePanelUI.IsOpen)
        {
            upgradePanelUI.OpenPanel();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("玩家已進入區域！");
            tips.SetActive(true);
            isPlayerNearSwitch = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("玩家離開");
            tips.SetActive(false);
            isPlayerNearSwitch = false;
        }
    }
}