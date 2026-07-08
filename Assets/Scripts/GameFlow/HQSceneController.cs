using UnityEngine;
using Unity.Cinemachine;

public class HQSceneController : MonoBehaviour
{
    public static HQSceneController Instance { get; private set; }

    [Header("Cinemachine 相機設定")]
    [Tooltip("請將 MaleCam 拖進此欄位")]
    [SerializeField] private CinemachineCamera vcamMale;
    
    [Tooltip("請將 FemaleCam 拖進此欄位")]
    [SerializeField] private CinemachineCamera vcamFemale;
    
    [Tooltip("請將 DeskCam 拖進此欄位")]
    [SerializeField] private CinemachineCamera vcamDesk;

    [Header("鏡頭池管理")]
    [Tooltip("將場景內所有與總部運鏡相關的鏡頭（男性、女性、桌面）拖進此陣列")]
    [SerializeField] private CinemachineCamera[] allCameras;

    [Header("技能設定")]
    [Tooltip("請放入要在總部提供玩家選擇的技能資料")]
    public SkillData[] availableSkills;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 防呆保護：如果開發者忘了手動指定 allCameras，自動綁定
        if (allCameras == null || allCameras.Length == 0)
        {
            allCameras = new CinemachineCamera[] { vcamMale, vcamFemale, vcamDesk };
        }
    }

    private void Start()
    {
        // 確保沒有任何遺漏，強制將所有權重重置一次
        // 為了確保一開始畫面不會亂跑，先聚焦在 Male 上
        FocusCandidate(true);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 強制重置模式 (Reset & Elevate)
    /// </summary>
    public void SwitchCamera(CinemachineCamera targetCamera)
    {
        // 簡單的防禦性檢查
        if (targetCamera == null) return;
        if (allCameras == null) return;

        // a. 先執行迴圈將所有相機 Priority 設為 10
        foreach (var cam in allCameras)
        {
            if (cam != null) 
            {
                cam.Priority.Value = 10;
            }
        }

        // b. 再將目標相機 Priority 設為 20
        targetCamera.Priority.Value = 20;
    }

    /// <summary>
    /// 根據參數決定切換到 vcamMale 或 vcamFemale
    /// </summary>
    public void FocusCandidate(bool isMale)
    {
        SwitchCamera(isMale ? vcamMale : vcamFemale);
    }

    /// <summary>
    /// 切換到桌面特寫
    /// </summary>
    public void FocusDesk()
    {
        SwitchCamera(vcamDesk);
    }
}
