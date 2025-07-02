# ADHD 生產力管理系統

專為 ADHD 使用者設計的生產力管理系統，提供完整的任務管理、時間追蹤和認知負荷優化功能。

## 🚀 快速開始

### 前置需求
- Docker Desktop

### 一鍵啟動
```bash
# 1. 下載專案
git clone [repository-url]
cd ADHD

# 2. 啟動系統
docker-compose up -d

# 3. 等待服務啟動 (大約 1-2 分鐘)
docker-compose logs -f

# 4. 開啟瀏覽器
```

### 系統訪問
- **前端應用**: http://localhost:3000
- **後端 API**: http://localhost:5000
- **API 文件**: http://localhost:5000/swagger
- **資料庫管理**: http://localhost:5050

### 預設帳號
- **應用程式**: demo@adhd.dev / demo123
- **資料庫管理**: admin@adhd.dev / admin123

## 📋 系統特色

### ADHD 專用設計
- ✅ 視覺時間管理
- ✅ 認知負荷優化
- ✅ 情緒調節支援
- ✅ 執行功能輔助

### 核心功能
- 🎯 智能任務分解
- ⏰ 番茄鐘技術整合
- 📊 專注力統計分析
- 🔔 溫和提醒系統
- 🎨 ADHD 友善 UI/UX

### 技術架構
- **前端**: React + TypeScript + Vite
- **後端**: ASP.NET Core 8
- **資料庫**: PostgreSQL 16
- **快取**: Redis 7
- **反向代理**: Nginx
- **容器化**: Docker Compose

## 🛠️ 開發指南

### 常用命令
```bash
# 啟動系統
docker-compose up -d

# 查看日誌
docker-compose logs -f [service-name]

# 停止系統
docker-compose down

# 重新建置
docker-compose up --build -d

# 重置系統 (清除資料)
docker-compose down -v
```

### 服務狀態檢查
```bash
# 檢查所有服務
docker-compose ps

# 檢查健康狀態
curl http://localhost:5000/health

# 連接資料庫
docker exec -it adhd-postgres psql -U adhd_user -d adhd_productivity
```

### 自訂設定
如需修改設定，請複製 `.env.example` 為 `.env` 並編輯：
```bash
cp .env.example .env
# 編輯 .env 檔案中的設定值
```

## 🗄️ 資料庫

系統使用 PostgreSQL 作為主資料庫，所有資料會自動持久化在 Docker volumes 中。

### 資料庫操作
```bash
# 連接資料庫
docker exec -it adhd-postgres psql -U adhd_user -d adhd_productivity

# 查看所有表格
docker exec -it adhd-postgres psql -U adhd_user -d adhd_productivity -c "\dt"

# 備份資料庫
docker exec -it adhd-postgres pg_dump -U adhd_user adhd_productivity > backup.sql

# 還原資料庫
cat backup.sql | docker exec -i adhd-postgres psql -U adhd_user -d adhd_productivity
```

## 🔧 故障排除

### 常見問題

**1. 埠口衝突**
```bash
# 修改 .env 檔案中的埠口設定
BACKEND_PORT=5001
FRONTEND_PORT=3001
```

**2. 容器啟動失敗**
```bash
# 查看詳細錯誤日誌
docker-compose logs [service-name]

# 重新建置容器
docker-compose up --build -d
```

**3. 資料庫連線問題**
```bash
# 檢查資料庫容器狀態
docker ps | grep postgres

# 重啟資料庫服務
docker-compose restart adhd-postgres
```

**4. 完全重置**
```bash
# 停止並移除所有容器和資料
docker-compose down -v
docker system prune -f

# 重新啟動
docker-compose up -d
```

## 📚 相關文件

- [系統分析與設計書](./系統分析與設計書.md) - 完整技術規格
- [ADHD 生產力系統建構指南](./ADHD%20生產力系統建構指南_.md) - 系統設計理念
- [視覺設計指南](./視覺設計指南.md) - UI/UX 設計規範
- [CLAUDE.md](./CLAUDE.md) - 開發者指南

## 🔐 安全性

- JWT 認證機制
- 密碼加密存儲
- CORS 安全設定
- SQL 注入防護
- XSS 防護機制

## 📄 授權

此專案採用 MIT 授權條款。

## 🤝 貢獻

歡迎提交 Issue 和 Pull Request 來改善系統。

---

**開發團隊**: ADHD 生產力系統團隊  
**版本**: 1.0.0  
**最後更新**: 2024