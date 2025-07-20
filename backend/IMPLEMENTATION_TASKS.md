# ADHD 生產力系統實施任務清單

基於深度靜態分析報告，以下是按優先順序排列的編碼任務清單。每個任務都是具體的編碼活動，可由編碼代理執行。

## 第一階段：立即修復關鍵問題（高優先級）

### 1. 修復後端 N+1 查詢問題

- [ ] 1.1 為 GetTasksQueryHandler.cs 編寫效能測試
  - 創建 `GetTasksQueryHandlerPerformanceTests.cs`
  - 編寫測試驗證當前查詢產生的 SQL 數量
  - 使用 InMemory 資料庫和日誌記錄追蹤查詢

- [ ] 1.2 重構 GetTasksQueryHandler 使用投影查詢
  - 修改 `GetTasksQueryHandler.cs` 第 103-109 行
  - 將循環中的映射改為 LINQ 投影
  - 確保單一查詢即可獲取所有資料

- [ ] 1.3 為資料庫添加複合索引
  - 在 `ApplicationDbContext.cs` 的 OnModelCreating 方法中添加索引
  - 創建 `UserId + Status + Priority` 複合索引
  - 創建 `UserId + DueDate` 複合索引

### 2. 實作 Refresh Token 持久化機制

- [ ] 2.1 創建 RefreshToken 實體類別
  - 在 `Domain/Entities/` 創建 `RefreshToken.cs`
  - 包含 Id、UserId、Token、ExpiresAt、CreatedAt、DeviceId、IsRevoked 屬性
  - 繼承自 BaseEntity

- [ ] 2.2 更新資料庫上下文和遷移
  - 在 `ApplicationDbContext.cs` 添加 RefreshTokens DbSet
  - 創建新的 EF Core 遷移
  - 為 Token 欄位添加唯一索引

- [ ] 2.3 重構 AuthController 的 Refresh 端點
  - 修改 `AuthController.cs` 的 RefreshToken 方法
  - 實作資料庫查詢驗證 refresh token
  - 實作撤銷舊 token 的邏輯

- [ ] 2.4 為 Refresh Token 功能編寫整合測試
  - 創建 `RefreshTokenIntegrationTests.cs`
  - 測試正常更新流程
  - 測試過期和撤銷的 token

### 3. 實作 API 速率限制

- [ ] 3.1 配置 ASP.NET Core 速率限制中間件
  - 在 `Program.cs` 中添加 AddRateLimiter 服務
  - 配置固定視窗限制策略
  - 為認證端點設定更嚴格的限制

- [ ] 3.2 為控制器端點應用速率限制
  - 在 `AuthController.cs` 的 Login 方法添加 [EnableRateLimiting("auth")]
  - 在其他控制器添加 [EnableRateLimiting("api")]
  - 創建自訂速率限制回應

- [ ] 3.3 編寫速率限制測試
  - 創建 `RateLimitingTests.cs`
  - 測試超過限制時的回應
  - 測試限制重置行為

### 4. 修復前端記憶體洩漏

- [ ] 4.1 重構 useTimerStore 的 interval 管理
  - 修改 `src/stores/useTimerStore.ts`
  - 將全域 timerInterval 變數移入 store state
  - 實作適當的清理機制

- [ ] 4.2 創建 Timer 清理的單元測試
  - 創建 `useTimerStore.test.ts`
  - 測試 timer 啟動和停止
  - 驗證 interval 正確清理

## 第二階段：架構重構（中優先級）

### 5. 實作 Repository Pattern

- [ ] 5.1 創建通用 Repository 介面
  - 在 `Application/Common/Interfaces/` 創建 `IRepository.cs`
  - 定義基本 CRUD 操作
  - 包含 IQueryable 支援用於複雜查詢

- [ ] 5.2 實作通用 Repository
  - 在 `Infrastructure/Data/` 創建 `Repository.cs`
  - 實作 IRepository 介面
  - 使用 DbContext 進行資料存取

- [ ] 5.3 創建特定領域的 Repository
  - 創建 `ITaskRepository.cs` 和 `TaskRepository.cs`
  - 添加特定於任務的查詢方法
  - 實作包含子任務的高效查詢

- [ ] 5.4 重構 Handlers 使用 Repository
  - 修改所有 Command/Query Handlers
  - 注入 Repository 而非 DbContext
  - 更新依賴注入配置

### 6. 實作 Unit of Work Pattern

- [ ] 6.1 創建 Unit of Work 介面和實作
  - 創建 `IUnitOfWork.cs` 在 Application 層
  - 創建 `UnitOfWork.cs` 在 Infrastructure 層
  - 管理交易和 SaveChanges

