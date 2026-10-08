# ARCHIVED

**Status**: Historical reference only.

This document is **NON-CANONICAL**.

Do not implement requirements from this document unless an active task explicitly restores them.

**Current sources of truth**:
- `/.agents/AGENTS.md`
- `/.agents/KIRO_PROJECT_CONTEXT.md`
- `/.agents/CURRENT_DESIGN_DECISIONS.md`

---

# Stage 4.5 最終報告 - roomNumber 參數移除 + 舊程式碼清理

**執行日期**: 2026-09-11  
**狀態**: ✅ 完成  
**ARCHIVED**: 2026-10-07

---

## 執行摘要

本階段完成 12 節點遷移的最後收尾工作：
1. 移除 `GameplayState(int roomNumber)` 參數依賴
2. 深度檢查並處理舊程式碼（UIFlowHelper, RouteDoorSpawner, MapNodeButton, HQExitTrigger）
3. 刪除確認無用的已淘汰檔案
4. 驗證完整 12 節點 Run 流程

---

## 1. GameplayState 重構

### 問題
- 舊建構子 `GameplayState(int roomNumber)` 要求呼叫端傳入房間編號
- 違反單一真相來源原則（應從 `CampaignData.CurrentNodeNumber` 讀取）
- 7 個呼叫點散落在不同檔案

### 解決方案
```csharp
// 舊方式（已移除）
new GameplayState(campaign.CurrentNodeNumber)

// 新方式
new GameplayState()  // 內部直接從 CampaignData 讀取
```

### 修改檔案（9 個）
1. **GameplayState.cs** - 移除舊建構子，新增無參數建構子
2. **TutorialState.cs** - Line 99, 164
3. **StageClearState.cs** - Line 95, 105
4. **HQExitTrigger.cs** - Line 36（改為 `new TutorialState()`）
5. **StartGame.cs** - Line 54, 84（改為 `new TutorialState()`）
6. **MapNodeButton.cs** - Line 71（改為 `new TutorialState()`）
7. **UIFlowHelper.cs** - Line 29（改為 `new TutorialState()`）
8. **OBSOLETE_WARNINGS.md** - 更新報告
9. **KIRO_PROJECT_CONTEXT.md** - 更新文檔

### 時機驗證

每個呼叫點都經過時機檢查，確認 `CurrentNodeNumber` 已正確設定：

| 檔案 | 呼叫點 | 時機 | CurrentNodeNumber 狀態 |
|------|--------|------|----------------------|
| TutorialState.cs | Line 99 | StartFormalCampaign() 後 | = 1 ✅ |
| TutorialState.cs | Line 164 | TrySelectRoute() 後 | = 1 ✅ |
| StageClearState.cs | Line 95 | ActivateFixedNode() 後 | = 下一節點 ✅ |
| StageClearState.cs | Line 105 | TrySelectRoute() 後 | = 下一節點 ✅ |
| StartGame.cs | Line 54, 84 | StartFormalCampaign() 後 | 改為 TutorialState ✅ |

---

## 2. UIFlowHelper.cs 深度檢查

### 檢查範圍
- ✅ 所有 `.unity` 場景檔案
- ✅ 所有 `.prefab` Prefab 檔案
- ✅ 檢查 UnityEvent 持久化綁定

### 發現
**場景綁定**：`S0.unity`（主選單場景）
- GameObject: `UIFlowHelper` (fileID: 472353436)
- 兩個按鈕的 `OnClick` 事件綁定到 `UIFlowHelper.GoToHQ()` 方法

**方法使用狀態**：
| 方法 | 場景綁定 | 程式碼呼叫 | 狀態 |
|------|----------|------------|------|
| GoToHQ() | ✅ S0.unity | ❌ 無 | 保留 |
| StartGameplay() | ❌ 無 | ❌ 無 | 保留 |
| GoToStageClear() | ❌ 無 | ❌ 無 | 保留 |
| GoToGameEnd() | ❌ 無 | ❌ 無 | 保留 |
| GoToMainMenu() | ❌ 無 | ❌ 無 | 保留 |

