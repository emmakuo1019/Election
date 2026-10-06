using UnityEngine;

/// <summary>
/// 音效測試工具 - 在 Inspector 中提供測試按鈕
/// 組員可以直接在場景中拖拉到 GameObject 上測試音效
/// </summary>
public class AudioTester : MonoBehaviour
{
    [Header("=== 音效測試工具 ===")]
    [Space(10)]
    
    [Header("背景音樂測試")]
    [Tooltip("點擊下方按鈕測試背景音樂")]
    public bool testBGM;

    [Header("音效測試")]
    [Tooltip("點擊下方按鈕測試各種音效")]
    public bool testSFX;

    [Space(20)]
    [Header("快速測試按鈕（在 Inspector 中使用）")]
    [Tooltip("測試：主選單/總部 BGM")]
    public bool playBGM_MenuAndHQ;
    
    [Tooltip("測試：關卡任務 BGM")]
    public bool playBGM_Mission;
    
    [Tooltip("測試：停止 BGM")]
    public bool stopBGM;

    [Space(10)]
    [Tooltip("測試：一般攻擊音效")]
    public bool playAttackNormal;
    
    [Tooltip("測試：成功轉化音效")]
    public bool playConvertSuccess;
    
    [Tooltip("測試：對手志工死亡音效")]
    public bool playEnemyVolunteerDeath;
    
    [Tooltip("測試：對手成功轉化音效")]
    public bool playEnemyConvertSuccess;
    
    [Tooltip("測試：土下座技能音效")]
    public bool playSkillDogeza;

    [Space(10)]
    [Header("音量調整測試")]
    [Range(0f, 1f)]
    public float testBGMVolume = 0.5f;
    
    [Range(0f, 1f)]
    public float testSFXVolume = 0.8f;

    private void Update()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[AudioTester] AudioManager 不存在！請確保場景中有 AudioManager。");
            return;
        }

        // BGM 測試
        if (playBGM_MenuAndHQ)
        {
            playBGM_MenuAndHQ = false;
            AudioManager.Instance.PlayBGM_MenuAndHQ();
            Debug.Log("▶ 播放：主選單/總部 BGM");
        }

        if (playBGM_Mission)
        {
            playBGM_Mission = false;
            AudioManager.Instance.PlayBGM_Mission();
            Debug.Log("▶ 播放：關卡任務 BGM");
        }

        if (stopBGM)
        {
            stopBGM = false;
            AudioManager.Instance.StopBGM();
            Debug.Log("⏹ 停止 BGM");
        }

        // SFX 測試
        if (playAttackNormal)
        {
            playAttackNormal = false;
            AudioManager.Instance.PlaySFX_AttackNormal();
            Debug.Log("🔊 播放：一般攻擊音效");
        }

        if (playConvertSuccess)
        {
            playConvertSuccess = false;
            AudioManager.Instance.PlaySFX_ConvertSuccess();
            Debug.Log("🔊 播放：成功轉化音效");
        }

        if (playEnemyVolunteerDeath)
        {
            playEnemyVolunteerDeath = false;
            AudioManager.Instance.PlaySFX_EnemyVolunteerDeath();
            Debug.Log("🔊 播放：對手志工死亡音效");
        }

        if (playEnemyConvertSuccess)
        {
            playEnemyConvertSuccess = false;
            AudioManager.Instance.PlaySFX_EnemyConvertSuccess();
            Debug.Log("🔊 播放：對手成功轉化音效");
        }

        if (playSkillDogeza)
        {
            playSkillDogeza = false;
            AudioManager.Instance.PlaySFX_SkillDogeza();
            Debug.Log("🔊 播放：土下座技能音效");
        }

        // 音量調整
        AudioManager.Instance.SetBGMVolume(testBGMVolume);
        AudioManager.Instance.SetSFXVolume(testSFXVolume);
    }

    private void OnGUI()
    {
        if (AudioManager.Instance == null) return;

        // 在遊戲畫面左上角顯示測試按鈕
        GUILayout.BeginArea(new Rect(10, 10, 250, 400));
        GUILayout.BeginVertical("box");

        GUILayout.Label("=== 音效測試面板 ===", GUI.skin.box);

        GUILayout.Space(10);
        GUILayout.Label("背景音樂：");
        if (GUILayout.Button("播放：主選單/總部 BGM"))
        {
            AudioManager.Instance.PlayBGM_MenuAndHQ();
        }
        if (GUILayout.Button("播放：關卡任務 BGM"))
        {
            AudioManager.Instance.PlayBGM_Mission();
        }
        if (GUILayout.Button("停止 BGM"))
        {
            AudioManager.Instance.StopBGM();
        }

        GUILayout.Space(10);
        GUILayout.Label("音效測試：");
        if (GUILayout.Button("一般攻擊"))
        {
            AudioManager.Instance.PlaySFX_AttackNormal();
        }
        if (GUILayout.Button("成功轉化"))
        {
            AudioManager.Instance.PlaySFX_ConvertSuccess();
        }
        if (GUILayout.Button("對手志工死亡"))
        {
            AudioManager.Instance.PlaySFX_EnemyVolunteerDeath();
        }
        if (GUILayout.Button("對手成功轉化"))
        {
            AudioManager.Instance.PlaySFX_EnemyConvertSuccess();
        }
        if (GUILayout.Button("土下座技能"))
        {
            AudioManager.Instance.PlaySFX_SkillDogeza();
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
