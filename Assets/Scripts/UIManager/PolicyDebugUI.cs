using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 政策與屬性 HUD 偵錯面板 (Policy & Stats Debug HUD)
/// 
/// 【功能說明】
/// 1. 讀取 GameDB.Instance.Run.ActiveCards，顯示當前持有的政策卡名稱。
/// 2. 讀取 GameDB.Instance.Run.Stats，顯示目前玩家的修正後屬性數值。
/// 3. 同步顯示誠信值 (HP)、資金 (MP)、選票與社會風氣，方便驗證數值變化。
/// 4. 支援自訂按鍵 (預設為 F3) 切換面板顯示/隱藏，避免與 WASD 移動中的 D 鍵衝突。
/// 
/// 【掛載與操作說明】
/// 1. 在場景中建立一個新的 GameObject（例如命名為 "DebugManager" 或 "PolicyDebugUI"）。
/// 2. 將此 PolicyDebugUI 腳本掛載到該 GameObject 上。
/// 3. 確保場景中已有 GameDB 實例（若無，請先啟動含有 GameDB 的引導場景如 S0 或 HQ）。
/// 4. 執行遊戲，按下 F3 鍵（或於 Inspector 自訂的按鍵）即可在畫面右下角切換顯示此偵錯面板。
/// </summary>
public class PolicyDebugUI : MonoBehaviour
{
    [Header("按鍵設定")]
    [Tooltip("切換偵錯面板顯示/隱藏的按鍵 (預設為 F3，以防與 WASD 移動的 D 衝突)")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F3;

    [Header("面板樣式")]
    [SerializeField] private float panelWidth = 320f;
    [SerializeField] private float panelHeight = 360f;
    [SerializeField] private float panelMargin = 15f;

    private bool isVisible = false;
    private GUIStyle richTextStyle;
    private Vector2 scrollPosition;

    private void Update()
    {
        // 偵測按鍵切換顯示狀態
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
        }
    }

    private void OnGUI()
    {
        if (!isVisible) return;

        // 初始化富文字樣式
        if (richTextStyle == null)
        {
            richTextStyle = new GUIStyle(GUI.skin.label);
            richTextStyle.richText = true;
        }

        // 計算右下角位置
        float xPos = Screen.width - panelWidth - panelMargin;
        float yPos = Screen.height - panelHeight - panelMargin;
        Rect rect = new Rect(xPos, yPos, panelWidth, panelHeight);

        // 繪製背景 Box
        GUI.Box(rect, "<b><color=cyan>【ELECTION 政策偵錯面板】</color></b>", new GUIStyle(GUI.skin.box) { richText = true });

        // 設定內容顯示範圍 (保留邊距)
        Rect contentRect = new Rect(rect.x + 10f, rect.y + 25f, rect.width - 20f, rect.height - 35f);
        GUILayout.BeginArea(contentRect);

        // 提示說明
        GUILayout.Label($"切換開關按鍵: <b><color=yellow>{toggleKey}</color></b>", richTextStyle);
        GUILayout.Label("----------------------------------------");

        if (GameDB.Instance == null || GameDB.Instance.Run == null)
        {
            GUILayout.Label("<color=red>警告: GameDB.Instance 或 RunData 為空！</color>", richTextStyle);
            GUILayout.EndArea();
            return;
        }

        // 使用 ScrollView 防止內容過多超出面板
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(contentRect.width), GUILayout.Height(contentRect.height - 40f));

        var run = GameDB.Instance.Run;

        // 1. 核心數值顯示 (HP, MP, Votes, Atmosphere)
        GUILayout.Label("<b>[ 核心狀態 ]</b>", richTextStyle);
        GUILayout.Label($"誠信值 (HP): {run.IntegrityHp:F1} / {run.MaxIntegrityHp:F1}");
        GUILayout.Label($"資金 (MP): {run.CurrentMP} / {run.MaxMP}");
        GUILayout.Label($"選票 (Player vs Opponent): {run.PlayerVotes} : {run.OpponentVotes} (得票率: {run.PlayerVotePercentage * 100f:F1}%)");
        GUILayout.Label($"社會風氣: {run.SocialAtmosphere} ({run.GetAtmosphereDescription()})");
        
        GUILayout.Label("----------------------------------------");

        // 2. 修正後屬性數值
        var stats = run.Stats;
        if (stats != null)
        {
            GUILayout.Label("<b>[ 玩家修正後屬性 (SSOT) ]</b>", richTextStyle);
            GUILayout.Label($"說服力 (Attack Influence): <color=lime>{stats.ModifiedAttackInfluence:F2}</color> (基: {stats.BaseAttackInfluence:F2})");
            GUILayout.Label($"移動速度 (Move Speed): <color=lime>{stats.ModifiedMoveSpeed:F2}</color> (基: {stats.BaseMoveSpeed:F2})");
            GUILayout.Label($"攻擊範圍 (Attack Range): <color=lime>{stats.ModifiedAttackRange:F2}</color> (基: {stats.BaseAttackRange:F2})");
            GUILayout.Label($"轉換機率 (Convert Chance): <color=lime>{stats.ModifiedConvertChance * 100f:F1}%</color> (基: {stats.BaseConvertChance * 100f:F1}%)");
            GUILayout.Label($"攻擊冷卻 (CD Delta): <color=lime>{stats.ModifiedAttackCooldown:F2}</color> (基: {stats.BaseAttackCooldown:F2})");
            GUILayout.Label($"全體NPC速度 (NPC Speed): <color=lime>{stats.ModifiedGlobalNpcSpeedMultiplier:F2}</color> (基: {stats.BaseGlobalNpcSpeedMultiplier:F2})");
            GUILayout.Label($"流失率 (Lose Rate): <color=lime>{stats.ModifiedLoseControlRate:F2}</color> (基: {stats.BaseLoseControlRate:F2})");
            GUILayout.Label($"擴散半徑 (Spread Radius): <color=lime>{stats.ModifiedSpreadRadius:F2}</color> (基: {stats.BaseSpreadRadius:F2})");
        }
        else
        {
            GUILayout.Label("<color=yellow>警告: PlayerStatsData 屬性資料為空！</color>", richTextStyle);
        }

        GUILayout.Label("----------------------------------------");

        // 3. 當前持有的政策卡
        GUILayout.Label("<b>[ 當前政策卡 (Active Cards) ]</b>", richTextStyle);
        var activeCards = run.ActiveCards;
        if (activeCards == null || activeCards.Count == 0)
        {
            GUILayout.Label("<i>(無持有任何政策卡)</i>", richTextStyle);
        }
        else
        {
            for (int i = 0; i < activeCards.Count; i++)
            {
                var card = activeCards[i];
                if (card != null)
                {
                    GUILayout.Label($"• {card.cardName} (<color=cyan>{card.faction?.factionName ?? "通用"}</color> | Rarity: {card.Rarity})");
                }
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}