- [ ] 6.2 整合 Unit of Work 到 Handlers
  - 修改 Command Handlers 使用 UnitOfWork
  - 確保交易的原子性
  - 處理交易失敗的情況

### 7. 重構前端大型元件

- [ ] 7.1 拆分 PriorityMatrix 元件
  - 創建 `PriorityMatrix/` 資料夾結構
  - 抽取 `MatrixQuadrant.tsx` 為獨立檔案
  - 抽取 `MatrixTaskCard.tsx` 為獨立檔案
  - 抽取 `MatrixFilters.tsx` 為獨立檔案

- [ ] 7.2 創建自訂 Hooks 管理邏輯
  - 創建 `hooks/useFilteredTasks.ts`
  - 創建 `hooks/useTaskDragDrop.ts`
  - 將業務邏輯從元件移至 hooks

- [ ] 7.3 優化 VisualTimer 元件
  - 創建 `hooks/useTimer.ts` 抽取計時邏輯
  - 拆分設定面板為 `TimerSettings.tsx`
  - 實作 React.memo 優化渲染

- [ ] 7.4 為重構的元件編寫測試
  - 創建各元件的測試檔案
  - 測試 props 和事件處理
  - 測試自訂 hooks 的邏輯

### 8. 統一前端狀態管理

- [ ] 8.1 創建中央 Store 管理器
  - 創建 `stores/RootStore.ts`
  - 實作 store 之間的協調
  - 處理跨 store 的反應

- [ ] 8.2 實作統一的 API 客戶端
  - 創建 `services/ApiClient.ts`
  - 集中處理認證標頭
  - 實作錯誤處理和重試邏輯

- [ ] 8.3 整合 SignalR 與 Stores
  - 創建 `stores/RealtimeSync.ts`
  - 實作 SignalR 事件到 store 更新的映射
  - 處理連線中斷和重連

## 第三階段：效能優化（中優先級）

### 9. 實作 Redis 快取層

- [ ] 9.1 創建快取服務介面
  - 創建 `Application/Common/Interfaces/ICacheService.cs`
  - 定義 Get、Set、Remove 等操作
  - 支援過期時間和標籤

- [ ] 9.2 實作 Redis 快取服務
  - 更新 `Infrastructure/` 中的 Redis 實作
  - 實作快取 Aside 模式
  - 實作標籤式快取失效

- [ ] 9.3 在 Handlers 中應用快取
  - 修改 GetTasksQueryHandler 使用快取
  - 實作快取失效策略
  - 為快取編寫測試

### 10. 前端效能優化

- [ ] 10.1 實作路由級程式碼分割
  - 修改 `App.tsx` 使用 React.lazy
  - 為每個頁面元件實作懶加載
  - 添加載入狀態元件

- [ ] 10.2 實作虛擬滾動
  - 安裝並配置 react-window
  - 在任務列表實作虛擬滾動
  - 優化大量資料的渲染效能

- [ ] 10.3 優化 Bundle 大小
  - 分析並移除未使用的依賴
  - 實作 tree shaking
  - 配置生產環境優化

## 第四階段：測試覆蓋率提升（低優先級）

### 11. 提升前端測試覆蓋率

- [ ] 11.1 為 Store 編寫完整測試
  - 完善 `useAuthStore.test.ts`
  - 創建 `useTaskStore.test.ts`
  - 測試所有 actions 和狀態變化

- [ ] 11.2 為關鍵元件編寫測試
  - 為 QuickCapture 編寫測試
  - 為 Header 和 Sidebar 編寫測試
  - 達到 80% 以上覆蓋率

### 12. 完善後端測試

- [ ] 12.1 修復測試專案配置
  - 更新 `AdhdProductivitySystem.Tests.csproj`
  - 確保所有依賴正確配置
  - 修復測試執行問題

- [ ] 12.2 為新功能編寫單元測試
  - 為 Repository 實作編寫測試
  - 為新的 Handler 邏輯編寫測試
  - 為安全功能編寫測試

- [ ] 12.3 創建端到端測試
  - 設定測試資料庫
  - 編寫主要使用案例的測試
  - 測試完整的請求流程

## 實施注意事項

1. **每個任務都應該**：
   - 先編寫測試
   - 實作功能
   - 確保測試通過
   - 進行程式碼審查

2. **依賴關係**：
   - 第一階段的任務可以並行進行
   - 第二階段依賴部分第一階段的完成
   - 第三和第四階段可以並行進行

3. **驗證標準**：
   - 所有測試必須通過
   - 不能降低現有的測試覆蓋率
   - 效能測試必須顯示改進