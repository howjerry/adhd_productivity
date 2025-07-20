# ADHD 生產力系統 API 文檔

## 概述

ADHD 生產力系統 API 專為 ADHD 使用者設計，提供針對執行功能挑戰優化的任務管理、專注會話和生產力分析功能。

## 🎯 以 ADHD 為中心的設計理念

我們的 API 設計優先考慮：
- **認知負荷減少**：簡化的請求/響應結構
- **執行功能支援**：內建任務分解和優先順序
- **時間管理**：進階調度和提醒系統
- **多巴胺調節**：獎勵系統和進度追蹤
- **情感安全**：溫和的錯誤處理和確認模式

## 📚 文檔資源

### API 規格
- **[OpenAPI 規格](./openapi.yml)** - OpenAPI 3.0 格式的完整 API 定義
- **[API 合約](./api-contracts.md)** - 端點和範例的快速參考

### 整合指南
- **快速入門** - 開發人員快速開始指南
- **SDK 文檔** - 官方 SDK 和函式庫
- **程式碼範例** - 多種語言的實作範例

## 🚀 快速開始

### 系統環境
- **開發環境**: http://localhost:5000
- **生產環境**: http://localhost
- **API 前綴**: /api

### 1. 身份驗證

所有受保護的 API 請求都需要在 Authorization 標頭中包含 JWT 令牌：

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "demo@adhd.dev",
    "password": "demo123"
  }'
```

### 2. 建立您的第一個任務

```bash
curl -X POST http://localhost:5000/api/tasks \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "完成專案提案",
    "description": "撰寫第一季專案提案",
    "priority": "High",
    "estimatedDuration": 60
  }'
```

### 3. 開始計時會話

```bash
curl -X POST http://localhost:5000/api/timer/sessions \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "taskId": "your-task-id",
    "duration": 25,
    "type": "Focus"
  }'
```

## 🔧 API 功能

### 任務管理 (Tasks)
- **智能分解**：自動任務分解為複雜專案
- **基於能量的過濾**：匹配任務到當前能量水平
- **上下文切換警告**：最小化認知負荷
- **優先順序矩陣**：ADHD 友善的優先順序系統

### 專注會話 (Timer Sessions)
- **可自訂持續時間**：適應個人注意力跨度
- **中斷處理**：溫和的暫停/恢復功能
- **背景聲音**：提升專注的音頻選項
- **進度追蹤**：即時會話監控

### 快速捕獲 (Capture)
- **即時記錄**：快速捕獲想法和任務
- **ADHD 優化**：減少摩擦的輸入介面
- **智能分類**：自動分類和組織

### 分析洞察 (Analytics)
- **注意力模式**：識別最佳專注時間
- **生產力趨勢**：追蹤改進進展
- **ADHD 特定指標**：專為 ADHD 使用者的分析
- **多巴胺追蹤**：監控獎勵系統效果

## 🔒 安全性與隱私

### 安全功能
- **JWT 身份驗證**：安全的基於令牌的身份驗證
- **速率限制**：防止濫用的保護
- **輸入驗證**：全面的資料清理
- **HTTPS 加密**：所有通訊加密

### 隱私考量
- **資料最小化**：僅收集必要資訊
- **ADHD 敏感資料**：增強心理健康資訊保護
- **使用者控制**：細緻的隱私設定
- **GDPR 合規**：完全符合資料保護法規

## 📊 速率限制

| 端點類型 | 速率限制 | 視窗 |
|----------|----------|------|
| 身份驗證 | 5 次請求 | 1 分鐘 |
| 標準 API | 60 次請求 | 1 分鐘 |
| 檔案上傳 | 10 次請求 | 1 分鐘 |
| 分析 | 20 次請求 | 1 分鐘 |

## 🌍 環境

| 環境 | 基礎 URL | 用途 |
|------|----------|------|
| 生產 | `http://localhost` | 實際系統 |
| 開發 | `http://localhost:5000` | 本地開發 |

