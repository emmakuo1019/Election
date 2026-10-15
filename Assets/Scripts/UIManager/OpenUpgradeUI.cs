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
            enabled = false;
            return;
        }

        if (upgradePanelUI == null)
        {
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
            tips.SetActive(true);
            isPlayerNearSwitch = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            tips.SetActive(false);
            isPlayerNearSwitch = false;
        }
    }
}
