## 架構守則
1. 資料單一來源 (SSOT)：所有跨場景存活的數值嚴禁存在 Manager 中，必須在 GameDB.cs 註冊。
2. 事件驅動 (Event-Driven)：UI 一律透過訂閱事件更新，嚴禁在 Update() 裡面輪詢。
3. Proxy 模式：若現有 Manager 需保留，它只能作為 Proxy 轉接，不可存放狀態。
4. 資料防呆：所有增減數值的方法必須包含邊界檢查（如除以零保護、負數防禦）。

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

### 核心主題
以「將政治博弈動作化」為核心，結合 Roguelite 成長感與政治選舉的即時性。透過戲謔荒謬的美術風格處理台灣政治認同議題，引導玩家反思選民決策與社會撕裂。

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

## 遊戲大流程（依 GameState 圖）

```
開始畫面 → 主選單 → 前導劇情 → 玩家競選總部
  ↓
[是否選擇第一個技能？]
  是 → 開始行動
  否 → 直接開始行動（可跳過）
  ↓
房間內戰鬥（Battle）
  ↓
[房間倒數是否結束？]
  是 → [資金是否為 0？]
          是 → 顯示結局 D：退選
          否 → [誠信是否為 0？]
                  是 → 顯示結局 D：退選
                  否 → 繼續往下
                       [票數是否大於對手？]
                         否 → 顯示選擇快報 → 依照比例恢復資金
                         是 → 顯示政策卡獎勵 → 依照比例恢復資金
                              [是否為第五個房間？]
                                否（非最後）→ 回到房間內戰鬥
                                是 → [是否為第一區？] → 選擇技能二
                                     [是否為第二區？] → 選擇大招
                                     [是否為第三區？] → Boss 戰
                                          ↓
                                     [倒數完畢，觸發失敗或成功條件]
                                          ↓
                                     結局（見下方結局系統）
```

### 遊戲流程狀態機 (GameFlow States)
狀態類別對應：`BootState`、`MainMenuState`、`CharacterSelectState`、`HQState`、`GameplayState`、`SafeRoomState`、`StageClearState`、`BossBattleState`、`GameEndState`。
以上狀態由全域的 `GameFlowManager` 負責管理與切換。

### 結局系統
| 結局 | 條件 | 畫面描述 |
|------|------|----------|
| 結局 A（政黨至上） | 社會風氣偏情緒下勝選 | 電視呈現勝選，周遭有競選小物、公仔 |
| 結局 B（政見至上） | 社會風氣偏理性下勝選 | 電視呈現勝選畫面 |
| 結局 C | 敗選（票數落後） | 電視呈現敗選畫面 |
| 結局 D | 誠信歸零 / 資金歸零 → 退選 | 電視呈現退選，選舉傳單在垃圾桶裡 |

---

## Block 與 Room 結構

- 全程共 **3 個區塊（Block）**，每個 Block 包含 **5 個房間（Room）**。
- 房間類型由系統依權重隨機生成：

| 房間類型 | 生成物件 | 時限 |
|----------|----------|------|
| 普通房間 | 選民 10～15、地方人士 0～2、對手志工 1～2 | 60 秒 |
| 特殊房間 | 恢復資金 或 恢復誠信 | 無限制 |
| Boss 戰 | 選民 60、Boss 1、對手志工 3 | 180 秒 |

- **第 5 個房間（最後一間）**：
  - 第一區 → 解鎖技能二選一
  - 第二區 → 解鎖大招選一
  - 第三區 → Boss 戰（選前之夜）

- 每區完成後返回**競選總部（headquarters）**，可繼承資源進入下一區或開新局。

---

## 核心系統

### 1. 選票系統（VoteManager）
- 預設雙方各 50 票。（資料存於 GameDB）
- 選民被轉化時票數即時更新。
- **計時結束時票數 > 對手** → 觸發通關 / 觸發政策卡獎勵。
- **票數 ≤ 對手** → 顯示選擇快報，依比例恢復部分資金，繼續下一房間（非最後房間）。

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

### 6. 技能系統（PlayerSkillManager）
技能採**階段解鎖**，每次解鎖為二選一，玩家自由搭配風格：

