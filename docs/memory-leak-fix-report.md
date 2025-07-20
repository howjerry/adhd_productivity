# Timer Store 記憶體洩漏修復報告

## 問題描述
`useTimerStore` 中使用了全域變數 `timerInterval` 來管理計時器的 interval，這可能導致以下問題：
1. 記憶體洩漏：當 store 被多次創建或元件重新渲染時，舊的 interval 可能不會被正確清理
2. 狀態不一致：全域變數與 store 狀態分離，難以追蹤和管理

## 解決方案

### 1. 將 interval 移入 store state
- 將 `timerInterval` 從全域變數改為 store state 的一部分
- 在 `TimerStoreState` interface 中新增 `timerInterval: ReturnType<typeof setInterval> | null`
- 初始狀態中設置 `timerInterval: null`

### 2. 更新所有 interval 相關操作
修改了以下方法以使用 state 中的 interval：
- `start()`: 在 state 中創建和儲存 interval
- `pause()`: 清理 state 中的 interval
- `resume()`: 在 state 中重新創建 interval
- `stop()`: 清理 state 中的 interval
- `reset()`: 透過呼叫 `stop()` 確保 interval 被清理

### 3. 更新清理邏輯
- 保留 `beforeunload` 事件監聽器
- 更新為從 store state 中獲取 interval 進行清理

## 測試覆蓋

創建了全面的單元測試 (`useTimerStore.test.ts`)，包含：

### 基本功能測試
- Timer 啟動和停止
- 暫停和繼續功能
- Timer tick 功能
- 重置功能
- 完成功能
- 模式切換

### 記憶體洩漏防護測試
- 多次啟動和停止不造成記憶體洩漏
- 重新啟動前清理現有的 interval
- 組件卸載時清理 interval

### 測試結果
所有 16 個測試案例均通過，確認：
- Interval 正確地被創建和清理
- 沒有殘留的 interval 造成記憶體洩漏
- 狀態管理正確且一致

## 影響範圍
- 此修改只影響 `useTimerStore` 的內部實作
- API 保持不變，不影響使用此 store 的元件
- 提升了程式碼的可維護性和可測試性

## 建議
1. 監控生產環境中的記憶體使用情況
2. 考慮在其他使用 interval 或 timeout 的 store 中採用類似模式
3. 定期執行測試以確保沒有引入新的記憶體洩漏問題