## 架構守則
1. 資料單一來源 (SSOT)：所有跨場景存活的數值嚴禁存在 Manager 中，必須在 GameDB.cs 註冊。絕對確保遊戲狀態大一統。
2. 事件驅動 (Event-Driven)：UI 一律透過訂閱事件更新，嚴禁在 Update() 裡面輪詢。
3. Proxy 模式：若現有 Manager 需保留，它只能作為 Proxy 轉接，不可存放狀態。
4. 資料防呆：所有增減數值的方法必須包含邊界檢查（如除以零保護、負數防禦）。
5. 模組化積木 (ScriptableObject)：所有政策卡、技能必須採用 Strategy Pattern 的 `ICardEffect` 介面積木設計。開發時不可寫死邏輯，需將 Proc（條件觸發）與數值效果拆分為獨立腳本，透過 Inspector 進行積木式組裝以拓展 Build 深度。

# Ponytail, lazy senior dev mode <必須遵守>

You are a lazy senior developer. Lazy means efficient, not careless. The best code is the code never written.

Before writing any code, stop at the first rung that holds:

1. Does this need to be built at all? (YAGNI)
2. Does it already exist in this codebase? Reuse the helper, util, or pattern that's already here, don't re-write it.
3. Does the standard library already do this? Use it.
4. Does a native platform feature cover it? Use it.
5. Does an already-installed dependency solve it? Use it.
6. Can this be one line? Make it one line.
7. Only then: write the minimum code that works.

The ladder runs after you understand the problem, not instead of it: read the task and the code it touches, trace the real flow end to end, then climb.

Bug fix = root cause, not symptom: a report names a symptom. Grep every caller of the function you touch and fix the shared function once — one guard there is a smaller diff than one per caller, and patching only the path the ticket names leaves a sibling caller still broken.

Rules:

- No abstractions that weren't explicitly requested.
- No new dependency if it can be avoided.
- No boilerplate nobody asked for.
- Deletion over addition. Boring over clever. Fewest files possible.
- Shortest working diff wins, but only once you understand the problem. The smallest change in the wrong place isn't lazy, it's a second bug.
- Question complex requests: "Do you actually need X, or does Y cover it?"
- Pick the edge-case-correct option when two stdlib approaches are the same size, lazy means less code, not the flimsier algorithm.
- Mark deliberate simplifications that cut a real corner with a known ceiling (global lock, O(n²) scan, naive heuristic) with a `ponytail:` comment naming the ceiling and upgrade path.

