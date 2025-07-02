# ADHD 生產力系統 - 一鍵部署指南

## 概述

這是一個完全容器化的 ADHD 生產力管理系統，所有服務都在 Docker 容器中運行，不依賴任何外部資源。使用者只需要 clone 專案並執行一鍵安裝腳本，即可建立完整的系統。

## 系統架構

```
┌─────────────────────────────────────────────────┐
│                  Nginx (Port 80)               │
│              反向代理和負載均衡                    │
└─────────────────┬───────────────────────────────┘
                  │
    ┌─────────────┼─────────────┐
    │             │             │
┌───▼────┐   ┌───▼────┐   ┌───▼────┐
│Frontend│   │Backend │   │Database│
│React   │   │.NET    │   │Postgres│
│Port    │   │Port    │   │Internal│
│3000    │   │5000    │   │5432    │
└────────┘   └────┬───┘   └────────┘
                  │
             ┌────▼────┐
             │  Redis  │
             │ Cache   │
             │Internal │
             │  6379   │
             └─────────┘
```

## 系統需求

### 最低需求
- **作業系統**: Windows 10/11, macOS 10.14+, Ubuntu 18.04+
- **記憶體**: 最少 4GB RAM (建議 8GB+)
- **硬碟空間**: 最少 10GB 可用空間
- **Docker**: Docker Desktop 或 Docker Engine
- **Docker Compose**: v2.0+
- **Git**: 用於下載專案

### 軟體依賴
- Docker Desktop (Windows/macOS) 或 Docker Engine (Linux)
- Docker Compose
- Git
- curl 或 wget (用於健康檢查)

## 快速安裝

### 方法 1: 使用一鍵安裝腳本 (推薦)

#### Linux/macOS:
```bash
curl -fsSL https://raw.githubusercontent.com/your-repo/ADHD/main/install.sh | bash
```

或者手動下載執行:
```bash
wget https://raw.githubusercontent.com/your-repo/ADHD/main/install.sh
chmod +x install.sh
./install.sh
```

#### Windows:
```cmd
curl -fsSL https://raw.githubusercontent.com/your-repo/ADHD/main/install.bat -o install.bat
install.bat
```

### 方法 2: 手動安裝

1. **Clone 專案**
   ```bash
   git clone https://github.com/your-username/ADHD.git
   cd ADHD
   ```

2. **建構和啟動**
   ```bash
   docker-compose -f docker-compose.self-contained.yml up -d --build
   ```

3. **等待服務啟動**
   ```bash
   # 檢查服務狀態
   docker-compose -f docker-compose.self-contained.yml ps
   
   # 查看啟動日誌
   docker-compose -f docker-compose.self-contained.yml logs -f
   ```

## 服務端點

安裝完成後，您可以通過以下端點訪問系統：

- **主要應用**: http://localhost
- **API 文檔**: http://localhost/api/swagger
- **API 端點**: http://localhost/api
- **資料庫管理** (可選): http://localhost:5050

## 管理指令

### 基本操作

```bash
# 進入專案目錄
cd ~/adhd-productivity-system  # Linux/macOS
cd %USERPROFILE%\adhd-productivity-system  # Windows

# 查看服務狀態
docker-compose -f docker-compose.self-contained.yml ps

# 啟動服務
docker-compose -f docker-compose.self-contained.yml up -d

# 停止服務
docker-compose -f docker-compose.self-contained.yml down

# 重啟服務
docker-compose -f docker-compose.self-contained.yml restart

# 查看日誌
docker-compose -f docker-compose.self-contained.yml logs -f

# 查看特定服務日誌
docker-compose -f docker-compose.self-contained.yml logs -f adhd-backend
```

### 資料庫管理

```bash
# 啟動 pgAdmin (資料庫管理界面)
docker-compose -f docker-compose.self-contained.yml --profile admin up -d

# 連接到資料庫
docker exec -it adhd-postgres psql -U adhd_user -d adhd_productivity

# 備份資料庫
docker exec adhd-postgres pg_dump -U adhd_user adhd_productivity > backup.sql

# 還原資料庫
docker exec -i adhd-postgres psql -U adhd_user adhd_productivity < backup.sql
```

### 清理和重建

```bash
# 使用清理腳本 (Linux/macOS)
./scripts/cleanup.sh

# 使用清理腳本 (Windows)
scripts\cleanup.bat

# 使用重建腳本 (Linux/macOS)
./scripts/rebuild.sh

# 手動清理所有資源
docker-compose -f docker-compose.self-contained.yml down --volumes --rmi all
docker system prune -af
```

## 故障排除

### 常見問題

#### 1. 端口衝突
**錯誤**: `port is already allocated`

**解決方案**:
```bash
# 檢查端口佔用 (Linux/macOS)
lsof -i :80
lsof -i :5000

# 檢查端口佔用 (Windows)
netstat -ano | findstr :80
netstat -ano | findstr :5000

# 停止佔用端口的服務或修改 docker-compose 文件中的端口映射
```