| 解鎖時機 | 技能 | 情緒版 | 理性版 |
|----------|------|--------|--------|
| 總部出發前 | 技能一 | 煽動情緒（短暫暈眩對手） | 政策論述（大範圍攻擊） |
| 第一區完成後 | 技能二 | 側翼出擊（大幅減少對手選票） | 發表白白皮書（短暫暈眩對手） |
| 第二區完成後 | 大招 | 群眾造勢（需深色選民，轉化支持者為深色選民） | 說明會（需深色選民，大範圍影響選民） |

- 技能鍵：J / K / L（分別對應技能一、技能二、大招）。
- 政黨技能（`PartySkillData`）有冷卻時間（`baseCooldown`）與資源消耗。
- 目前程式中現有實作範例：`DogezaSkill`（土下座）——轉換 Cold 屬性選民的特殊技能。

### 7. 政策卡系統（PolicyCard）
通關房間後（票數 > 對手）可選擇政策卡，效果全局疊加：

| 政策卡 | 風格 | Buff | Debuff |
|--------|------|------|--------|
| 街頭造勢 | 情緒 | 範圍大 | 支持者容易流失 |
| 政策說明會 | 理性 | 支持者不易流失 | 範圍小 |
| 精準訴求 | 理性 | 攻擊力大幅提升 | 範圍小 |
| 情緒動員 | 情緒 | 有機率擴散至其他選民 | 所有選民移動速度增加 |
/待增
PolicyCard 數值欄位（`PolicyEffectRuntimeManager` 管理）：

