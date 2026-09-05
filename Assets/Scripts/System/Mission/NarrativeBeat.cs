using UnityEngine;

/// <summary>
/// 劇情資料的最小單位。這個資料不參與任務判定或場景切換，
/// 只讓劇情導演決定要播放的對話、運鏡、Timeline 與角色動畫。
/// </summary>
[CreateAssetMenu(fileName = "NarrativeBeat", menuName = "Mission/Narrative Beat")]
public class NarrativeBeat : ScriptableObject
{
    [SerializeField] private string beatId;
    [SerializeField, TextArea(2, 5)] private string designerNote;
    [SerializeField] private TutorialStepData dialogue;
    [Tooltip("預留給 Cinemachine、Timeline 或動畫資料；由 NarrativeDirector 的實作解讀。")]
    [SerializeField] private UnityEngine.Object presentationAsset;

    public string BeatId => beatId;
    public string DesignerNote => designerNote;
    public TutorialStepData Dialogue => dialogue;
    public UnityEngine.Object PresentationAsset => presentationAsset;
}
