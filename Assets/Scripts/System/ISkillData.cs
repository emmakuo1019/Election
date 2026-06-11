using UnityEngine;

/// <summary>
/// 共通的技能資料介面。
/// 無論是一般戰鬥技能 (SkillData) 或大招 (PartySkillData)，
/// 只要實作此介面，就能共用同一個 SkillState 來執行播放邏輯。
/// </summary>
public interface ISkillData
{
    /// <summary>
    /// 用於觸發 Animator 的變數名稱
    /// </summary>
    string AnimationTriggerName { get; }

    /// <summary>
    /// 技能的冷卻時間
    /// </summary>
    float Cooldown { get; }

    /// <summary>
    /// 技能狀態應鎖定的持續時間 (動畫播放長度)
    /// </summary>
    float Duration { get; }

    /// <summary>
    /// 執行技能的具體邏輯 (例如：生成特效、扣除資源、造成傷害)
    /// </summary>
    /// <param name="caster">施放技能的玩家物件</param>
    void ExecuteSkill(GameObject caster);

    /// <summary>
    /// 每幀更新技能邏輯 (例如：持續位移、追蹤)
    /// </summary>
    /// <param name="caster">施放技能的玩家物件</param>
    /// <param name="deltaTime">幀間隔時間</param>
    void UpdateSkill(GameObject caster, float deltaTime);
}