| 欄位 | 說明 |
|------|------|
| `attackRadiusMultiplier` | 攻擊範圍倍率（相乘） |
| `convertChanceDelta` | 轉化率加成（相加） |
| `attackCooldownDelta` | 攻擊冷卻調整（相加） |
| `loseControlRateDelta` | 選民流失率 |
| `spreadRadius` | 影響擴散半徑 |
| `globalNpcSpeedMultiplier` | 全體 NPC 速度倍率（相乘） |
| `socialClimateDelta` | 社會風氣值變化 |

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
狀態類別對應：`VoterIdleState`（閒置）、`VoterWanderState`（徘徊）、`VoterFollowState`（跟隨玩家）、`VoterHitState`（受擊）、`VoterStunState`（暈眩）、`VoterCheerState`（歡呼）。
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
- 倒數結束前票數 > 對手
- 資金與誠信皆未歸零

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
| `MapScene` | 地圖節點選擇（單線隨機節點生成） |
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
├── UIManager/       # UI 元件
├── Effects/         # 特效、物件池相關
├── Enemy/           # 敵方 AI
├── ScenesManager/   # 場景切換管理
└── RandomEvents/    # 隨機事件（未開發）
```

### 程式規範
- **語言**：C#，Unity 6000.x 系列。
- **資料單一來源 (SSOT)**：全域狀態存放在 `GameDB.cs` 中，不依賴於 Manager 儲存全域數值，確保跨場景資料的一致性與防呆。
- **Singleton 模式**：主要系統使用 `Instance` 靜態屬性，`Awake` 中防止重複建立。若 Manager 仍存在則主要作為 Proxy。
- **事件系統**：使用 C# `event Action` / `event delegate` 解耦系統間通訊。UI 必須訂閱事件更新，嚴禁在 Update 輪詢。
- **資料與邏輯分離**：資料層使用 `ScriptableObject`（`VoterConfig`、`PolicyCardData`、`PartySkillData`）或資料元件（`VoterData`），邏輯層獨立（`VoterLogic`）。
- **物件池**：特效使用 `PoolManager`（`AutoReturnToPool`、`PooledParticleInstance`）。
- **進度儲存**：`PlayerPrefs` 儲存 Block/Room 進度與待處理事件。
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
- 棄保效應機制：區塊完成時若票數未高於對手，對手深色選民出現率 +10%，待實作。
- 結局 A/B/C/D 的結局畫面演出待製作。
- 前導劇情（S1）待製作。
- **敵人 AI 後續優化**：
  - **實作真實傷害判定**：目前 `EnemyAttackState` 僅觸發動畫，需加入實際對目標扣血的邏輯。
  - **實作 Billboard**：敵人 Sprite 需永遠面向攝影機，避免在 3D 空間中不自然旋轉。
  - **敵人數值調整**：需調高 `NavMeshAgent` 的速度，優化敵人追擊節奏。

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
* **`BattleEventManager`**：戰鬥場景中的大聲公（靜態事件中心）。當玩家死亡或打贏房間時，發送 `OnRoomCleared` 或 `OnPlayerDied` 廣播，讓大腦決定下一步。

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

在寫狀態切換邏輯時，必須考慮極限值與特殊流程。例如從 `SafeRoomState` (安全房) 離開時，必須檢查 `if (roomNumber == 15) { 進入Boss }`，防止無縫切換時不小心跳過主線重要事件。

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
3. **修改切換樞紐**：去前一個狀態（如 `StageClearState` 的轉場邏輯中），加入判定 `if (roomNumber + 1 == 8) ChangeState(new MerchantRoomState());`。

**(完)**

---

## 🧹 重構總結 (GameDB 統一數值管理)
> 紀錄時間：2026-07-02

我們已經成功將專案中散落的數值與 Manager 集中至 `GameDB`，以下是歸檔與確認狀態：

### 1. 殘留引用掃描結果
- **掃描目標**：`PlayerMPSystem`、`VoteManager`、`PolicyEffectRuntimeManager` 等舊版全域實例，以及直接操作 `HP` / `MP` / `Vote` 的外流代碼。
- **掃描結果**：**乾淨無殘留**！
  - 所有的 UI (`MPBarUI`, `HPBarUI`, `VoteDisplayUI`, `LevelTimerUI`, `RewardPanelUI`, `MiniSettlementUI`) 已經正確改為訂閱 `GameDB.Instance.Run` 的事件 (`OnMPChanged`, `OnVotesChanged` 等)。
  - `VoterLogic.cs` 與 `VoterData.cs` 中呼叫陣營與影響力的邏輯，皆已安全轉向 `GameDB.Instance.Run` 或 `PolicyManager`。
  - `DogezaSkillData` 等技能邏輯也正確掛載至 `GameDB.Instance.Run.ModifyMP`，包含防呆與邊界檢查。

### 2. Obsolete 歸檔狀態
- 🗑️ `VoteManager.cs`：已徹底移除，由 `GameDB.RunData.AddVote()` 負責計票。
- 🗑️ `PlayerMPSystem.cs`：已徹底移除，由 `GameDB.RunData.ModifyMP()` 取代。
- 📦 `PolicyEffectRuntimeManager.cs`：已歸檔至 `Assets/Scripts/Obsolete/`，被新的 `PolicyManager` 正式取代。
核心邏輯的遷移完全符合 Clean Architecture 與 SSOT 原則。

### 3. 測試腳本生成
- 建立 `GameDBTest.cs` 於 `Assets/Scripts/System/GameDBTest.cs`。
- 功能：每幀安全地監控 `GameDB.Instance.Run` 的 HP、MP 與選票狀態，確保跨場景資料存活正確無誤。

---

## 🧹 重構總結 (社會風氣與政策倍率職責拆解)
> 紀錄時間：2026-07-03

我們接續了 GameDB 的集中化重構，成功將「社會風氣 (Social Atmosphere)」與「政策卡倍率」的職責徹底解耦：

### 1. 社會風氣全域繼承 (`GameDB.RunData`)
- **完全取代**：舊有的 `SocialAtmosphereManager` 已被徹底刪除。
- **資料中心化**：`SocialAtmosphere` 的數值上下限、邊界保護以及 `OnAtmosphereChanged` 事件，全部移入 `GameDB.Instance.Run` 中。
- **純函數轉換**：影響深色選民生成率的 `GetDarkVoterRate()`，已重構為直接向 GameDB 取值的唯讀方法。`Spawner` 等生成器現已完美對接 GameDB。

### 2. 政策倍率拆分 (`PolicyManager` 與 `PlayerHealthSystem`)
- **徹底解耦**：原本揉合了血量、倍率、社會風氣的 `PolicyEffectRuntimeManager` 已經被完全刪除。
- **`PolicyManager`**：現在只專職負責 `AttackRadiusMultiplier` 等「政策卡增益倍率」的管理，並提供唯讀 Getter 供 `PlayerAttack` 等戰鬥邏輯調用。
- **`PlayerHealthSystem`**：作為生命 Proxy，現在不僅負責 `TakeDamage()` 與 `Heal()`（並將之轉交給 GameDB），更成功訂閱了 `GameDB.Instance.Run.OnIntegrityHpChanged`。當生命歸零時，會主動發送 `BattleEventManager.TriggerPlayerDied()` 給全域狀態機，完美達成事件驅動設計！

---

## 🧹 重構總結 (技能特效池化與 SocketBuilder 雙軌生成系統)
> 紀錄時間：2026-07-05

為了達成高效能與靈活的編輯環境，進行了以下優化與擴充：

### 1. 技能特效池化 (Skill VFX Pooling)
- **`SkillData.cs`**：將 `skillEffectPrefab` 更新為 `vfxPrefab` (利用 `FormerlySerializedAs` 保持相容性)，並新增 `vfxDuration` 統一管理特效生命週期。修改 `ExecuteSkill()` 直接對接 `PoolManager` 取出特效實體。
- **`PooledVFXInstance.cs`**：建立新的特效自動回收腳本，實作 `IPoolable` 介面。支援基於時間 (`duration`) 或是 ParticleSystem 的 `OnParticleSystemStopped` 回呼來自動執行 `Release`。
- **特殊技能適配**：修改 `DogezaSkill.cs` 等繼承自 `SkillData` 的特規技能，使其自訂的 `ExecuteSkill` 也遵循新的物件池取用規範。

### 2. 建築配件自動生成器 (`SocketBuilder.cs`)
- **雙軌生成與回收機制**：
  - **Play Mode**：透過 `PoolManager` 取出與回收 (`Get`/`Release`)。
  - **Edit Mode**：為了支援美術人員預覽，結合了 `#if UNITY_EDITOR` 與 `UnityEditor.PrefabUtility.InstantiatePrefab()`，保留藍色的 Prefab 連結。並支援了 `Undo.RegisterCreatedObjectUndo`（Ctrl+Z 復原機制）。回收則使用 `DestroyImmediate()` 處理。