### 決策
- **移除 Obsolete 標記**（檔案仍在使用）
- **加入盤點註解**（記錄 2026-09-11 檢查結果）
- **保留所有方法**（避免場景引用丟失）

---

## 3. 舊程式碼清理

### 深度檢查清單

對每個檔案執行以下檢查：
1. ✅ 搜尋所有 `.unity` 場景檔案
2. ✅ 搜尋所有 `.prefab` Prefab 檔案
3. ✅ 搜尋所有 `.cs` 檔案中的型別引用
4. ✅ 確認無 UnityEvent 綁定
5. ✅ 確認無 MonoBehaviour 掛載
6. ✅ 確認無方法呼叫

### 檢查結果

#### RouteDoorSpawner.cs
- **類型**: `static class`（不可掛載）
- **場景綁定**: ❌ 無
- **Prefab 綁定**: ❌ 無
- **型別引用**: ❌ 無（只有 DoorController.cs 的註解提到）
- **方法呼叫**: ❌ 無
- **結論**: ✅ 可安全刪除

#### MapNodeButton.cs
- **類型**: `MonoBehaviour`
- **場景綁定**: ❌ 無
- **Prefab 綁定**: ❌ 無
- **型別引用**: ❌ 無
- **方法呼叫**: ❌ 無
- **結論**: ✅ 可安全刪除

#### HQExitTrigger.cs
- **類型**: `MonoBehaviour`
- **場景綁定**: ❌ 無（HQ 場景已改用 StartGame.cs）
- **Prefab 綁定**: ❌ 無
- **型別引用**: ❌ 無
- **方法呼叫**: ❌ 無
- **結論**: ✅ 可安全刪除

### 已刪除檔案

```
✅ Assets/Scripts/System/Mission/RouteDoorSpawner.cs
✅ Assets/Scripts/System/Mission/RouteDoorSpawner.cs.meta
✅ Assets/Scripts/System/MapNodeButton.cs
✅ Assets/Scripts/System/MapNodeButton.cs.meta (不存在)
✅ Assets/Scripts/GameFlow/HQExitTrigger.cs
✅ Assets/Scripts/GameFlow/HQExitTrigger.cs.meta
```

---

## 4. 測試驗證

### 編譯狀態
✅ **零錯誤、零警告**（除 Unity 內建 FindObjectOfType 警告）

### T 鍵完整 Run 測試

| 測試點 | 檢查項目 | 結果 |
|--------|----------|------|
| **開始戰役** | HQ → StartGame → TutorialState | ✅ 通過 |
| **教學完成** | Tutorial → 雙門選路 → 節點 1 | ✅ 通過 |
| **關卡結算（選路）** | MissionChoice 節點 → 獎勵 → 雙門 → 下一節點 | ✅ 通過 |
| **關卡結算（單門）** | Elite/Boss 前 → 獎勵 → 單門 → Fixed 節點 | ✅ 通過 |
| **節點 5 → 6** | Elite_Arena 正確載入 | ✅ 通過 |
| **節點 11 → 12** | Boss_Arena 正確載入 | ✅ 通過 |
| **Boss 擊敗** | 總結算 → 返回 HQ | ✅ 通過 |
| **資源系統** | 跨節點累積，返回 HQ 重置 | ✅ 通過 |

**測試次數**：2 次完整 Run
- 第 1 次：刪除檔案前驗證
- 第 2 次：刪除檔案後驗證

---

## 5. 文檔更新

### KIRO_PROJECT_CONTEXT.md

**更新章節**：
1. **已知限制 → 舊場景相容性**
   - 改為「已解決」
   - 移除「待清理」建議

2. **更新歷史**
   - 新增 v2.1 版本記錄
   - 記錄 roomNumber 重構和檔案刪除

### OBSOLETE_WARNINGS.md

**更新內容**：
- roomNumber 參數重構完成狀態
- 已修改檔案清單（9 個）
- 編譯狀態（無警告）
- UIFlowHelper 深度檢查結果
- 已刪除檔案清單
- 測試驗證結果

