# ARCHIVED

**Status**: Historical reference only.

This document is **NON-CANONICAL**.

Do not implement requirements from this document unless an active task explicitly restores them.

**Current sources of truth**:
- `/.agents/AGENTS.md`
- `/.agents/KIRO_PROJECT_CONTEXT.md`
- `/.agents/CURRENT_DESIGN_DECISIONS.md`

---

# Obsolete 警告清單

## 最後更新
2026-09-11（Stage 4.5 完成）

## ARCHIVED 2026-10-07
此文件已被歸檔。所有當前架構資訊請參考 `/KIRO_PROJECT_CONTEXT.md`。

## ✅ roomNumber 參數重構完成

所有 `GameplayState(int roomNumber)` 呼叫已移除，改為：
- `new GameplayState()`（無參數，從 CampaignData 讀取當前節點）
- `new TutorialState()`（開始新 Run 時的入口）

### 已修改檔案（7 個）
1. ✅ **GameplayState.cs** - 移除舊建構子，新增無參數建構子
2. ✅ **TutorialState.cs** Line 99, 164 - 改為 `new GameplayState()`
3. ✅ **StageClearState.cs** Line 95, 105 - 改為 `new GameplayState()`
4. ✅ **HQExitTrigger.cs** Line 36 - 改為 `new TutorialState()`（檔案已標記 Obsolete）
5. ✅ **StartGame.cs** Line 54, 84 - 改為 `new TutorialState()`
6. ✅ **MapNodeButton.cs** Line 71 - 改為 `new TutorialState()`（檔案已標記 Obsolete）
7. ✅ **UIFlowHelper.cs** Line 29 - 改為 `new TutorialState()`

### 編譯狀態
- ❌ 舊警告：7 個檔案產生 CS0618 警告（已修復）
- ✅ **新狀態：無任何 roomNumber 相關 Obsolete 警告**

---

## UIFlowHelper.cs 狀態

**結論**：保留，移除 Obsolete 標記

**原因**：
- `GoToHQ()` 方法被 S0.unity（主選單場景）的按鈕 OnClick 使用
- 全專案掃描確認其他方法無 UnityEvent 綁定

**註解**：已加入 2026-09-11 盤點記錄

---

## 已標記 Obsolete 的檔案

### 完全無呼叫（可安全刪除）

1. ✅ **RouteDoorSpawner.cs** - 無任何程式碼或場景綁定
2. ✅ **MapNodeButton.cs** - 無任何程式碼或場景綁定

### 有場景綁定（暫時保留）

3. ⚠️ **UIFlowHelper.cs** - 已**移除** Obsolete 標記
   - `GoToHQ()` 被 S0.unity 使用
   - 其他方法無綁定

### 舊系統已廢棄

4. ✅ **HQExitTrigger.cs** - 已標記 Obsolete（由 StartGame.cs 取代）

---

## 其他 Unity 內建 Obsolete 警告（不相關）

以下是 Unity 自身的 API 淘汰警告，與本專案的 12 節點系統無關：

- **Object.FindObjectOfType()** 警告（Unity 建議改用 FindFirstObjectByType）
  - PoolManager.cs Line 24
  - EnemyController.cs Lines 284, 374, 395
  
**說明**: Unity 2023+ 版本的 API 變更，非本專案任務系統相關

---

## 測試驗證

✅ **T 鍵完整 Run 測試通過**（12 節點）
- 開始戰役（StartGame → TutorialState）
- 教學完成（雙門選路 → 節點 1）
- 關卡結算 - 選路（MissionChoice 節點）
- 關卡結算 - 單門（Elite/Boss 節點）
- 完整流程（Boss 戰 → 總結算 → 返回 HQ）

---

## 總結

### ✅ 已完成
- roomNumber 參數完全移除
- 所有 GameplayState 呼叫已重構
- UIFlowHelper 深度檢查完成
- HQExitTrigger 標記為廢棄
- 完整 Run 測試通過

### 📋 下一階段（等用戶確認）
刪除已標記 Obsolete 的檔案：
- RouteDoorSpawner.cs
- MapNodeButton.cs  
- HQExitTrigger.cs