- **美術防變形規範**：所有生成的配件在 SetParent 後，會自動重置 `localPosition = Vector3.zero` 與 `localRotation = Quaternion.identity`（不強制縮放，尊重 Prefab 原始比例），確保建築物不管怎麼形變，配件都能完美對齊 Socket 不變形。

---

## 🧹 重構總結 (總部重構與鍵盤驅動流程)
> 紀錄時間：2026-07-08

完成了總部 (Headquarters) 場景的 UI 與運鏡重構，主要改動如下：

### 1. 純鍵盤驅動流程 (`HQState.cs`)
- 徹底移除了總部內的 UI 按鈕點擊依賴，改為透過 `Keyboard.current` 進行全鍵盤操作。
- **選角階段**：使用 `A` / `D` 切換角色鏡頭，`Enter` 確認進入選技能。
- **選技能階段**：使用 `W` / `S` 切換技能企劃書，`Enter` 寫入 `GameDB` 並觸發 `FadeOut` 進入戰鬥關卡，`Esc` 退回選角階段。
- **物理隔離**：進入 `HQState` 時主動停用 `PlayerController`，完全阻斷玩家在總部內的實體移動與攻擊。

### 2. 鏡頭大腦升級 (`HQSceneController.cs`)
- 實作「重置與碾壓法 (Reset & Elevate)」：透過迴圈將所有相機權重壓低 (Priority 10)，再單獨拉高目標相機 (Priority 20)，解決了 Cinemachine 鏡頭切換殘留的問題。
- 提供了對外部全域狀態機非常友善的乾淨 API：`FocusMale()`、`FocusFemale()`、`FocusDesk()`。

