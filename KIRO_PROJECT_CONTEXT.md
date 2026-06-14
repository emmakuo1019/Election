# Election（酸宗痛）專案說明文件

> 供 AI 助理優先讀取，用於了解遊戲的製作方向與規範。
> 最後更新：2026-06-08（依據企劃書 G01 酸宗痛 企劃書.pdf 及流程圖修訂）

---

## 專案概述

- **遊戲名稱**：酸宗痛
- **引擎**：Unity（URP 渲染管線，C#）
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

| 按鍵 | 功能 |
|------|------|
| WASD | 移動 |
| L Shift | 衝刺（Dash） |
| 左鍵 / Attack | 普通攻擊（演說） |
| J / K / L | 技能（技能一、技能二、大招） |
| Q / E / R | 技能（企劃書另一版本操作方式） |
| Esc | 選單 |

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
- 預設雙方各 50 票。
- 選民被轉化時票數即時更新。
- **計時結束時票數 > 對手** → 觸發通關 / 觸發政策卡獎勵。
- **票數 ≤ 對手** → 顯示選擇快報，依比例恢復部分資金，繼續下一房間（非最後房間）。

### 2. 誠信值（HP）
- 預設值 70 / 100（`IntegrityHp = 70f`, `MaxIntegrityHp = 100f`）。
- **歸零邏輯**：
  - 誠信歸零後，深色選民開始「燃燒」（每秒減少），燃燒完才真正觸發退選（結局 D）。
  - 形象越差（低於 20%～1%）→ 敵人血量提升 10%～50%。
- 在記者監視範圍內使用**情緒政策**會扣除誠信。
- `IntegrityHpRatio` 影響選民的基礎 HP。

### 3. 資金（MP）
- 用於施放技能、造勢大招。
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
| 第一區完成後 | 技能二 | 側翼出擊（大幅減少對手選票） | 發表白皮書（短暫暈眩對手） |
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
├── System/          # 核心系統（VoteManager、BattleFlowController 等）
├── UIManager/       # UI 元件
├── Effects/         # 特效、物件池相關
├── Enemy/           # 敵方 AI
├── ScenesManager/   # 場景切換管理
└── RandomEvents/    # 隨機事件（未開發）
```

### 程式規範
- **語言**：C#，Unity 6000.x 系列。
- **Singleton 模式**：主要系統使用 `Instance` 靜態屬性，`Awake` 中防止重複建立。
- **事件系統**：使用 C# `event Action` / `event delegate` 解耦系統間通訊。
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

1. **大腦 `GameFlowManager` (全域狀態機)**
* **職責**：管理遊戲的宏觀生命週期（主選單 ➔ 總部 ➔ 戰鬥/休息 ➔ 結算 ➔ Boss）。
* **特性**：跨場景不銷毀 (`DontDestroyOnLoad`) 的 Singleton。只負責切換 `IState`，絕對不處理具體的 UI 動畫或戰鬥傷害計算。
* **狀態讀取**：對外僅提供唯讀屬性 `public IState CurrentState => stateMachine?.CurrentState;`，嚴禁外部腳本直接修改狀態。


2. **雙手 `UIManager` & 全域 Canvas**
* **職責**：純粹的視覺呈現。只提供 `ShowPanel()` 與 `HidePanel()` 以及 UI 序列的控制。
* **特性**：與大腦一樣是 `DontDestroyOnLoad`。它不知道「遊戲現在玩到哪裡」，只聽命於大腦的指揮。


3. **神經 `UIFlowHelper` & `BattleEventManager**`
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

* **訂閱時機**：在場景確定載入**完成後**（Coroutine 結束時），才向 `BattleEventManager` 訂閱事件。
* **解除時機**：**必須、一定、絕對**要在 `Exit()` 中解除訂閱（`-=`），防止舊狀態在背景繼續干擾新流程。

### 3. 轉場邏輯與分流 (Edge Cases 防禦)

在寫狀態切換邏輯時，必須考慮極限值與特殊流程。例如從 `SafeRoomState` (安全房) 離開時，必須檢查 `if (roomNumber == 15) { 進入Boss }`，防止無縫切換時不小心跳過主線重要事件。

---

## 🚫 三、 避坑指南與嚴禁寫法 (Anti-Patterns)

為了避免架構退化回義大利麵條代碼（Spaghetti Code），嚴禁使用以下寫法：

### ❌ 嚴禁使用 Checklist Pattern 控制 UI (Update 裡的 Booleans)

* **錯誤示範**：在 State 裡面寫 `bool isDataClosed`，然後在 `Update()` 裡面一直 `if(isDataClosed)`。這違反開閉原則，且效能低落。
* **✅ 正確做法 (委派回呼 Action)**：使用 UI Sequence Controller 模式。由 State 呼叫 `UIManager.Instance.StartSequence(Action onComplete)`，把「展演完畢後要切換狀態的邏輯」當作參數傳給 UI，等 UI 播完後自己 `Invoke()` 呼叫它。

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