Not lazy about: understanding the problem (read it fully and trace the real flow before picking a rung, a small diff you don't understand is just laziness dressed up as efficiency), input validation at trust boundaries, error handling that prevents data loss, security, accessibility, the calibration real hardware needs (the platform is never the spec ideal, a clock drifts, a sensor reads off), anything explicitly requested. Lazy code without its check is unfinished: non-trivial logic leaves ONE runnable check behind, the smallest thing that fails if the logic breaks (an assert-based demo/self-check or one small test file; no frameworks, no fixtures). Trivial one-liners need no test.

(Yes, this file also applies to agents working on the ponytail repo itself. Especially to them.)


---

# Election（酸宗痛）專案說明文件

> 供 AI 助理優先讀取，用於了解遊戲的製作方向與規範。
> 最後更新：2026-07-02（合併 KIRO_PROJECT_CONTEXT 並導入 GameDB SSOT 架構）

## 專案概述

- **遊戲名稱**：酸宗痛
- **引擎**：Unity 6000.3.17f1（URP 渲染管線，C#）
- **開發工具**：Unity、Rider、Maya、CSP
- **遊戲類型**：Roguelite 選舉主題動作遊戲
- **目標平台**：PC / Steam
- **遊玩人數**：1 人
- **遊戲時長**：約 15 分鐘
- **目標客群**：15～30 歲，對戲謔諷刺有興趣的玩家
- **開發階段**：Demo 製作中（大三下完成核心機制展示）

### 核心主題與玩法收斂（三環結構配重）
以「將政治博弈動作化」為核心，結合 Roguelite 成長感與政治選舉的即時性。透過戲謔荒謬的美術風格處理台灣政治認同議題，引導玩家反思選民決策與社會撕裂。

> **重構設計更新：減法與配重**
> 為了避免核心未收斂並增加 Build 深度，遊戲核心玩法已收斂為以下三環結構：
> - **戰技（核心重心）**：房間內的即時戰鬥「拔除主動打牌機制」。玩家的注意力 100% 集中於精準走位、閃避記者干擾、以及施放基礎拜票/演說技能。
> - **戰術（外圍輔助）**：政策卡從「動詞（主動動作）」轉變為「形容詞/副詞（被動外掛）」。卡牌的選擇移至房間外的「過場/商人/結算」階段進行配置（類似《黑帝斯》的祝福機制）。
> - **戰略（資源管理）**：所有的遊戲資源收斂於「誠信值 (Integrity)」與「資金/選票」的單一矛盾管理。

---

## 操作方式

| 按鍵                   | 功能             |
|----------------------|----------------|
| WASD                 | 移動             |
| L Shift / xbox把手 右板機 | 衝刺（Dash）       |
| 空白鍵 / xbox把手 X鍵      | 普通攻擊（演說）       |
| J / K / L            | 技能（技能一、技能二、大招） |
| A / B / Y            | 技能（xbox把手）       |
| Esc                  | 選單             |

---

## 遊戲大流程（依固定 8 節點）

```
開始畫面 → 主選單 → 前導劇情 → 玩家競選總部
  ↓
教學關卡（Tutorial，不計入正式節點）
  ↓
節點 1（Opening，固定開場關卡）
  ↓
節點 2-3（MissionChoice，二選一任務）
  ├─ 任務成功 → 獎勵三選一（正常政策卡）
  └─ 任務失勢 → 獎勵三選一（普通卡+中性保底）
       ↓
       [是否誠信歸零？]
         是 → 結局 D：退選
         否 → 顯示雙門，選擇下一關
  ↓
節點 4（Elite，精英戰）
  ├─ 任務成功 → 獎勵三選一
  └─ 同場景顯示雙門，選擇節點 5
  ↓
節點 5-7（MissionChoice，二選一任務）
  ├─ 同節點 2-3 流程
  └─ 誠信歸零可能觸發退選
  ↓
節點 8（FinalBoss，最終決戰）
  ├─ 將指定的對手參選人生命值降至 0 → 成功
  └─ 玩家誠信生命歸零 → 失敗
       ↓
       結局（見下方結局系統）
```

### 流程狀態機 (GameFlow States)
- `BootState`：重置戰役資料
- `MainMenuState`：主選單
- `HQState`：總部派系選擇
- `TutorialState`：教學關卡（不計入正式節點）
- `GameplayState`：統一處理所有戰鬥關卡（Opening / MissionChoice / Elite / FinalBoss）
- `StageClearState`：統一結算入口，記錄結果、等待選卡、處理雙門或自動前進
- `GameEndState`：結局畫面

**已移除的狀態**：
- `BossBattleState`（Boss 戰已統一由 `GameplayState` 處理）
- `SafeRoomState`（不再有安全房繞路機制）

### 結局系統
| 結局 | 條件 | 畫面描述 |
|------|------|----------|
| 結局 A（政黨至上） | 社會風氣偏情緒下勝選 | 電視呈現勝選，周遭有競選小物、公仔 |
| 結局 B（政見至上） | 社會風氣偏理性下勝選 | 電視呈現勝選畫面 |
| 結局 C | 敗選（票數落後） | 電視呈現敗選畫面 |
| 結局 D | 誠信歸零 / 資金歸零 → 退選 | 電視呈現退選，選舉傳單在垃圾桶裡 |

---

## 遊戲流程與關卡結構

- **Demo 流程長度**：全程固定為 **8 個節點**。
  - **節點 1**：固定開場關卡（Opening）
  - **節點 2-3**：二選一任務（MissionChoice）
  - **節點 4**：精英戰（Elite），完成後在 Elite 場景雙門選第 5 關
  - **節點 5-7**：二選一任務（MissionChoice）
  - **節點 8**：最終 Boss（FinalBoss），完成後觸發結局
  
- **戰役定義系統（CampaignDefinition）**：
  - 所有節點由 `CampaignDefinition.asset` 統一定義，包含節點角色、固定任務、專用場景、劇情插入點
  - 二選一節點的任務由 `MissionPool` 以固定種子抽取，保證可重現性
  - 任務選項一旦生成即保存於 `CampaignData.PendingOptions`，UI 重載或重碰門不會重新抽選
  
- **岔路選擇機制**：
  - 二選一節點在獎勵選取後動態生成雙門（由 `RouteDoorSpawner` 管理）
  - 門只回報選項索引（`OnRouteSelected`），不直接寫入戰役資料
  - Elite（第 4 關）在同場景完成後顯示雙門，不切換場景
  - 玩家走進門觸發選擇，`Campaign.TrySelectRoute()` 寫入選定任務並載入下一關
  
- **任務系統（Mission System）**：
  - 任務類型：`EliminateAll`（消滅對手）、`ReachVotePercent`（達到 X% 票倉）、`Survive`（生存 N 秒）
  - 任務資料由 `RoomMissionData` ScriptableObject 定義（含類型、目標值、圖示、任務簡報與可出現節點）；抽取權重屬於 `MissionPool.Entry`
  - 任務結果使用 `EncounterResult` 記錄成功／失勢、任務資料與節點，取代靜態 `MissionTracker.LastResult`
  - 進場時由 `GameplayState` 自動顯示任務簡報對話框（復用 `TutorialDialogueUI`）

- **關卡生命週期**：
  - `Loading` → `Briefing` → `Active` → `ObjectiveResolved` → `RewardSelection` → `RouteSelection` → `Transitioning`
  - 每個階段只能進入一次，由 `BattleEventManager.EncounterPhase` 追蹤當前階段
  - `EliminateAll` 不啟動倒數；`Survive` 時間到成功；`ReachVotePercent` 在時限內達標成功、時間到記為失勢

- **獎勵系統**：
  - 成功：正常三選一政策卡（含稀有卡）
  - 失勢：三張普通卡，至少一張中性可用卡，其餘可偏離流派（ponytail: 需 playtest 後調整）
  - 失勢會記錄 `EncounterResult`，並寫入劇情紀錄供後續引用

- **劇情預留**：
  - 每個節點可配置 `onEnterBeat`、`onSuccessBeat`、`onFailureBeat`
  - Elite 節點額外有 `afterEliteBeat`，FinalBoss 節點有 `finalEndingBeat`
  - 由 `NarrativeDirector` 統一播放（ponytail: 預留邊界，待美術資源與劇本完成後實作）

| 房間類型 | 生成物件 | 時限 |
|----------|----------|------|
| 普通房間（節點 2-3, 5-7） | 依 MissionPool 抽取任務定義 | 依任務類型 |
| Elite（節點 4） | 選民 12、地方人士 0、對手志工 2 | EliminateAll，無時限 |
| FinalBoss（節點 8） | 對手參選人 1（目前 HP 30，可調整） | 無倒數；擊敗對手即勝 |

**ponytail 技術債標記**：
- Elite（第 4 關）目前使用 `TestMVP` 場景，因原 `TestSmallBoss` 缺少 `MissionTracker`、`RewardItemSpawner`、`BattleFlowController` 等必要組件。
- 若未來替換專用精英場景，需保留上述組件並在場景內放置雙門生成點。

---

## 核心系統

### 1. 選票系統（VoteManager）
- 預設雙方各 50 票。（資料存於 GameDB）
- 選民被轉化時票數即時更新；一般搶票任務仍可依 `ReachVotePercent` 以限時達標判定。
- FinalBoss 不使用選票或計時器判定：場景內唯一勾選 `isFinalBossOpponent` 的對手生命值歸零時，
  `BattleEventManager.OnFinalBossDefeated` 才會結算勝利。護衛或未來召喚物不能勾選此欄位。

### 2. 誠信值（HP）
- 預設值 70 / 100（存放於 GameDB 中）。
- **歸零邏輯**：
  - 誠信歸零後，深色選民開始「燃燒」（每秒減少），燃燒完才真正觸發退選（結局 D）。
  - 形象越差（低於 20%～1%）→ 敵人血量提升 10%～50%。
- 在記者監視範圍內使用**情緒政策**會扣除誠信。
- 誠信值比例影響選民的基礎 HP。

### 3. 資金（MP）
- 用於施放技能、造勢大招。（資料存於 GameDB）
- **補充方式**：
  - 成功轉化選民回補
  - 特殊房間回補
  - 每房結算依得票率回補（保底恢復 20）
- 資金歸零 → 結束行動（觸發結局 D）。

### 4. 社會風氣（SocialAtmosphere）
- **情緒風氣**：深色選民生成率 +10%～70%，支持者穩定性低（高風險高報酬）。
- **理性風氣**：深色選民生成率 +10%～30%，支持者穩定性高（適合長線經營）。
- 影響最終結局類型（A 或 B）。
- 由政策卡的 `socialClimateDelta` 調整。

### 5. 演說攻擊（PlayerAttack）
- 扇形範圍攻擊（`attackRange = 3f`，`attackAngle = 60°`）。
- 攻擊方向跟隨玩家最後移動方向。
- `attackInfluence`：每次演說對選民 `currentPosition` 的影響值。
- `convertChance = 0.3f`：普通選民轉化機率（可被政策卡加成）。
- `darkVoterConvertChance = 0.8f`：Dark（深色）選民的轉化機率。
- **Dark 屬性選民不會被普通演說範圍影響**（需特殊手段）。
- 消耗 MP（資金）施放。

### 6. 技能系統（PlayerSkillManager）與技能解鎖節奏 (Pacing)
技能系統作為玩家的主動「戰技」，採用**強制進度節點**解鎖，與政策卡抽取機制完全分離。

- **解鎖節奏 (Pacing)**：
  - **第 2 關**：必定解鎖基礎技能。
  - **第 5 關前**：必定保底出現第二技能解鎖選項。
- 每次解鎖提供二選一，讓玩家自由搭配風格。

| 技能階級 | 技能鍵 | 描述與範例 |
|----------|--------|------------|
| 技能一 | J | 基礎技能，如：煽動情緒（情緒版）或政策論述（理性版） |
| 技能二 | K | 進階技能，如：側翼出擊（情緒版）或發表白皮書（理性版） |
| 大招 | L | 強力技能，如：群眾造勢（情緒版）或說明會（理性版） |

- 政黨技能（`PartySkillData`）有冷卻時間（`baseCooldown`）與資源消耗。
- 目前程式中現有實作範例：`DogezaSkill`（土下座）——轉換 Cold 屬性選民的特殊技能。

### 7. 政策卡系統（PolicyCard / 被動祝福機制）
通關房間後，場景內出現三個獎勵物件，玩家靠近按 E 選擇。
**重構聲明：政策卡不再是戰鬥中可主動施放的「技能」，而是作為「被動外掛/祝福」存在，用以建構深度的流派 (Build)。**

#### 獎勵選卡機制（場景內互動，Cult of the Lamb 風格）
- **敵人全滅後**，場景中段生成三個獎勵物件（3D 底座 + Billboard 卡面 Sprite）。
- 玩家靠近物件顯示 `RewardDescriptionUI`（置中卡名 + 說明 + 互動提示）。
- **按 E 選擇**：套用卡牌效果，其餘物件消失，觸發 `TriggerRewardCollected`，由 `StageClearState` 進入下一步。
- **不按類別篩選獎勵**：卡池完全隨機，保留殺戮尖塔的 Build 建立挑戰性。

#### 卡牌稀有度與視覺設計
- 稀有度分為 `Common`（普通）、`Rare`（稀有）、`Legendary`（傳說），對應不同卡框視覺。
- 每張卡有獨立 `cardArtwork` Sprite 作為卡面主圖。
- **派系歸屬**改以 `FactionData faction`（可 null = 通用）取代舊有 `CardType` 分類，保留低調標記但不影響卡面主視覺，避免玩家一眼判斷跳過思考。

#### 程式實作邏輯與 Build 流派
未來所有政策卡以**派系（FactionData）**作為 Build 流派歸屬基礎：
- 不同派系有獨立牌池，獎勵階段從所有派系牌池混合隨機出卡。
- 派系目前為「陳派」與「柯派」，未來依企劃調整。

**程式架構支援**：
1. **條件觸發機制 (Proc)**：觸發器 (`IProcTrigger`) 與效果器分離，支援「完美閃避後觸發」等條件。
2. **風氣連動**：卡牌的 `socialClimateDelta` 動態調整全域社會風氣，影響選民 AI 行為。

*(開發提醒：請善用 `ICardEffect` 介面，所有設計遵循 GameDB SSOT 修改數值。)*

### 8. 戰役定義系統（CampaignDefinition）
戰役定義系統將整個 Demo 流程的 8 個節點、Boss 節點、劇情插入點統一管理，取代舊的 Block 系統。

#### 核心組件
- **`CampaignDefinition` (ScriptableObject)**：
  - 定義 8 個節點的角色（Opening / MissionChoice / Elite / FinalBoss）
  - 固定節點（1、4、8）指定專用任務或場景
  - 二選一節點（2-3、5-7）留空，由 `MissionPool` 抽取
  - 每個節點可配置進場、成功、失敗、Elite 後、FinalBoss 結束的 `NarrativeBeat`
  - 指定雙門 Prefab（`routeDoorPrefab`）供 `RouteDoorSpawner` 使用

- **`CampaignData` (Runtime)**：
  - 存放於 `GameDB.Campaign`，DontDestroyOnLoad 跨場景保存
  - `CurrentNodeNumber`：當前節點（1-8）
  - `CurrentRole`：當前節點角色（Opening / MissionChoice / Elite / FinalBoss）
  - `ActiveRoom`：當前已選定的任務與場景
  - `PendingOptions`：雙門的兩個待選任務（生成後保存，不重抽）
  - `Results`：`List<EncounterResult>`，記錄所有節點的任務結果

- **`EncounterResult` (Struct)**：
  - `nodeNumber`：節點編號
  - `mission`：本次使用的 `RoomMissionData`（FinalBoss 可為空）
  - `outcome`：`EncounterOutcome.Success` / `EncounterOutcome.Failed`
  - `usedDistressReward`：是否使用失勢獎勵

#### 流程邏輯
1. **教學完成後**：`Campaign.StartFormalCampaign()` 啟動節點 1
2. **任務結算後**：`StageClearState` 呼叫 `Campaign.ResolveCurrentEncounter(outcome)` 記錄結果
3. **獎勵選取後**：`Campaign.TryPrepareNextStep()` 判斷下一步：
   - 節點 1–7 依下一個節點決定固定前進或二選一；FinalBoss 的結果由 `StageClearState` 直接進入結局，沒有獎勵或選路。
   - 若為二選一節點 → `needsRouteChoice = true`，生成雙門
   - 若為固定節點 → 直接啟動下一節點，自動前進
4. **雙門選擇後**：`Campaign.TrySelectRoute(optionIndex)` 寫入選定任務，切換到 `GameplayState`

#### 種子系統
- 使用 `CampaignDefinition.DefaultSeed` 作為本局種子
- 任務抽取時傳入 `seed + nodeNumber`，確保同種子同節點抽出相同任務
- 可重現任務選項、測試固定流程

#### 驗證機制
- `CampaignDefinition.IsValid()`：啟動前檢查節點完整性、角色正確性、固定任務是否存在
- `HasValidMissionChoices()`：檢查每個二選一節點（2-3、5-7）是否有至少 2 個有效任務
- 所有驗證失敗會阻止戰役啟動並輸出明確錯誤訊息

#### 與舊系統的差異
| 舊系統 | 新系統 |
|--------|--------|
| Block 1/2/3（每個 5 關） | 固定 8 節點（依定義） |
| `CompletedBlocks`、`RoomSequence` | `CurrentNodeNumber`、`Results` |
| `StartRandomBlock()`、`TryCompleteCurrentBlock()` | `StartFormalCampaign()`、`TryPrepareNextStep()` |
| 第 4 關 = BossBattleState | 第 4 關 = Elite，第 8 關 = FinalBoss |
| 安全房 20% 隨機出現 | 已移除，只有固定節點與二選一 |
| `OnRoomCleared` 同時代表任務完成、領獎、選路、換場 | 拆分為 `OnObjectiveResolved`、`OnRewardCollected`、`OnRouteSelected` |

#### 獎勵選卡機制（場景內互動，Cult of the Lamb 風格）
- **敵人全滅後**，場景中段生成三個獎勵物件（3D 底座 + Billboard 卡面 Sprite）。
- 玩家靠近物件顯示 `RewardDescriptionUI`（置中卡名 + 說明 + 互動提示）。
- **按 E 選擇**：套用卡牌效果，其餘物件消失，觸發 `TriggerRewardCollected` 進入結算。
- **不按類別篩選獎勵**：卡池完全隨機，保留殺戮尖塔的 Build 建立挑戰性。

#### 卡牌稀有度與視覺設計
- 稀有度分為 `Common`（普通）、`Rare`（稀有）、`Legendary`（傳說），對應不同卡框視覺。
- 每張卡有獨立 `cardArtwork` Sprite 作為卡面主圖。
- **派系歸屬**改以 `FactionData faction`（可 null = 通用）取代舊有 `CardType` 分類，保留低調標記但不影響卡面主視覺，避免玩家一眼判斷跳過思考。

#### 程式實作邏輯與 Build 流派
未來所有政策卡以**派系（FactionData）**作為 Build 流派歸屬基礎：
- 不同派系有獨立牌池，獎勵階段從所有派系牌池混合隨機出卡。
- 派系目前為「陳派」與「柯派」，未來依企劃調整。

**程式架構支援**：
1. **條件觸發機制 (Proc)**：觸發器 (`IProcTrigger`) 與效果器分離，支援「完美閃避後觸發」等條件。
2. **風氣連動**：卡牌的 `socialClimateDelta` 動態調整全域社會風氣，影響選民 AI 行為。

*(開發提醒：請善用 `ICardEffect` 介面，所有設計遵循 GameDB SSOT 修改數值。)*

---

## 選民系統（Voter）

### 一般選民
*立場光譜(我方----\----對手，基礎左右各5格)*
- 範圍：-5（完全敵對）到 +5（完全支持玩家）
- 達到 ±5 才算完全轉化，計入選票。

選民是玩家獲取勝選的基石，然而每位選民的立場與柔軟度各異。作為參選人，玩家的核心使命在於洞察不同『標籤』背後的選民訴求，透過精準的政策投其所好，將游離的中間選民轉化為堅實的選票支持。

### 標籤系統 (VoterLabel)

| | #理性固化 | #情緒共振 |
|-- | -------- | -------- |
| 弱點標記 | 政見 | 情緒動員 |

> 打連擊的概念（轉化成功可短時間加速度）

- `VoterLabel`：程式碼對應 `Rational`（理性）或 `Emotion`（情緒）。
- `VoterAttribute`：程式碼對應 `None`、`Cold`（冷感）、`Dark`（深色）。

### 屬性：深色選民＊＊
政治立場極度固化的群體。雖然是玩家穩固的鐵票來源，卻也具備極高的情緒敏感度。一旦遭遇對手惡意抹黑，其產生的強烈情緒波動將導致嚴重的倒戈危機，是戰場上最難預測的雙面刃。

深色選民的行為

> #### 盲目性
> 這類選民完全無視你的數值，只要你的 **「政黨顏色」對了**，或是單純討厭對手的顏色，他都是屬於你的選票；反之，若顏色不對，即便你擁有對應政策，他也完全不動搖。
> #### 黑粉
> 深色選民看似是玩家的鐵票，但他們 **「極不穩定」**。如果對手釋放一個強大的「負面抹黑」，深色選民會比理性選民更快倒戈。

*(程式實作提醒：被玩家轉化後會跟隨玩家移動 `ShouldFollowPlayer = true`，移動速度較快 `darkMoveSpeed = 2f`。誠信歸零後開始「燃燒」，每秒減少，清零才結局 D。普通演說無法影響深色選民)*

### 屬性：冷感選民＊＊
被攻擊後會「退後 / 閃避 」
必須到一定的攻擊量

### 選民狀態機 (Voter States)
狀態類別對應：
- `VoterIdleState`（閒置）：靜止等待，持續掃描周圍決定下一行為。
- `VoterWanderState`（徘徊）：在場地內隨機遊走。
- `VoterFollowState`（跟隨玩家）：被轉化後跟隨玩家移動。
- `VoterHitState`（受擊）：被攻擊時的短暫受擊反應。
- `VoterStunState`（暈眩）：被暈眩技能命中後的無法行動狀態。
- `VoterCheerState`（歡呼）：完全轉化後的慶祝狀態。
- `VoterApatheticState`（冷感逃跑）：冷感屬性選民專用，偵測到玩家靠近時加速逃離，距離足夠後回到 Idle。
- `VoterWaverState`（動搖）：立場搖擺中，速度減半、停止尋路並顯示問號表情，對冷感選民仍會觸發逃跑邏輯。

以上狀態由 `VoterLogic` 內建的 `StateMachine` 進行切換與管理。

---

## 敵人設計

| 敵人 | 類型 | 行為 / 機制 |
|------|------|------------|
| 對手志工 | 阻礙者 | 巡視選區、鞏固選民立場、短暫暈眩玩家 |
| 記者 | 攻擊型 | HP=10（只對普攻有效），玩家在其範圍內使用情緒政策 → 扣誠信 |
| 地方人士 | 獎勵型 | 中立立場，攻下後掉落資金 +20、選票 +20 |
| 對手參選人（Boss） | Boss | 會使用技能：法條彈幕、激進口號、負面抹黑；彈幕同時扣誠信與得票 |

**Boss 勝利條件**：
- 將帶有 `isFinalBossOpponent` 標記的對手參選人生命值降至 0。
- 玩家生命值歸零仍是失敗；最終戰不使用倒數或票數比較。

### 敵人狀態機架構 (Enemy AI States)
目前已實作一套符合 SOLID 原則、基於純 C# 介面的基礎敵人 AI 狀態機：
- `EnemyController`：作為核心控制器，管理 `NavMeshAgent` 與 `Animator`，並透過 `targetLayer` (選民 Layer) 搭配 `Physics.OverlapSphere` 進行動態索敵。具備 GC 優化（預先實例化所有狀態）與遲滯區間（`escapeRange`）設計。
- 具體狀態包含：
  - `EnemyIdleState`：停止移動、播放 Idle 動畫，並持續發射隱形球體掃描周圍目標。
  - `EnemyMoveState`：恢復導航並追擊目標，若進入 `attackRange` 則攻擊，若目標逃脫超過 `escapeRange` 則放棄追擊。
  - `EnemyAttackState`：鎖死移動、面向目標並觸發 Attack 動畫。加入攻擊計時器（攻擊時長與後搖），結束後重新評估狀態。

---

## 玩家狀態機（PlayerState 圖）

```
Idle（閒置）
  ├─ WASD → Move（移動）
  ├─ Attack → Attack（攻擊）
  ├─ L Shift → Dash（衝刺）
  ├─ JKL → Skill（技能選單：技能一 / 技能二 / 大招）
  └─ 被對手技能攻擊 → Stun（暈眩）

Move（移動）
  ├─ Attack → Attack（攻擊）
  └─ L Shift → Dash（衝刺）→ 回到 Move 或 Idle

Attack（攻擊）
  ├─ 攻擊完畢 → Move 或 Idle
  └─ JKL → Skill（技能）

Skill（技能）
  ├─ JKL 選擇技能一 / 技能二 / 大招 → 執行後回到 Idle
  └─ Attack → Attack

Dash（衝刺）
  └─ L Shift 再按 → 回到 Move（企劃書中 Shift 可從 Move 和 Idle 直接進入）

Stun（暈眩）
  └─ 時間到 → 回到 Idle
```

狀態類別對應：`IdleState`、`MoveState`、`AttackState`、`SkillState`、`DashState`、`StunState`。
以上狀態均實作 `IState` 介面（非繼承 MonoBehaviour），包含 `Enter`、`Update`、`PhysicsUpdate`、`Exit` 生命週期。
狀態切換邏輯由獨立的純 C# 類別 `StateMachine` 管理，並由 `PlayerController`（負責元件依賴與輸入擷取）在內部進行組合（Composition）與初始化。

---

## 場景列表

| 場景名稱 | 用途 |
|----------|------|
| `S0` | 開始畫面 / 主選單 |
| `S1` | 前導劇情 |
| `headquarters` | 競選總部（區塊間休息、技能選擇、繼承資源） |
| `TestMVP` | 普通 Battle 關卡（主要開發場景） |
| `TestSpecial` | 特殊房間（恢復資金或誠信，20% 機率出現） |
| `TestSmallBoss` | 小 Boss 測試場景 |
| `particalTest` | 粒子特效測試 |
| `endGamePanel` | 遊戲結束 / 結局畫面 |

---

## 程式架構規範

### 資料夾結構
```
Assets/Scripts/
├── Player/          # 玩家控制、攻擊、技能、狀態機
│   └── StateMachine/  # Idle / Move / Dash / Attack / Skill / Stun 狀態
├── Voter/           # 選民邏輯、資料、外觀
├── System/          # 核心系統（GameDB、VoteManager、BattleFlowController 等）
│   ├── Mission/     # 任務系統（RoomMissionData、MissionTracker、MissionPool、DoorController 等）
│   └── Reward/      # 場景內獎勵系統（RewardItem、RewardItemSpawner、RewardDescriptionUI）
├── UIManager/       # UI 元件
├── Effects/         # 特效、物件池相關
├── Enemy/           # 敵方 AI
├── Tutorial/        # 教學對話系統（TutorialManager、TutorialDialogueUI、TutorialTipsUI）
├── ScenesManager/   # 場景切換管理
└── RandomEvents/    # 隨機事件（未開發）
```

### 程式規範
- **語言**：C#，Unity 6000.x 系列。
- **資料單一來源 (SSOT)**：全域狀態存放在 `GameDB.cs` 中，不依賴於 Manager 儲存全域數值，確保跨場景資料的一致性與防呆。
- **Singleton 模式**：主要系統使用 `Instance` 靜態屬性，`Awake` 中防止重複建立。若 Manager 仍存在則主要作為 Proxy。
- **事件系統**：使用 C# `event Action` / `event delegate` 解耦系統間通訊。UI 必須訂閱事件更新，嚴禁在 Update 輪詢。
- **資料與邏輯分離**：資料層使用 `ScriptableObject`（`VoterConfig`、`PolicyCardData`、`PartySkillData`、`CampaignDefinition`）或資料元件（`VoterData`），邏輯層獨立（`VoterLogic`）。
- **物件池**：特效使用 `PoolManager`（`AutoReturnToPool`、`PooledParticleInstance`）。
- **進度儲存**：戰役進度完全存放於 `GameDB.Campaign`（`CampaignData`），跨場景不銷毀。不再使用 `PlayerPrefs` 儲存進度。
- **注釋語言**：繁體中文。

### 命名習慣
- 類別：PascalCase
- 私有欄位：camelCase，加 `[SerializeField]`
- 靜態常數：PascalCase

---

## 目前已知待辦 / 尚未完成
- `RandomEvents`：目錄存在但尚無實作（企劃書提及隨機節點但細節未定）。
- `Enemy/EnemyAI.cs`：有基礎 AI，記者 / 志工 / Boss 的完整行為待實作。
- 大招（群眾造勢 / 說明會）邏輯待實作。
- 棄保效應機制：區塊完成時若票數未高於對手，對手深色選民出現率 +10%，待實作（ponytail: 舊 Block 系統概念，需重新設計為節點觸發）。
- 結局 A/B/C/D 的結局畫面演出待製作。
- 前導劇情（S1）待製作。
- **場景內獎勵物件**：`RewardItem` Prefab（3D 底座 + Billboard SpriteRenderer）待美術製作與 Inspector 設定。
- **任務資料填寫**：`RoomMissionData` SO assets 待在 `Assets/Data/Mission/` 建立，`MissionPool` 待組裝，各戰鬥 scene 需放置 `MissionTracker`、`RewardItemSpawner`、兩個門物件。
- **對話系統多頁**：`TutorialStepData.dialogueLines` 每個元素為獨立一頁，按確認鍵翻頁，最後一頁才關閉對話框。
- **技能選擇場景內化**：第 5/10 關的技能選擇預留於 `RewardItemSpawner`（TODO 標記），待技能選擇物件設計完成後實作（ponytail: 舊 Block 系統概念，需調整為節點 4 完成後觸發）。
- **StageClearState 轉場動畫**：預留漫畫網點風格過場動畫接入點（`ponytail:` 注解標記）。
- **敵人 AI 後續優化**：
  - **實作真實傷害判定**：目前 `EnemyAttackState` 僅觸發動畫，需加入實際對目標扣血的邏輯。
  - **實作 Billboard**：敵人 Sprite 需永遠面向攝影機，避免在 3D 空間中不自然旋轉。
  - **敵人數值調整**：需調高 `NavMeshAgent` 的速度，優化敵人追擊節奏。

### ponytail 技術債與架構待辦
- **Elite 場景專用化**：第 4 關 Elite 目前使用 `TestMVP` 場景，因原 `TestSmallBoss` 缺少 `MissionTracker`、`RewardItemSpawner`、`BattleFlowController` 等組件。若替換專用場景需補齊這些組件並放置雙門生成點。
- **教學流程統一**：教學關卡目前保留舊 `OnRoomCleared` 事件相容，待其他部分穩定後統一為新事件流程（`OnObjectiveResolved` 等）。
- **劇情系統實作**：`NarrativeBeat` 與 `NarrativeDirector` 目前只建立接口邊界，Timeline／運鏡／對話編排系統待美術資源與劇本完成後實作。
- **種子系統 UI**：目前種子僅在 `CampaignDefinition` 設定，未來可考慮讓玩家選擇或輸入種子（類似《死亡細胞》的日常挑戰）。
- **失勢獎勵平衡**：目前設計為普通卡 + 至少一張中性卡，需 playtest 後調整數值與卡池組成。

---

## 給 AI 的溝通指引

- 預設使用**繁體中文**溝通。
- 修改程式碼時，請優先閱讀相關腳本後再動手，避免與現有架構衝突。
- 新功能請遵循現有的事件解耦模式，避免系統間直接呼叫。
- 新增 ScriptableObject 資料類別請放在 `Assets/Data/` 對應子資料夾。
- 新增腳本請放在 `Assets/Scripts/` 對應資料夾。
- **資金 = MP（法力）**，**誠信值 = HP（血量）**，兩者是完全不同的資源，注意不要混淆。
- **深色選民 ≠ Dark Attribute**：程式中 `VoterAttribute.Dark` 對應企劃書「深色選民」，`VoterAttribute.Cold` 對應「#冷感選民」，`VoterLabel.Emotion` 對應「#情緒共振」。

# 🎮 Unity 專案架構與全域狀態機開發規範

本文件記錄了本專案的核心架構設計、狀態機（State Machine）開發準則，以及 UI 流程控制規範。未來所有的功能擴充與 AI 程式碼生成，**都必須嚴格遵守以下原則**，以維持程式碼的乾淨、高擴充性（OCP）與單一職責（SRP）。

## 🌟 一、 核心系統架構 (The Big Picture)

本專案採用 **Clean Architecture（乾淨架構）**，將遊戲邏輯分為三大核心區塊，嚴禁跨界干涉：

1. **資料大腦 `GameDB` (SSOT)**
* **職責**：全域單一資料來源，集中管理玩家、遊戲進度等所有跨場景資料。
* **特性**：包含完整防呆機制、事件訂閱更新。所有變更皆須經過 `GameDB` 以確保資料正確性。

2. **流程大腦 `GameFlowManager` (全域狀態機)**
* **職責**：管理遊戲的宏觀生命週期（主選單 ➔ 總部 ➔ 戰鬥/休息 ➔ 結算 ➔ Boss）。
* **特性**：跨場景不銷毀 (`DontDestroyOnLoad`) 的 Singleton。只負責切換 `IState`，絕對不處理具體的 UI 動畫或戰鬥傷害計算。
* **狀態讀取**：對外僅提供唯讀屬性 `public IState CurrentState => stateMachine?.CurrentState;`，嚴禁外部腳本直接修改狀態。

3. **雙手 `UIManager` & 全域 Canvas**
* **職責**：純粹的視覺呈現。只提供 `ShowPanel()` 與 `HidePanel()` 以及 UI 序列的控制。
* **特性**：與流程大腦一樣是 `DontDestroyOnLoad`。它不知道「遊戲現在玩到哪裡」，只聽命於大腦的指揮與 `GameDB` 的事件推播。

4. **神經 `UIFlowHelper` & `BattleEventManager`**
* **職責**：負責傳遞訊號。
* **`UIFlowHelper`**：掛載於 UI 按鈕上，將玩家的點擊事件（OnClick）轉發為大腦的狀態切換指令（例如 `ChangeState(new CharacterSelectState())`）。
* **`BattleEventManager`**：戰鬥場景中的事件中心。正式戰役以 `OnObjectiveResolved`、`OnRewardCollected`、`OnRouteSelected` 推進；`OnRoomCleared` 只保留給教學關過渡相容。FinalBoss 則由 `OnFinalBossDefeated` 回報對手已擊敗。

---

## 🛠️ 二、 狀態 (IState) 開發規範

當未來需要新增任何全域流程狀態（如：商店狀態、轉蛋狀態）時，請遵守以下實作守則：

### 1. 場景載入必須使用非同步與 Coroutine

**嚴禁**在 `Enter()` 中直接呼叫 `SceneManager.LoadScene` 並預期物件立刻可用（會導致 Race Condition 與 NullReferenceException）。

* ✅ **正確做法**：在 `Enter()` 中啟動 Coroutine (`GameFlowManager.Instance.StartCoroutine(...)`)，使用 `LoadSceneAsync` 並加上 `while (!asyncLoad.isDone) { yield return null; }`。
* **UI 開啟時機**：必須等 `isDone` 為 `true` 後，才呼叫 `UIManager` 開啟對應的場景內 HUD（確保場景物件如 Timer 已被 Awake）。

### 2. 事件訂閱與解除 (防止 Memory Leak)

* **訂閱時機**：在場景確定載入**完成後**（Coroutine 結束時），才向 `BattleEventManager` 或 `GameDB` 訂閱事件。
* **解除時機**：**必須、一定、絕對**要在 `Exit()` 中解除訂閱（`-=`），防止舊狀態在背景繼續干擾新流程。

### 3. 轉場邏輯與分流 (Edge Cases 防禦)

在寫狀態切換邏輯時，必須以 `CampaignDefinition` 的節點角色判定特殊流程；不可再用房號、Block 或安全房狀態推導主線，避免跳過 Elite 或 FinalBoss。

---

## 🚫 三、 避坑指南與嚴禁寫法 (Anti-Patterns)

為了避免架構退化回義大利麵條代碼（Spaghetti Code），嚴禁使用以下寫法：

### ❌ 嚴禁使用 Checklist Pattern 控制 UI (Update 裡的 Booleans)

* **錯誤示範**：在 State 裡面寫 `bool isDataClosed`，然後在 `Update()` 裡面一直 `if(isDataClosed)`。這違反開閉原則，且效能低落。
* **✅ 正確做法 (委派回呼 Action)**：使用 UI Sequence Controller 模式。由 State 呼叫 `UIManager.Instance.StartSequence(Action onComplete)`，把「展演完畢後要切換狀態的邏輯」當作參數傳給 UI，等 UI 播完後自己 `Invoke()` 呼叫它。
* **✅ 正確做法 (資料綁定)**：依賴 `GameDB` 廣播的 Action 更新數值型 UI，嚴禁 Update 中檢查數值。

### ❌ 嚴禁在 Manager 裡寫滿 if-else

* **錯誤示範**：`if(isPaused) { ... } else if (isGameOver) { ... }`。
* **✅ 正確做法**：所有行為都封裝在具體的 `IState` (例如 `PauseState`, `GameplayState`) 中。Manager 的 `Update()` 裡面永遠只有乾淨的一行 `stateMachine.CurrentState?.Update();`。

### ❌ 嚴禁全域狀態干涉個體戰鬥狀態

* 全域大腦 (`GameFlowManager`) 不可以去呼叫 `Player.Attack()` 或控制怪物 AI。大腦只看宏觀的「房間進入」與「房間結束」。具體戰鬥由 `PlayerStateMachine` 等局部狀態機自行負責。

---

## 🚀 四、 未來擴充標準流程 (How to Add a New Feature)

當你想增加一個新功能（例如：第 8 關固定進入「商人房間」）時，請依照以下 3 步：

1. **建立新 State**：新增 `MerchantRoomState.cs` 實作 `IState`。寫好 Coroutine 場景載入與離開事件監聽。
2. **建立/註冊新 UI**：在 `UIManager` 新增商人的 Panel 欄位與 `Show/Hide` 方法。在 `MerchantRoomState` 的 Enter/Exit 中呼叫。
3. **修改戰役定義**：在 `CampaignDefinition` 為明確節點或角色配置內容；需要特殊房型時，擴充 `CampaignNodeDefinition`／流程介面，而不是在 `StageClearState` 寫房號魔術數字。

**(完)**

---


## 🧹 開發日誌 (Development Log)
> 紀錄格式已依日期排列，方便後續直接更新至工作室網頁

### 📅 2026-09-05：穩定化固定 8 節點戰役流程
- **核心重構**：以「固定節點骨架、合資格的隨機任務二選一」取代 Block 系統。全程固定 8 個節點：節點 1 為開場、節點 4 為 Elite、節點 8 為 FinalBoss，其餘為二選一。
- **戰役定義系統**：新增 `CampaignDefinition` ScriptableObject，明定 8 個節點的角色、固定任務、專用場景與劇情插入點。`CampaignData` 收斂為本局唯一進度，移除舊 Block／`RoomSequence`／重複房號推進。
- **關卡生命週期拆分**：將房間生命週期明確切成 `Loading → Briefing → Active → ObjectiveResolved → RewardSelection → RouteSelection → Transitioning`，每個階段只能進入一次。拆開泛用 `OnRoomCleared`，任務判定、獎勵選定、出口選路、場景切換分別使用帶資料的事件（`OnObjectiveResolved`、`OnRewardCollected`、`OnRouteSelected`）。
- **失勢獎勵機制**：成功使用正常三選一卡；失勢使用三張普通卡，至少一張中性可用卡，其餘可偏離流派。失勢會記錄 `EncounterResult`，並寫入劇情紀錄。誠信歸零才結束本局。
- **雙門動態生成**：二選一節點在獎勵後由 `RouteDoorSpawner` 動態生成雙門 Prefab；門只回報選項索引，顯示資料與一次性鎖定由流程控制器管理。舊單出口流程不再參與選關。
- **劇情預留**：每個節點可配置進場、任務結果、Boss 前後的 `NarrativeBeat`。第 4 關 Elite 的雙門選擇前插入劇情；後續 Cinemachine 運鏡、角色動畫與對話由 `NarrativeDirector` 依 Beat 播放，不直接耦合任務、門或場景切換（ponytail: 預留邊界，待美術資源與劇本完成後實作）。
- **種子驅動抽選**：任務選項用固定 seed 生成並保存，UI 重載或重碰門不會重抽。加入 12 隻敵人的 Elite 任務、搶票限時任務與 7 張中性普通卡。
- **狀態機精簡**：移除舊 `BossBattleState`、`SafeRoomState`、Block 相關代碼；第 4 關不再走 Boss／結局邏輯；第 8 關才結束本局。教學保留舊事件相容（待穩定後統一）。
- **驗證機制**：加入 `CampaignDefinition.IsValid()` 與 `HasValidMissionChoices()` 啟動前驗證，確保節點完整性與每個二選一節點都有至少 2 個有效任務。
- **UI 更新**：`MapProgressUI` 從「已完成區塊：X / 3」改為真正的「選戰倒數：X / 8 天」；第 1 節點顯示 8/8，並訂閱戰役進度更新。

### 📅 2026-08-24：關卡任務系統、場景內選卡、對話系統改版
- **任務系統**：新增 `RoomMissionData`、`MissionObjectiveType`、`MissionPool`、`MissionTracker`，支援三種任務類型（消滅/搶票/生存）。任務資料存於 `CampaignData.ActiveRoom`，結算後由 `StageClearState` 從 `MissionPool` 抽取兩個選項注入岔路。
- **岔路門系統**：新增 `DoorController`（進入觸發）與 `DoorPreviewZone`（靠近顯示任務 tip），分離感應範圍與選擇觸發，復用 `TutorialTipsUI` 顯示任務說明一行文字。
- **場景內獎勵選卡**：拔除 `UIManager` 結算 UI 序列（Data/Reward/Skill Panel），改為場景內 `RewardItem`（3D 物件 + Billboard）+ `RewardItemSpawner`（任務結果後生成）+ `RewardDescriptionUI`（置中說明面板）；玩家靠近按 E 選擇，選完觸發 `TriggerRewardCollected`。
- **政策卡架構調整**：移除 `CardType` enum，改以 `FactionData faction`（可 null = 通用）標記派系歸屬；新增 `cardArtwork` Sprite 欄位；稀有度（`CardRarity`）作為卡框視覺語言，不按類別篩選獎勵。
- **對話系統多頁**：`TutorialDialogueUI` 改為逐頁顯示，每個 `dialogueLines` 元素為獨立一頁；`TutorialManager` 按確認鍵翻頁，最後一頁才關閉對話框。
- **`StageClearState` 精簡**：移除所有 UI 流程，只保留推進房間、生成岔路選項、切換下一 State；預留漫畫網點轉場動畫接入點。
- **錯誤修復**：修正計票系統重複計算、關卡房號 off-by-one 錯誤、玩家速度未即時同步等核心 Bug。
- **舊代碼隔離**：將廢棄的 Manager (如 `PolicyCardManager`, `BlockProgressManager` 等) 徹底隔離並搬移至 `Obsolete` 資料夾，維持專案 100% 編譯成功與零缺失引用。
- **生命週期管理**：實作 `AutoDestroy` 組件並與 `PolicyEffectRuntimeManager` 整合，確保動態生成物件能自動銷毀，解決轉場時潛在的記憶體洩漏 (Memory Leak)。

### 📅 2026-07-13：政策卡牌系統積木化 (Strategy Pattern)
- **架構重構**：全面導入 `ICardEffect` 介面，實作了數值修改 (`StatModifierEffect`) 與物件生成 (`SpawnObjectEffect`) 等邏輯積木，支援玩家在遊戲中使用多重卡牌效果組合技。

### 📅 2026-07-08：UI 重構與流程解耦
- **純鍵盤總部**：完成總部場景純鍵盤操作重構 (`HQState`)，並升級鏡頭大腦 (`HQSceneController`) 改善運鏡。
- **UI 解耦**：各項遊戲 UI 徹底改為「訂閱 GameDB 事件」更新機制，使 UI 切換與全域狀態機 (GameFlowManager) 達成完全解耦。

### 📅 2026-07-05：技能與戰鬥系統擴充
- **特效與實體生成**：實作技能特效物件池 (`PoolManager`) 與建築配件自動生成器 (`SocketBuilder`)，同時支援 Play Mode 與 Edit Mode 預覽。
- **技能機制**：新增「放置立牌」等全新技能，並實現基於 GameDB 資料驅動的總部技能解鎖與自動裝備，解除場景間的相依性。

### 📅 2026-07-02 ~ 2026-07-03：核心架構升級 (GameDB & SSOT)
- **統一真相來源**：成功將專案中散落的進度、數值、玩家狀態與社會風氣，統一交由 `GameDB` 作為單一真相來源 (SSOT) 集中管理。
- **消滅舊儲存**：徹底拔除舊版 Manager 依賴與 `PlayerPrefs` 儲存機制，落實 Clean Architecture 開發規範。
