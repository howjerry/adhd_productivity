# ADHD 生產力系統 - 完整部署指南

## 📋 目錄
1. [系統概述](#系統概述)
2. [架構說明](#架構說明)
3. [環境需求](#環境需求)
4. [快速部署](#快速部署)
5. [環境變數配置](#環境變數配置)
6. [詳細安裝步驟](#詳細安裝步驟)
7. [生產環境部署](#生產環境部署)
8. [監控與維護](#監控與維護)
9. [故障排除](#故障排除)
10. [安全配置](#安全配置)

## 🎯 系統概述

ADHD 生產力系統是一個完全容器化的應用程式，專為 ADHD 使用者設計。系統採用微服務架構，包含前端 React 應用、ASP.NET Core API 後端、PostgreSQL 資料庫和 Redis 快取。

### 主要特色
- **完全容器化**：所有組件都在 Docker 容器中運行
- **一鍵部署**：使用 Docker Compose 快速啟動
- **自包含系統**：不依賴外部服務
- **ADHD 優化**：專門為 ADHD 使用者設計的 UI/UX
- **即時通信**：使用 SignalR 的 WebSocket 支援

## 🏗️ 架構說明

```
┌─────────────────────────────────────────────────┐
│                 Nginx (Port 80)                │ ← 唯一對外暴露端口
│            反向代理和負載均衡                      │
└─────────────────┬───────────────────────────────┘
                  │
    ┌─────────────┼─────────────┐
    │             │             │
┌───▼────┐   ┌───▼────┐   ┌───▼────┐
│Frontend│   │Backend │   │Database│
│React   │   │ASP.NET │   │Postgres│
│內部端口 │   │內部端口 │   │內部端口 │
│3000    │   │5000    │   │5432    │
└────────┘   └────┬───┘   └────────┘
                  │
             ┌────▼────┐
             │  Redis  │
             │ Cache   │
             │內部端口  │
             │  6379   │
             └─────────┘
```

### 服務組件

| 服務 | 技術棧 | 功能 | 內部端口 |
|------|--------|------|----------|
| **前端** | React + TypeScript + Vite | 使用者介面，PWA 支援 | 3000 |
| **後端** | ASP.NET Core 8 + C# | RESTful API，SignalR Hub | 5000 |
| **資料庫** | PostgreSQL 16 | 主要資料存儲 | 5432 |
| **快取** | Redis 7 | 會話快取，即時資料 | 6379 |
| **代理** | Nginx | 反向代理，SSL 終止 | 80/443 |

## 💻 環境需求

### 最低系統需求
- **CPU**: 2核心
- **記憶體**: 4GB RAM (建議 8GB+)
- **硬碟**: 10GB 可用空間
- **網路**: 穩定的網路連接

### 軟體需求
- **作業系統**: 
  - Windows 10/11 (with WSL2)
  - macOS 10.14+
  - Ubuntu 18.04+ / Debian 10+
  - CentOS 7+ / RHEL 7+
- **Docker**: Docker Engine 20.10+ 或 Docker Desktop
- **Docker Compose**: v2.0+
- **Git**: 用於克隆專案

### 支援的瀏覽器
- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

## 🚀 快速部署

### 方法 1: 一鍵部署腳本 (推薦)

**Linux/macOS:**
```bash
# 下載並執行安裝腳本
curl -fsSL https://raw.githubusercontent.com/your-org/adhd-productivity/main/install.sh | bash

# 或手動執行
wget https://raw.githubusercontent.com/your-org/adhd-productivity/main/install.sh
chmod +x install.sh
./install.sh
```

**Windows PowerShell:**
```powershell
# 下載並執行安裝腳本
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/your-org/adhd-productivity/main/install.ps1" -OutFile "install.ps1"
PowerShell -ExecutionPolicy Bypass -File install.ps1
```

### 方法 2: 手動部署

```bash
# 1. 克隆專案
git clone https://github.com/your-org/adhd-productivity.git
cd adhd-productivity

# 2. 複製環境變數模板
cp .env.example .env

# 3. 編輯環境變數 (見下方配置說明)
nano .env

# 4. 啟動系統
docker-compose up -d

# 5. 查看狀態
docker-compose ps
```

### 驗證部署

```bash
# 檢查所有服務狀態
docker-compose ps

# 檢查健康狀況
curl http://localhost/health

# 查看日誌
docker-compose logs -f
```

## ⚙️ 環境變數配置

### 必要配置 (.env)

創建 `.env` 文件並配置以下變數：

```bash
# ======================
# 資料庫配置
# ======================
POSTGRES_DB=adhd_productivity
POSTGRES_USER=adhd_user
POSTGRES_PASSWORD=YourSecurePassword123!
POSTGRES_HOST=adhd-postgres
POSTGRES_PORT=5432

# ======================
# Redis 配置
# ======================
REDIS_HOST=adhd-redis
REDIS_PORT=6379
REDIS_PASSWORD=YourRedisPassword123!

# ======================
# JWT 安全配置
# ======================
JWT_SECRET_KEY=YourVeryLongAndSecureJWTSecretKeyThatIsAtLeast32CharactersLong
JWT_ISSUER=ADHDProductivitySystem
JWT_AUDIENCE=ADHDUsers
JWT_EXPIRY_MINUTES=60

# ======================
# 應用程式配置
# ======================
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
NODE_ENV=production

# ======================
# 前端配置
# ======================
VITE_API_BASE_URL=http://localhost/api
VITE_SIGNALR_HUB_URL=http://localhost/hubs

# ======================
# 管理員介面 (可選)
# ======================
PGADMIN_DEFAULT_EMAIL=admin@adhd.local
PGADMIN_DEFAULT_PASSWORD=YourAdminPassword123!

# ======================
# 安全設定
# ======================
ALLOWED_ORIGINS=http://localhost,http://localhost:3000,https://yourdomain.com
CORS_ALLOW_CREDENTIALS=true

# ======================
# 日誌配置
# ======================
LOG_LEVEL=Information
LOG_RETENTION_DAYS=30

# ======================
# 效能設定
# ======================
MAX_REQUEST_SIZE=10MB
RATE_LIMIT_REQUESTS_PER_MINUTE=100
```

### 開發環境配置

開發環境使用不同的設定：

```bash
# 開發環境 (.env.development)
ASPNETCORE_ENVIRONMENT=Development
NODE_ENV=development
LOG_LEVEL=Debug
VITE_API_BASE_URL=http://localhost:5000/api
ALLOWED_ORIGINS=http://localhost:3000,http://localhost:5173
```

### 生產環境配置

```bash
# 生產環境 (.env.production)
ASPNETCORE_ENVIRONMENT=Production
NODE_ENV=production
LOG_LEVEL=Warning
VITE_API_BASE_URL=https://yourdomain.com/api
ALLOWED_ORIGINS=https://yourdomain.com
```

## 📦 詳細安裝步驟

### 步驟 1: 準備環境

```bash
# 確認 Docker 版本
docker --version
docker-compose --version

# 確認系統資源
free -h  # Linux
vm_stat  # macOS
```

### 步驟 2: 獲取專案代碼

```bash
# 使用 HTTPS
git clone https://github.com/your-org/adhd-productivity.git

# 或使用 SSH (如果已設定)
git clone git@github.com:your-org/adhd-productivity.git

cd adhd-productivity
```

### 步驟 3: 環境配置

```bash
# 複製環境變數模板
cp .env.example .env

# 使用您偏好的編輯器編輯
vim .env
# 或
nano .env
# 或
code .env
```

### 步驟 4: 生成安全密鑰

```bash
# 生成 JWT 密鑰 (Linux/macOS)
openssl rand -base64 64

# 生成 JWT 密鑰 (Windows PowerShell)
[System.Web.Security.Membership]::GeneratePassword(64, 10)

# 生成資料庫密碼
openssl rand -base64 32
```

### 步驟 5: 啟動服務

```bash
# 拉取最新映像
docker-compose pull

# 建構並啟動服務
docker-compose up -d --build

# 查看啟動進度
docker-compose logs -f
```

### 步驟 6: 驗證安裝

```bash
# 檢查服務狀態
docker-compose ps

# 測試健康端點
curl http://localhost/health

# 測試前端
curl http://localhost

# 測試 API
curl http://localhost/api/health
```

## 🌐 生產環境部署

### 域名和 SSL 配置

1. **準備域名**
```bash
# 更新 .env 文件
VITE_API_BASE_URL=https://api.yourdomain.com
ALLOWED_ORIGINS=https://yourdomain.com
```

2. **SSL 憑證配置**
```nginx
# nginx/conf.d/ssl.conf
server {
    listen 443 ssl http2;
    server_name yourdomain.com;
    
    ssl_certificate /etc/ssl/certs/yourdomain.crt;
    ssl_certificate_key /etc/ssl/private/yourdomain.key;
    
    # SSL 安全配置
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512;
    ssl_prefer_server_ciphers off;
}
```

### 環境優化

```bash
# 生產環境 docker-compose.override.yml
version: '3.8'

services:
  adhd-backend:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '0.5'
        reservations:
          memory: 512M
          cpus: '0.25'

  adhd-postgres:
    environment:
      - POSTGRES_INITDB_ARGS=--encoding=UTF-8 --lc-collate=C --lc-ctype=C
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./database/backup:/backup
    deploy:
      resources:
        limits:
          memory: 2G
          cpus: '1'
```

### 自動部署腳本

```bash
#!/bin/bash
# deploy-production.sh

set -e

echo "開始生產環境部署..."

# 拉取最新代碼
git pull origin main

# 檢查環境變數
if [ ! -f .env ]; then
    echo "錯誤：找不到 .env 文件"
    exit 1
fi

# 備份資料庫
docker-compose exec adhd-postgres pg_dump -U $POSTGRES_USER $POSTGRES_DB > backup_$(date +%Y%m%d_%H%M%S).sql

# 更新服務
docker-compose pull
docker-compose up -d --build --remove-orphans

# 健康檢查
sleep 30
if ! curl -f http://localhost/health; then
    echo "健康檢查失敗，回滾..."
    docker-compose rollback
    exit 1
fi

echo "部署完成！"
```

## 📊 監控與維護

### 日誌管理

```bash
# 查看所有服務日誌
docker-compose logs

# 查看特定服務日誌
docker-compose logs adhd-backend
docker-compose logs adhd-postgres

# 即時查看日誌
docker-compose logs -f --tail=100

# 查看錯誤日誌
docker-compose logs | grep -i error
```

### 效能監控

```bash
# 查看容器資源使用
docker stats

# 查看磁碟使用
docker system df

# 查看網路狀況
docker network ls
```

### 資料庫維護

```bash
# 進入資料庫
docker-compose exec adhd-postgres psql -U adhd_user -d adhd_productivity

# 備份資料庫
docker-compose exec adhd-postgres pg_dump -U adhd_user adhd_productivity > backup.sql

# 還原資料庫
cat backup.sql | docker-compose exec -T adhd-postgres psql -U adhd_user -d adhd_productivity

# 查看資料庫大小
docker-compose exec adhd-postgres psql -U adhd_user -d adhd_productivity -c "SELECT pg_size_pretty(pg_database_size('adhd_productivity'));"
```

### 定期維護腳本

```bash
#!/bin/bash
# maintenance.sh - 每日維護腳本

# 清理舊日誌
find ./logs -name "*.log" -mtime +30 -delete

# 清理 Docker 資源
docker system prune -f

# 備份資料庫
./scripts/backup-database.sh

# 更新統計資訊
docker-compose exec adhd-postgres psql -U adhd_user -d adhd_productivity -c "ANALYZE;"
```

## 🔧 故障排除

### 常見問題及解決方案

#### 1. 容器啟動失敗

**問題**: 容器無法啟動
```bash
# 檢查日誌
docker-compose logs

# 檢查容器狀態
docker-compose ps

# 重新建構
docker-compose build --no-cache
docker-compose up -d
```

#### 2. 資料庫連接問題

**問題**: 無法連接到資料庫
```bash
# 檢查資料庫容器
docker-compose logs adhd-postgres

# 檢查網路連接
docker-compose exec adhd-backend ping adhd-postgres

# 測試資料庫連接
docker-compose exec adhd-postgres psql -U adhd_user -d adhd_productivity -c "SELECT 1;"
```

#### 3. 端口衝突

**問題**: 端口已被占用
```bash
# 查看端口使用 (Linux/macOS)
lsof -i :80
lsof -i :5000

# 查看端口使用 (Windows)
netstat -ano | findstr :80

# 修改端口映射
# 在 docker-compose.yml 中修改 ports 配置
```

#### 4. 記憶體不足

**問題**: 記憶體不足導致服務異常
```bash
# 增加 Docker 記憶體限制
# Docker Desktop > Settings > Resources > Memory

# 優化服務配置
# 在 docker-compose.yml 中添加記憶體限制
deploy:
  resources:
    limits:
      memory: 1G
```

#### 5. SSL 憑證問題

**問題**: HTTPS 配置錯誤
```bash
# 檢查憑證文件
ls -la /etc/ssl/certs/
ls -la /etc/ssl/private/

# 測試憑證
openssl x509 -in /etc/ssl/certs/yourdomain.crt -text -noout

# 重新載入 Nginx 配置
docker-compose exec adhd-nginx nginx -s reload
```

### 診斷工具

```bash
#!/bin/bash
# diagnostic.sh - 系統診斷腳本

echo "=== ADHD 生產力系統診斷 ==="

echo "1. Docker 狀態"
docker --version
docker-compose --version

echo "2. 服務狀態"
docker-compose ps

echo "3. 資源使用"
docker stats --no-stream

echo "4. 網路狀態"
docker network ls

echo "5. 磁碟使用"
docker system df

echo "6. 健康檢查"
curl -f http://localhost/health || echo "健康檢查失敗"

echo "診斷完成"
```

## 🔒 安全配置

### 基本安全措施

1. **更改預設密碼**
```bash
# 在 .env 中設定強密碼
POSTGRES_PASSWORD=$(openssl rand -base64 32)
REDIS_PASSWORD=$(openssl rand -base64 32)
JWT_SECRET_KEY=$(openssl rand -base64 64)
```

2. **網路安全**
```bash
# 限制對外暴露端口
# 僅暴露 80 和 443 端口給外網
# 其他服務端口僅在內部網路中可訪問
```

3. **檔案權限**
```bash
# 設定適當的文件權限
chmod 600 .env
chmod 600 ssl/private/*
chown root:root ssl/private/*
```

### 進階安全配置

```nginx
# nginx/conf.d/security.conf
# 安全標頭
add_header X-Frame-Options DENY;
add_header X-Content-Type-Options nosniff;
add_header X-XSS-Protection "1; mode=block";
add_header Strict-Transport-Security "max-age=31536000; includeSubDomains";
add_header Content-Security-Policy "default-src 'self'";

# 隱藏版本資訊
server_tokens off;

# 限制請求大小
client_max_body_size 10m;
```

### 防火牆配置

```bash
# UFW (Ubuntu)
sudo ufw allow 80
sudo ufw allow 443
sudo ufw enable

# iptables
iptables -A INPUT -p tcp --dport 80 -j ACCEPT
iptables -A INPUT -p tcp --dport 443 -j ACCEPT
iptables -A INPUT -p tcp --dport 22 -j ACCEPT
iptables -A INPUT -j DROP
```

### 備份策略

```bash
#!/bin/bash
# backup-strategy.sh

# 每日備份
BACKUP_DIR="/backup/daily"
DATE=$(date +%Y%m%d)

# 備份資料庫
docker-compose exec adhd-postgres pg_dump -U adhd_user adhd_productivity | gzip > "$BACKUP_DIR/db_$DATE.sql.gz"

# 備份配置文件
tar -czf "$BACKUP_DIR/config_$DATE.tar.gz" .env docker-compose.yml nginx/

# 保留 30 天的備份
find $BACKUP_DIR -mtime +30 -delete

# 每週備份到遠端
if [ $(date +%u) -eq 7 ]; then
    rsync -av $BACKUP_DIR/ user@backup-server:/backups/adhd-productivity/
fi
```

## 📞 支援與協助

### 取得協助

1. **查看文檔**: 本指南涵蓋了大部分常見情況
2. **檢查日誌**: 使用 `docker-compose logs` 查看詳細錯誤資訊
3. **社群支援**: GitHub Issues 或討論區
4. **專業支援**: 聯繫開發團隊

### 報告問題

提交問題報告時，請包含：
- 作業系統和版本
- Docker 和 Docker Compose 版本
- 錯誤訊息和日誌
- 重現步驟
- 環境配置 (移除敏感資訊)

---

**版本**: 1.0.0  
**最後更新**: 2024年12月22日  
**維護者**: ADHD 生產力系統開發團隊