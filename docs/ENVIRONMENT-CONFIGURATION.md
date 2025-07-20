# ADHD 生產力系統 - 環境變數和配置選項

## 📋 目錄
1. [概述](#概述)
2. [環境變數清單](#環境變數清單)
3. [配置檔案](#配置檔案)
4. [環境特定配置](#環境特定配置)
5. [安全配置](#安全配置)
6. [效能調優](#效能調優)
7. [監控配置](#監控配置)
8. [故障排除](#故障排除)

## 🎯 概述

本文檔詳細說明 ADHD 生產力系統的所有環境變數和配置選項。系統使用環境變數來管理不同環境（開發、測試、生產）的配置，確保系統的靈活性和安全性。

### 配置層次結構

```
配置優先級 (高到低):
1. 命令列參數
2. 環境變數
3. .env 檔案
4. 預設值
```

## 📝 環境變數清單

### 資料庫配置

| 變數名稱 | 預設值 | 描述 | 必填 | 範例 |
|----------|--------|------|------|------|
| `POSTGRES_DB` | `adhd_productivity` | PostgreSQL 資料庫名稱 | ✅ | `adhd_productivity` |
| `POSTGRES_USER` | `adhd_user` | PostgreSQL 使用者名稱 | ✅ | `adhd_user` |
| `POSTGRES_PASSWORD` | - | PostgreSQL 密碼 | ✅ | `SecurePassword123!` |
| `POSTGRES_HOST` | `localhost` | PostgreSQL 主機地址 | ✅ | `adhd-postgres` |
| `POSTGRES_PORT` | `5432` | PostgreSQL 端口 | ❌ | `5432` |
| `POSTGRES_INITDB_ARGS` | `--encoding=UTF-8` | 資料庫初始化參數 | ❌ | `--encoding=UTF-8 --lc-collate=C` |

### Redis 配置

| 變數名稱 | 預設值 | 描述 | 必填 | 範例 |
|----------|--------|------|------|------|
| `REDIS_HOST` | `localhost` | Redis 主機地址 | ✅ | `adhd-redis` |
| `REDIS_PORT` | `6379` | Redis 端口 | ❌ | `6379` |
| `REDIS_PASSWORD` | - | Redis 密碼 | ❌ | `RedisPassword123!` |
| `REDIS_DB` | `0` | Redis 資料庫索引 | ❌ | `0` |
| `REDIS_TIMEOUT` | `5000` | 連接超時時間 (毫秒) | ❌ | `5000` |

### JWT 身份驗證配置

| 變數名稱 | 預設值 | 描述 | 必填 | 範例 |
|----------|--------|------|------|------|
| `JWT_SECRET_KEY` | - | JWT 簽名密鑰 (至少32字符) | ✅ | `VeryLongAndSecureSecretKey123!` |
| `JWT_ISSUER` | `ADHDProductivitySystem` | JWT 發行者 | ❌ | `ADHDProductivitySystem` |
| `JWT_AUDIENCE` | `ADHDUsers` | JWT 接收者 | ❌ | `ADHDUsers` |
| `JWT_EXPIRY_MINUTES` | `60` | 訪問令牌過期時間 (分鐘) | ❌ | `60` |
| `JWT_REFRESH_EXPIRY_DAYS` | `30` | 刷新令牌過期時間 (天) | ❌ | `30` |

### ASP.NET Core 配置

| 變數名稱 | 預設值 | 描述 | 必填 | 範例 |
|----------|--------|------|------|------|
| `ASPNETCORE_ENVIRONMENT` | `Development` | 執行環境 | ✅ | `Production` |
| `ASPNETCORE_URLS` | `http://+:5000` | 監聽 URL | ✅ | `http://+:5000` |
| `ALLOWED_ORIGINS` | `http://localhost:3000` | CORS 允許的來源 | ✅ | `https://yourdomain.com` |
| `CORS_ALLOW_CREDENTIALS` | `true` | 是否允許憑證 | ❌ | `true` |

### 前端配置

| 變數名稱 | 預設值 | 描述 | 必填 | 範例 |
|----------|--------|------|------|------|
| `NODE_ENV` | `development` | Node.js 執行環境 | ✅ | `production` |
| `VITE_API_BASE_URL` | `http://localhost:5000/api` | API 基礎 URL | ✅ | `https://api.yourdomain.com` |
| `VITE_SIGNALR_HUB_URL` | `http://localhost:5000/hubs` | SignalR Hub URL | ✅ | `https://api.yourdomain.com/hubs` |
| `VITE_APP_NAME` | `ADHD Productivity System` | 應用程式名稱 | ❌ | `My ADHD App` |

### 日誌配置

| 變數名稱 | 預設值 | 描述 | 必填 | 範例 |
|----------|--------|------|------|------|
| `LOG_LEVEL` | `Information` | 日誌級別 | ❌ | `Warning` |
| `LOG_RETENTION_DAYS` | `30` | 日誌保留天數 | ❌ | `7` |
| `LOG_PATH` | `logs/` | 日誌檔案路徑 | ❌ | `/app/logs/` |
| `LOG_FORMAT` | `json` | 日誌格式 | ❌ | `text` |

### 效能配置

| 變數名稱 | 預設值 | 描述 | 必填 | 範例 |
|----------|--------|------|------|------|
| `MAX_REQUEST_SIZE` | `10MB` | 最大請求大小 | ❌ | `50MB` |
| `RATE_LIMIT_REQUESTS_PER_MINUTE` | `100` | 每分鐘請求限制 | ❌ | `200` |
| `CONNECTION_POOL_SIZE` | `100` | 資料庫連接池大小 | ❌ | `50` |
| `COMMAND_TIMEOUT` | `30` | 命令超時時間 (秒) | ❌ | `60` |

### 安全配置

| 變數名稱 | 預設值 | 描述 | 必填 | 範例 |
|----------|--------|------|------|------|
| `REQUIRE_HTTPS` | `false` | 是否要求 HTTPS | ❌ | `true` |
| `HSTS_MAX_AGE` | `31536000` | HSTS 最大年齡 (秒) | ❌ | `31536000` |
| `SECURITY_HEADERS_ENABLED` | `true` | 是否啟用安全標頭 | ❌ | `true` |
| `XSS_PROTECTION` | `true` | 是否啟用 XSS 保護 | ❌ | `true` |

### 管理員配置

| 變數名稱 | 預設值 | 描述 | 必填 | 範例 |
|----------|--------|------|------|------|
| `PGADMIN_DEFAULT_EMAIL` | `admin@adhd.local` | pgAdmin 預設電子郵件 | ❌ | `admin@yourdomain.com` |
| `PGADMIN_DEFAULT_PASSWORD` | - | pgAdmin 預設密碼 | ❌ | `AdminPassword123!` |
| `PGADMIN_CONFIG_SERVER_MODE` | `False` | pgAdmin 伺服器模式 | ❌ | `False` |

## 📁 配置檔案

### appsettings.json (後端)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=${POSTGRES_HOST};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD};Port=${POSTGRES_PORT}",
    "RedisConnection": "${REDIS_HOST}:${REDIS_PORT}"
  },
  "JWT": {
    "SecretKey": "${JWT_SECRET_KEY}",
    "Issuer": "${JWT_ISSUER}",
    "Audience": "${JWT_AUDIENCE}",
    "TokenExpirationMinutes": "${JWT_EXPIRY_MINUTES}"
  },
  "AllowedOrigins": [
    "${ALLOWED_ORIGINS}"
  ],
  "Serilog": {
    "MinimumLevel": {
      "Default": "${LOG_LEVEL}",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "${LOG_PATH}adhd-productivity-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": "${LOG_RETENTION_DAYS}"
        }
      }
    ]
  }
}
```

### docker-compose.yml 環境變數

```yaml
version: '3.8'

services:
  adhd-postgres:
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_INITDB_ARGS: "${POSTGRES_INITDB_ARGS}"
      PGDATA: /var/lib/postgresql/data/pgdata

  adhd-redis:
    command: >
      redis-server 
      --appendonly yes 
      --appendfsync everysec
      --maxmemory 256mb
      --maxmemory-policy allkeys-lru
      ${REDIS_PASSWORD:+--requirepass} ${REDIS_PASSWORD}

  adhd-backend:
    environment:
      - ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
      - ASPNETCORE_URLS=${ASPNETCORE_URLS}
      - POSTGRES_HOST=adhd-postgres
      - POSTGRES_PORT=5432
      - POSTGRES_DB=${POSTGRES_DB}
      - POSTGRES_USER=${POSTGRES_USER}
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
      - REDIS_HOST=adhd-redis
      - REDIS_PORT=6379
      - REDIS_PASSWORD=${REDIS_PASSWORD}
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - JWT_ISSUER=${JWT_ISSUER}
      - JWT_AUDIENCE=${JWT_AUDIENCE}
      - JWT_EXPIRY_MINUTES=${JWT_EXPIRY_MINUTES}
      - LOG_LEVEL=${LOG_LEVEL}

  adhd-frontend:
    environment:
      - NODE_ENV=${NODE_ENV}
      - VITE_API_BASE_URL=${VITE_API_BASE_URL}
      - VITE_SIGNALR_HUB_URL=${VITE_SIGNALR_HUB_URL}
```

## 🌍 環境特定配置

### 開發環境 (.env.development)

```bash
# 開發環境配置
NODE_ENV=development
ASPNETCORE_ENVIRONMENT=Development
LOG_LEVEL=Debug

# 本地服務 URL
VITE_API_BASE_URL=http://localhost:5000/api
VITE_SIGNALR_HUB_URL=http://localhost:5000/hubs

# 寬鬆的 CORS 設定
ALLOWED_ORIGINS=http://localhost:3000,http://localhost:5173,http://localhost:8080

# 開發用資料庫
POSTGRES_DB=adhd_productivity_dev
POSTGRES_USER=dev_user
POSTGRES_PASSWORD=dev_password

# 開發用 JWT (較短過期時間)
JWT_EXPIRY_MINUTES=1440  # 24 小時

# 除錯設定
DEBUG=true
VERBOSE_LOGGING=true
```

### 測試環境 (.env.testing)

```bash
# 測試環境配置
NODE_ENV=test
ASPNETCORE_ENVIRONMENT=Testing
LOG_LEVEL=Information

# 測試資料庫
POSTGRES_DB=adhd_productivity_test
POSTGRES_USER=test_user
POSTGRES_PASSWORD=test_password

# 測試用 JWT
JWT_SECRET_KEY=test_secret_key_for_testing_only
JWT_EXPIRY_MINUTES=15  # 15 分鐘

# 測試特定設定
RESET_DATABASE_ON_STARTUP=true
SEED_TEST_DATA=true
```

### 生產環境 (.env.production)

```bash
# 生產環境配置
NODE_ENV=production
ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Warning

# 生產服務 URL
VITE_API_BASE_URL=https://api.yourdomain.com
VITE_SIGNALR_HUB_URL=https://api.yourdomain.com/hubs

# 嚴格的 CORS 設定
ALLOWED_ORIGINS=https://yourdomain.com

# 生產資料庫 (強密碼)
POSTGRES_DB=adhd_productivity
POSTGRES_USER=adhd_prod_user
POSTGRES_PASSWORD=VerySecureProductionPassword123!

# 生產用 JWT (強密鑰)
JWT_SECRET_KEY=VeryLongAndSecureProductionJWTSecretKey123!
JWT_EXPIRY_MINUTES=60  # 1 小時

# 安全設定
REQUIRE_HTTPS=true
HSTS_MAX_AGE=31536000
SECURITY_HEADERS_ENABLED=true

# 效能設定
CONNECTION_POOL_SIZE=50
RATE_LIMIT_REQUESTS_PER_MINUTE=200
```

## 🔒 安全配置

### 密碼生成指南

```bash
# 生成強密碼 (32 字符)
openssl rand -base64 32

# 生成 JWT 密鑰 (64 字符)
openssl rand -base64 64

# 使用 Python 生成密碼
python3 -c "import secrets; print(secrets.token_urlsafe(32))"

# 使用 Node.js 生成密碼
node -e "console.log(require('crypto').randomBytes(32).toString('base64'))"
```

### 環境變數驗證

```bash
#!/bin/bash
# validate-env.sh - 驗證環境變數

validate_env() {
    local var_name=$1
    local var_value=${!var_name}
    local required=$2
    local min_length=$3
    
    if [ "$required" = "true" ] && [ -z "$var_value" ]; then
        echo "錯誤：必要的環境變數 $var_name 未設定"
        return 1
    fi
    
    if [ -n "$min_length" ] && [ ${#var_value} -lt $min_length ]; then
        echo "錯誤：環境變數 $var_name 長度不足 (最少 $min_length 字符)"
        return 1
    fi
    
    return 0
}

# 驗證必要變數
validate_env "POSTGRES_PASSWORD" true 8
validate_env "JWT_SECRET_KEY" true 32
validate_env "REDIS_PASSWORD" false 8

echo "環境變數驗證完成"
```

### 環境變數加密

```bash
# 使用 gpg 加密環境變數檔案
gpg --symmetric --cipher-algo AES256 .env

# 解密
gpg --decrypt .env.gpg > .env

# 使用 ansible-vault
ansible-vault encrypt .env
ansible-vault decrypt .env
```

## ⚡ 效能調優

### 資料庫效能配置

```bash
# PostgreSQL 效能調優
POSTGRES_SHARED_BUFFERS=256MB
POSTGRES_EFFECTIVE_CACHE_SIZE=1GB
POSTGRES_WORK_MEM=16MB
POSTGRES_MAINTENANCE_WORK_MEM=256MB
POSTGRES_MAX_CONNECTIONS=100
POSTGRES_CHECKPOINT_COMPLETION_TARGET=0.7
```

### Redis 效能配置

```bash
# Redis 效能調優
REDIS_MAXMEMORY=512mb
REDIS_MAXMEMORY_POLICY=allkeys-lru
REDIS_SAVE_INTERVAL=900 1 300 10 60 10000
REDIS_TCP_KEEPALIVE=300
REDIS_TIMEOUT=0
```

### ASP.NET Core 效能配置

```bash
# ASP.NET Core 效能調優
DOTNET_GCConserveMemory=1
DOTNET_GCHeapCount=2
DOTNET_ThreadPool_UnfairSemaphoreSpinLimit=6
CONNECTION_POOL_MIN_SIZE=5
CONNECTION_POOL_MAX_SIZE=100
COMMAND_TIMEOUT=30
```

## 📊 監控配置

### 應用程式監控

```bash
# 監控相關環境變數
ENABLE_METRICS=true
METRICS_PORT=9090
HEALTH_CHECK_INTERVAL=30
HEALTH_CHECK_TIMEOUT=10
ENABLE_PERFORMANCE_COUNTERS=true
```

### 日誌監控

```bash
# 結構化日誌配置
LOG_FORMAT=json
LOG_INCLUDE_SCOPES=true
LOG_TIMESTAMP_FORMAT=ISO8601
LOG_CORRELATION_ID=true

# 日誌聚合
SERILOG_ELASTICSEARCH_URL=http://elasticsearch:9200
SERILOG_ELASTICSEARCH_INDEX=adhd-productivity-logs
```

### 警報配置

```bash
# 警報閾值
ALERT_CPU_THRESHOLD=80
ALERT_MEMORY_THRESHOLD=90
ALERT_DISK_THRESHOLD=85
ALERT_ERROR_RATE_THRESHOLD=5
ALERT_RESPONSE_TIME_THRESHOLD=2000

# 通知設定
ALERT_EMAIL=admin@yourdomain.com
ALERT_WEBHOOK_URL=https://hooks.slack.com/services/YOUR/SLACK/WEBHOOK
```

## 🔧 故障排除

### 環境變數除錯

```bash
# 檢查環境變數是否正確載入
docker-compose config

# 查看特定服務的環境變數
docker-compose exec adhd-backend env | grep POSTGRES

# 驗證環境變數
docker-compose exec adhd-backend bash -c 'echo $JWT_SECRET_KEY | wc -c'
```

### 常見配置錯誤

#### 1. JWT 密鑰太短
```bash
# 錯誤
JWT_SECRET_KEY=short

# 正確
JWT_SECRET_KEY=$(openssl rand -base64 64)
```

#### 2. 資料庫連接字串錯誤
```bash
# 錯誤
POSTGRES_HOST=localhost  # 在容器中應該使用服務名稱

# 正確
POSTGRES_HOST=adhd-postgres
```

#### 3. CORS 配置錯誤
```bash
# 錯誤
ALLOWED_ORIGINS=*  # 不安全

# 正確
ALLOWED_ORIGINS=https://yourdomain.com,https://www.yourdomain.com
```

### 配置驗證腳本

```bash
#!/bin/bash
# config-check.sh - 配置檢查腳本

echo "=== 配置檢查 ==="

# 檢查必要檔案
if [ ! -f .env ]; then
    echo "❌ .env 檔案不存在"
    exit 1
fi

# 檢查敏感資訊
if grep -q "your_secure_password" .env; then
    echo "❌ 發現預設密碼，請更改"
    exit 1
fi

# 檢查 JWT 密鑰長度
JWT_LENGTH=$(grep "JWT_SECRET_KEY" .env | cut -d'=' -f2 | wc -c)
if [ $JWT_LENGTH -lt 32 ]; then
    echo "❌ JWT 密鑰過短 (當前: $JWT_LENGTH, 最少: 32)"
    exit 1
fi

# 檢查資料庫配置
if ! grep -q "POSTGRES_PASSWORD" .env; then
    echo "❌ 缺少資料庫密碼配置"
    exit 1
fi

echo "✅ 配置檢查通過"
```

## 📚 範例配置

### 完整 .env 範例

```bash
# ==============================================
# ADHD 生產力系統 - 生產環境配置範例
# ==============================================

# 資料庫配置
POSTGRES_DB=adhd_productivity
POSTGRES_USER=adhd_user
POSTGRES_PASSWORD=SecureDBPassword123!@#
POSTGRES_HOST=adhd-postgres
POSTGRES_PORT=5432
POSTGRES_INITDB_ARGS=--encoding=UTF-8 --lc-collate=C --lc-ctype=C

# Redis 配置
REDIS_HOST=adhd-redis
REDIS_PORT=6379
REDIS_PASSWORD=SecureRedisPassword123!@#

# JWT 配置
JWT_SECRET_KEY=VeryLongAndSecureJWTSecretKeyForProductionUse123!@#$%^&*
JWT_ISSUER=ADHDProductivitySystem
JWT_AUDIENCE=ADHDUsers
JWT_EXPIRY_MINUTES=60

# 應用程式配置
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
NODE_ENV=production

# 前端配置
VITE_API_BASE_URL=https://api.yourdomain.com
VITE_SIGNALR_HUB_URL=https://api.yourdomain.com/hubs

# 安全配置
ALLOWED_ORIGINS=https://yourdomain.com,https://www.yourdomain.com
CORS_ALLOW_CREDENTIALS=true
REQUIRE_HTTPS=true
HSTS_MAX_AGE=31536000

# 日誌配置
LOG_LEVEL=Information
LOG_RETENTION_DAYS=30
LOG_PATH=/app/logs/

# 效能配置
MAX_REQUEST_SIZE=10MB
RATE_LIMIT_REQUESTS_PER_MINUTE=100
CONNECTION_POOL_SIZE=50
COMMAND_TIMEOUT=30

# 管理員配置 (可選)
PGADMIN_DEFAULT_EMAIL=admin@yourdomain.com
PGADMIN_DEFAULT_PASSWORD=SecureAdminPassword123!@#
```

---

**版本**: 1.0.0  
**最後更新**: 2024年12月22日  
**維護者**: ADHD 生產力系統開發團隊