## 📱 實際實作的端點

### 身份驗證 (AuthController)
```
POST /api/auth/register    # 使用者註冊
POST /api/auth/login       # 使用者登入
POST /api/auth/refresh     # 刷新令牌
GET  /api/auth/me          # 取得當前使用者資訊
```

### 任務管理 (TasksController)
```
GET    /api/tasks                    # 取得使用者任務（支援過濾和分頁）
POST   /api/tasks                    # 建立新任務
GET    /api/tasks/{id}              # 取得特定任務
PUT    /api/tasks/{id}              # 更新任務
DELETE /api/tasks/{id}              # 刪除任務
```

### 計時器會話 (TimerController)
```
GET    /api/timer/sessions          # 取得計時器會話
POST   /api/timer/sessions          # 開始新的計時器會話
GET    /api/timer/sessions/{id}     # 取得特定會話
PUT    /api/timer/sessions/{id}     # 更新會話
DELETE /api/timer/sessions/{id}     # 刪除會話
```

### 快速捕獲 (CaptureController)
```
GET    /api/capture                 # 取得捕獲項目
POST   /api/capture                 # 建立新的捕獲項目
PUT    /api/capture/{id}            # 更新捕獲項目
DELETE /api/capture/{id}            # 刪除捕獲項目
```

## 🔄 WebSocket 事件 (SignalR Hubs)

即時更新通過 SignalR 連接提供：

### Timer Hub (`/hubs/timer`)
```javascript
// 計時器事件
connection.on('TimerStarted', (session) => {});
connection.on('TimerCompleted', (session) => {});
connection.on('TimerInterrupted', (session) => {});
```

### Notification Hub (`/hubs/notification`)
```javascript
// 通知事件
connection.on('NotificationReceived', (notification) => {});
connection.on('TaskReminder', (reminder) => {});
```

## 📈 監控與狀態

- **API 狀態**: /health 端點
- **效能指標**: 可在開發者儀表板中查看
- **事故報告**: 服務中斷的自動通知

## 🆘 支援與協助

### 技術支援
- **回應時間**: 一般查詢 24 小時，緊急問題 4 小時
- **開發者入口**: 提供完整的文檔和範例

## 🔄 版本更新與版本控制

### 當前版本: v1.0.0

我們遵循[語義版本控制](https://semver.org/)：
- **主要版本**: 破壞性變更
- **次要版本**: 新功能，向後相容
- **補丁版本**: 錯誤修復和改進

### 近期更新
- **v1.0.0** (2024-12-22): 初始公開發布
  - 完整的任務管理 API
  - 專注會話追蹤
  - 基本分析端點
  - WebSocket 即時更新
  - 快速捕獲功能

### 棄用政策
- **30 天通知** 用於次要破壞性變更
- **90 天通知** 用於主要版本變更
- **遷移指南** 為所有破壞性變更提供

## 🧪 測試

### API 測試工具
- **Swagger UI**: http://localhost:5000 (開發環境)
- **健康檢查**: http://localhost:5000/health

### 測試帳號
```
Demo 帳號: demo@adhd.dev / demo123
Test 帳號: test@adhd.dev / test123
Admin 帳號: admin@adhd.dev / admin123
```

## 🤝 貢獻

我們歡迎貢獻來改進我們的 API 文檔：

1. **Fork** 文檔儲存庫
2. **建立** 功能分支
3. **進行** 改進
4. **提交** Pull Request

### 文檔標準
- 使用清晰、簡潔的語言
- 包含 ADHD 特定考量
- 提供實際範例
- 測試所有程式碼範例

## 📄 授權

此 API 文檔依照 [MIT 授權](./LICENSE) 授權。

---

**需要協助？** 
- 📧 電子郵件: api-support@adhd-productivity.dev
- 💬 聊天: 可在開發者入口中使用

**最後更新**: 2024年12月22日  
**文檔版本**: 1.0.0