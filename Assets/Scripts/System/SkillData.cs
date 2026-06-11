using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillData", menuName = "Election/SkillData", order = 1)]
public class SkillData : ScriptableObject
{
    [Header("基礎設定")]
    public string skillName;
    public float cooldown; // 用於冷卻計算
    
    [Header("動畫與表現")]
    public string animationTriggerName;
    public GameObject skillEffectPrefab;
    
    // 未來可依需求擴充，例如 damage、range 等
}