### 3. UI 漸變與解耦 (`UIManager.cs`)
- `UIManager` 內部實作了 `FadeOut()` 與 `FadeIn()` 的 Coroutine 方法，供全域狀態機跨場景呼叫，確保轉場過程中的黑畫面遮罩與點擊阻斷。
- 貫徹 SSOT：技能選擇完畢後直接寫入 `GameDB.Instance.Player.EquipBaseSkillJ`，徹底擺脫了對場景內 Player 實體的依賴。
- **流程合併精簡**：將原本獨立的 `CharacterSelectState` 與對應的 `CharacterSelectPanel` 徹底刪除，完全併入總部 (`HQState`) 的選角介面中，簡化了狀態機的複雜度。

## 🧹 重構總結 (技能系統擴充與 SSOT 裝備落實)

完成了「理性流派」新技能的實作，並徹底清理了技能管理器的跨場景相依，主要改動如下：

### 1. 新技能：放置人形立牌 (`StandeeSkillData`)
- **Fire-and-forget 架構**：新增 `StandeeSkillData` (繼承自 `SkillData`) 與 `StandeeBehavior`。技能施放後於玩家前方實例化立牌，由立牌自行利用 Coroutine 計時並透過 `Physics.OverlapSphere` 掃描，強制轉化周圍的 `Rational` 理性選民。該設計完美實現了技能持續效果與玩家本體邏輯的解耦。

### 2. 徹底落實 GameDB 跨場景資料 (SSOT)
- **拔除靜態依賴**：完全移除了 `PlayerSkillManager` 中的 `static equippedPartySkill` 等靜態快取變數。
- **統一讀寫入口**：總部裝備技能時，一律寫入 `GameDB.Instance.Player.EquipBaseSkillJ`。當戰鬥場景載入時 (`Awake`)，由 `PlayerSkillManager` 主動向 `GameDB` 讀取當前裝備，徹底杜絕了多場景切換與重啟造成的資料脫鉤。

### 3. UI 單一職責重構
- **隔離邏輯**：新增 `HQSkillSelectionUI` 專門負責綁定總部的技能按鈕點擊，單純負責 UI 反饋與寫入 `GameDB`。
- **依賴清理**：同步更新了舊有的 `UpgradePanelUI` 與 `HeadquartersManager`，修正了所有因全域靜態變數移除而產生的過期呼叫，維護了專案的 Clean Architecture 規範。

---

## 🧹 重構總結 (專案大清洗與架構除蟲)
> 紀錄時間：2026-07-08

**專案狀態更新**：Stable / Clean (零重大架構風險)

### 1. 已完成的除蟲與優化任務
- **核心計票系統重構**：解決選票重複與物件池初始化問題。
- **關卡邏輯重構**：修正房號 off-by-one 錯誤與 Boss 關切換時序。
- **生存時間過關機制實作**：時間到 $\rightarrow$ 選民自動退場 $\rightarrow$ 開門。
- **政策卡資料持久化**：遷移至 `GameDB`，移除場景依賴，改用靜態讀取。
- **社會風氣正負號邏輯校正**：確保正負極端完美對應情緒與理性設定。

### 2. 備註特殊設計
- **敵人機制確認**：保留「敵人不死」為遊戲設計特色，排除相關除蟲項目。

---

## 🧹 重構總結 (政策卡系統與積木化)
> 紀錄時間：2026-07-13

**專案狀態更新**：[Status: Refactoring Completed, Ready for Gameplay Testing]

### 1. 統一玩家數值與 SSOT 擴展
- **`PlayerStatsData.cs`**：建立全新類別作為玩家戰鬥數值的單一真相來源 (SSOT)，包含 `ModifiedAttackRange`、`ModifiedAttackInfluence` 等屬性。
- **`GameDB.cs`**：將 `PlayerStatsData` 實體以及當前生效的卡牌列表 (`ActiveCards`) 註冊至 `GameDB.RunData` 統一管理。

### 2. 導入 Strategy Pattern (策略模式)
- **`ICardEffect.cs`**：建立卡牌效果積木基礎介面。
- **`StatModifierEffect.cs`**：實作第一個數值修改積木，支援加法與乘法計算，並可選擇不同的 `StatType` 進行數值變更。
- **`PolicyCardData.cs`**：將舊版寫死的數值欄位全部移除，改用 `[SerializeReference]` 儲存 `ICardEffect` 列表，實現企劃可在 Inspector 中自由組裝卡牌效果的機制。

