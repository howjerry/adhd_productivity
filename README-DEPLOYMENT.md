# ADHD 生產力系統 - 一鍵部署

🚀 **完全容器化的 ADHD 生產力管理系統，無需任何外部依賴，一鍵即可運行！**

## ⚡ 快速開始

### 系統需求
- Docker Desktop 或 Docker Engine
- 4GB+ RAM (建議 8GB)
- 10GB+ 硬碟空間

### 一鍵安裝

#### 🐧 Linux / 🍎 macOS
```bash
curl -fsSL https://raw.githubusercontent.com/your-repo/ADHD/main/install.sh | bash
```

#### 🪟 Windows
```cmd
curl -fsSL https://raw.githubusercontent.com/your-repo/ADHD/main/install.bat -o install.bat && install.bat
```

### 手動安裝
```bash
git clone https://github.com/your-username/ADHD.git
cd ADHD
docker-compose -f docker-compose.self-contained.yml up -d --build
```

## 🌐 訪問系統

安裝完成後，開啟瀏覽器訪問：
- **主要應用**: http://localhost
- **API 文檔**: http://localhost/api/swagger
- **資料庫管理**: http://localhost:5050 (可選)

## 📋 系統特色

✅ **完全容器化** - 所有服務都在 Docker 中運行  
✅ **零外部依賴** - 無需安裝 PostgreSQL、Redis 等  
✅ **一鍵安裝** - 自動處理所有配置和初始化  
✅ **資料持久化** - 使用 Docker volumes 保存資料  
✅ **健康檢查** - 自動監控服務狀態  
✅ **清理腳本** - 一鍵清理和重建  

## 🛠️ 常用指令

```bash
# 查看狀態
docker-compose -f docker-compose.self-contained.yml ps

# 查看日誌
docker-compose -f docker-compose.self-contained.yml logs -f

# 停止服務
docker-compose -f docker-compose.self-contained.yml down

# 重啟服務
docker-compose -f docker-compose.self-contained.yml up -d

# 完全清理 (⚠️ 會刪除所有資料)
./scripts/cleanup.sh    # Linux/macOS
scripts\cleanup.bat     # Windows

# 重建系統
./scripts/rebuild.sh    # Linux/macOS
```

## 🏗️ 系統架構

```
用戶 ➜ Nginx ➜ Frontend (React) + Backend (.NET) ➜ PostgreSQL + Redis
```

所有服務運行在內部 Docker 網路中，只有 Nginx (端口 80) 對外暴露。

## 🔧 故障排除

### 端口衝突
```bash
# 檢查端口 80 是否被佔用
lsof -i :80          # Linux/macOS
netstat -ano | findstr :80    # Windows
```

### 記憶體不足
- 增加 Docker Desktop 記憶體限制 (建議 4GB+)
- 關閉其他應用程式
- 使用 `docker system prune -af` 清理資源

### 服務無法啟動
```bash
# 查看詳細日誌
docker-compose -f docker-compose.self-contained.yml logs

# 重新建構
docker-compose -f docker-compose.self-contained.yml build --no-cache
```

## 📚 詳細文檔

查看 [DEPLOYMENT.md](./DEPLOYMENT.md) 獲取完整的部署指南和故障排除。

## 🆘 獲得幫助

- 📄 [詳細部署指南](./DEPLOYMENT.md)
- 🐛 [回報問題](https://github.com/your-username/ADHD/issues)
- 💬 [討論區](https://github.com/your-username/ADHD/discussions)

---

**讓我們一起提升 ADHD 群體的生產力！** 🎯