#### 2. 記憶體不足
**錯誤**: `Cannot start service: not enough memory`

**解決方案**:
- 增加 Docker Desktop 的記憶體限制
- 關閉其他不必要的應用程式
- 使用 `docker system prune` 清理未使用的資源

#### 3. 映像建構失敗
**錯誤**: `failed to build`

**解決方案**:
```bash
# 清理 Docker 快取
docker builder prune -af

# 重新建構
docker-compose -f docker-compose.self-contained.yml build --no-cache
```

#### 4. 服務無法連接
**錯誤**: `connection refused`

**解決方案**:
```bash
# 檢查容器是否運行
docker ps

# 檢查網路連接
docker network ls
docker network inspect adhd-productivity-network

# 重啟服務
docker-compose -f docker-compose.self-contained.yml restart
```

#### 5. 資料庫初始化失敗
**錯誤**: `database initialization failed`

**解決方案**:
```bash
# 清理資料庫 volume
docker volume rm adhd_postgres_data

# 重新啟動
docker-compose -f docker-compose.self-contained.yml up -d
```

### 日誌查看

```bash
# 查看所有服務日誌
docker-compose -f docker-compose.self-contained.yml logs

# 查看特定服務日誌
docker-compose -f docker-compose.self-contained.yml logs adhd-postgres
docker-compose -f docker-compose.self-contained.yml logs adhd-backend
docker-compose -f docker-compose.self-contained.yml logs adhd-frontend

# 實時查看日誌
docker-compose -f docker-compose.self-contained.yml logs -f --tail=100
```

### 效能監控

```bash
# 查看容器資源使用情況
docker stats

# 查看系統資源
docker system df

# 查看網路流量
docker exec adhd-nginx cat /var/log/nginx/access.log
```

## 資料備份和還原

### 備份

```bash
# 建立備份目錄
mkdir -p backups/$(date +%Y%m%d)

# 備份資料庫
docker exec adhd-postgres pg_dump -U adhd_user adhd_productivity | gzip > backups/$(date +%Y%m%d)/database.sql.gz

# 備份 Redis 資料
docker exec adhd-redis redis-cli --rdb - | gzip > backups/$(date +%Y%m%d)/redis.rdb.gz

# 備份 Docker volumes
docker run --rm -v adhd_postgres_data:/data -v $(pwd)/backups/$(date +%Y%m%d):/backup alpine tar czf /backup/postgres_volume.tar.gz -C /data .
docker run --rm -v adhd_redis_data:/data -v $(pwd)/backups/$(date +%Y%m%d):/backup alpine tar czf /backup/redis_volume.tar.gz -C /data .
```

### 還原

```bash
# 停止服務
docker-compose -f docker-compose.self-contained.yml down

# 還原資料庫
gunzip -c backups/20240101/database.sql.gz | docker exec -i adhd-postgres psql -U adhd_user adhd_productivity

# 還原 volumes
docker run --rm -v adhd_postgres_data:/data -v $(pwd)/backups/20240101:/backup alpine tar xzf /backup/postgres_volume.tar.gz -C /data
docker run --rm -v adhd_redis_data:/data -v $(pwd)/backups/20240101:/backup alpine tar xzf /backup/redis_volume.tar.gz -C /data

# 重新啟動服務
docker-compose -f docker-compose.self-contained.yml up -d
```

## 安全注意事項

### 生產環境建議

1. **更改預設密碼**
   ```bash
   # 編輯 .env 文件，更改以下變數:
   POSTGRES_PASSWORD=your_secure_password
   REDIS_PASSWORD=your_redis_password
   JWT_SECRET=your_jwt_secret
   PGADMIN_PASSWORD=your_admin_password
   ```

2. **限制網路存取**
   - 使用防火牆限制對 80/443 端口的存取
   - 移除 pgAdmin 服務（生產環境）
   - 考慮使用 VPN 或跳板機

3. **啟用 HTTPS**
   - 配置 SSL 證書
   - 修改 nginx 配置以支援 HTTPS

4. **定期更新**
   ```bash
   # 更新基礎映像
   docker-compose -f docker-compose.self-contained.yml pull
   docker-compose -f docker-compose.self-contained.yml up -d
   ```

## 開發環境

### 開發模式啟動

```bash
# 使用開發配置 (如果存在)
docker-compose -f docker-compose.yml up -d

# 啟動包含 pgAdmin 的完整環境
docker-compose -f docker-compose.self-contained.yml --profile admin up -d
```

### 代碼熱重載

開發環境支援代碼熱重載：
- 前端: Vite 提供熱重載
- 後端: .NET 支援熱重載 (如果配置了開發模式)

## 聯絡資訊

如果您遇到問題或需要協助：

1. 查看 [GitHub Issues](https://github.com/your-username/ADHD/issues)
2. 提交新的 Issue
3. 查看項目文檔

---

**注意**: 請確保在生產環境中更改所有預設密碼和安全配置。