### 3. 消滅 God Class 與依賴重構
- **徹底拔除 `PolicyManager`**：刪除原有的 God Class，清空所有過度耦合的靜態依賴。
- **重構依賴**：`PlayerAttack`、`VoterLogic` 等戰鬥邏輯，現在統一改為讀取 `GameDB.Instance.Run.Stats` 來獲取當前正確的戰鬥數值，達到真正的低耦合與高內聚。
- **總結架構**：已完成從 God Class 轉向 ScriptableObject + Strategy Pattern 的重構，Stats 統一由 GameDB 管理。

---

## 🧹 待辦事項：專案掃除與清算 (Legacy Cleanup)
> 紀錄時間：2026-07-17

**任務狀態**：[Status: Completed]

### 1. 診斷結果與當前進度
已完成隔離區建立，並將 `[Safe to Delete]` 檔案（如 `ScoreManagerSample.cs`）移至 `Obsolete`，且已將 `PolicyCardManager.cs` 的抽卡邏輯遷移至 GameDB，並將其檔案移至 `Obsolete`。
進一步將 `CampaignProgressManager.cs`、`BlockProgressManager.cs` 與 `HeadquartersManager.cs` 搬移至 `Obsolete` 進行隔離，專案核心進度系統已 100% 收斂至 GameDB，且 PlayerPrefs 與靜態 Manager 已完成解耦。

### 2. 清理清單與依賴狀態
- `ScoreManagerSample.cs`: **[Safe to Delete]** (已隔離，完全無引用)。
- `PolicyCardManager.cs`: **[Needs Migration]** (已隔離，已被 GameDB.RunData 的抽卡邏輯取代)。
- `CampaignProgressManager.cs` 與 `BlockProgressManager.cs`: **[Needs Migration]** (已隔離，相關進度數據完全移入 `GameDB.Instance.Campaign` 中)。
- `HeadquartersManager.cs`: **[Needs Migration]** (已隔離，完全無引用，總部邏輯已被 `HQSceneController.cs` 替代)。
- `S0Manager.cs`, `S1Manager.cs`: **[Needs Migration]** (場景腳本，需將 Unity UI 事件轉綁給 `UIFlowHelper` 後拔除)。

---

## 🧹 重構總結 (PlayerPrefs 消滅與 GameDB SSOT 大一統)
> 紀錄時間：2026-07-17

**專案狀態更新**：[Status: Completed, Next: 實作新政策卡積木 (SpawnObjectEffect) 與測試核心好玩度]

我們已徹底消滅專案中散落的 `PlayerPrefs` 進度與狀態儲存，完成向 `GameDB` 的大一統收斂：

### 1. 進度與狀態完全收斂至 GameDB
- **`GameDB` (SSOT) 擴充**：於 `RunData` 中新增 `HasPendingSkillSelection` 暫存變數，於 `CampaignData` 中移植了 `TotalBlockCount` 常數、戰役完成度判定、隨機 Room 序列生成以及 Block 的 TryComplete 推進方法。
- **無狀態化與解耦**：完全解除了對 `BlockProgressManager` 和 `CampaignProgressManager` 的靜態呼叫。

### 2. 舊 Manager 徹底隔離
- **搬移隔離**：已將 [HeadquartersManager.cs](file:///Users/guoyutang/Desktop/Election/Assets/Scripts/Obsolete/HeadquartersManager.cs)、[CampaignProgressManager.cs](file:///Users/guoyutang/Desktop/Election/Assets/Scripts/Obsolete/CampaignProgressManager.cs)、與 [BlockProgressManager.cs](file:///Users/guoyutang/Desktop/Election/Assets/Scripts/Obsolete/BlockProgressManager.cs) 連同其 `.meta` 檔案正式移入 `Assets/Scripts/Obsolete/`。
- **維護 Clean Architecture**：核心系統與 UI（包括 `MapProgressUI`、`StartGame`、`BattleFlowController` 與 `MapNodeButton`）現在統一對齊 `GameDB` API，編譯 100% 成功。

### 3. 下一步計畫
- 🚀 **[To-Do: 實作新政策卡積木 (SpawnObjectEffect) 與測試核心好玩度]**