---

## 6. 關鍵決策記錄

### 為何修改「永遠不會執行」的程式碼？

**問題**：MapNodeButton.cs 和 HQExitTrigger.cs 被標記為「無呼叫」，為何還要修改它們？

**原因**：
1. 移除 `GameplayState(int)` 建構子導致**編譯錯誤**
2. Unity 編譯器檢查所有程式碼，即使不會執行
3. **兩種處理方式**：
   - 修改程式碼（讓它們編譯通過）
   - 先刪除檔案（避免修改無用程式碼）

**選擇**：修改後刪除
- 先修改讓專案編譯通過
- 深度檢查確認無綁定
- 最後刪除檔案

### 為何保留 UIFlowHelper？

**發現**：S0.unity（主選單場景）的按鈕 OnClick 綁定到 `GoToHQ()` 方法

**考量**：
1. 刪除檔案會導致場景引用丟失（Missing Script）
2. 需要重新配置 S0.unity 場景的按鈕
3. GoToHQ() 功能仍然有效且必要

**決策**：保留整個類別
- 移除 Obsolete 標記
- 保留所有方法（避免場景編輯風險）
- 加入盤點註解

---

## 7. 修改影響範圍

### 已修改檔案（6 個）
1. GameplayState.cs - 建構子重構
2. TutorialState.cs - 2 處呼叫
3. StageClearState.cs - 2 處呼叫
4. StartGame.cs - 2 處呼叫
5. UIFlowHelper.cs - 移除 Obsolete + 修改呼叫
6. KIRO_PROJECT_CONTEXT.md - 更新文檔

### 已刪除檔案（3 個 + 2 個 .meta）
1. RouteDoorSpawner.cs + .meta
2. MapNodeButton.cs（.meta 不存在）
3. HQExitTrigger.cs + .meta

### 已標記 Obsolete 但保留
- UIFlowHelper.cs（移除標記）

---

## 8. 後續建議

### ✅ 已完成
- 12 節點架構穩定運行
- roomNumber 參數完全移除
- 舊程式碼清理完成
- 文檔更新完整

### 🔧 可選優化（非必要）
1. **Unity API 更新**
   - `FindObjectOfType` → `FindFirstObjectByType`
   - 影響檔案：PoolManager.cs, EnemyController.cs
   - 優先級：低（Unity 內建警告，不影響功能）

2. **S0.unity 場景重構**
   - 考慮移除 UIFlowHelper 依賴
   - 直接在按鈕腳本中呼叫 GameFlowManager
   - 優先級：低（現有方案運作正常）

3. **DoorController.cs 註解更新**
   - Line 94 註解仍提到 RouteDoorSpawner
   - 建議改為「由 RoomExitController 配置」
   - 優先級：低（只是註解）

---

## 9. 驗收標準

| 標準 | 狀態 | 證據 |
|------|------|------|
| 所有 GameplayState(int) 呼叫已移除 | ✅ | grep 搜尋結果 |
| 無 roomNumber 相關 Obsolete 警告 | ✅ | 編譯 Log |
| 舊檔案已刪除 | ✅ | 檔案系統檢查 |
| 無場景/Prefab 綁定殘留 | ✅ | 全專案搜尋 |
| 完整 12 節點 Run 測試通過 | ✅ | T 鍵測試（2 次） |
| 文檔更新完整 | ✅ | KIRO_PROJECT_CONTEXT.md |
| 零編譯錯誤 | ✅ | Unity Console |

---

## 10. 相關文檔

1. **MISSION_TIMING_FIX.md** - MissionTracker 時序競爭修復
2. **OBSOLETE_WARNINGS.md** - 已淘汰程式碼清單和最終狀態
3. **STAGE3_FINAL_REPORT.md** - 12 節點架構驗證
4. **KIRO_PROJECT_CONTEXT.md** - 專案架構文檔（已更新至 v2.1）

---

**報告完成日期**: 2026-09-11  
**執行者**: Kiro AI Agent  
**狀態**: ✅ Stage 4.5 完成，8→12 節點遷移正式收尾
