# ADHD 生產力系統 - 故障排除指南

## 📋 目錄
1. [快速故障排除清單](#快速故障排除清單)
2. [系統無法啟動](#系統無法啟動)
3. [前端問題](#前端問題)
4. [後端 API 問題](#後端-api-問題)
5. [資料庫問題](#資料庫問題)
6. [效能問題](#效能問題)
7. [認證和授權問題](#認證和授權問題)
8. [網路和連接問題](#網路和連接問題)
9. [日誌和監控](#日誌和監控)
10. [常見錯誤代碼](#常見錯誤代碼)
11. [升級和遷移問題](#升級和遷移問題)
12. [開發環境問題](#開發環境問題)

## 🚨 快速故障排除清單

### 第一步：基本檢查
```bash
# 1. 檢查所有服務狀態
docker-compose ps

# 2. 檢查服務日誌
docker-compose logs

# 3. 檢查系統資源
docker stats

# 4. 驗證網路連接
curl http://localhost/health
```

### 第二步：服務健康檢查
```bash
# 檢查各服務健康狀況
curl http://localhost/health                    # 整體健康狀況
curl http://localhost:5000/health               # 後端 API
curl http://localhost                           # 前端應用
```

### 第三步：日誌分析
```bash
# 查看最近的錯誤日誌
docker-compose logs --tail=100 | grep -i error
docker-compose logs --tail=100 | grep -i exception
docker-compose logs --tail=100 | grep -i failed
```

## 🔧 系統無法啟動

### 問題：Docker Compose 啟動失敗

#### 症狀
- `docker-compose up` 命令失敗
- 容器無法啟動或立即退出
- 端口被占用錯誤

#### 診斷步驟
```bash
# 1. 檢查 Docker 運行狀態
docker info

# 2. 檢查端口占用
# Linux/macOS
netstat -tlnp | grep :80
lsof -i :80

# Windows
netstat -ano | findstr :80

# 3. 檢查 Docker Compose 檔案語法
docker-compose config

# 4. 檢查映像是否存在
docker images | grep adhd

# 5. 檢查磁碟空間
df -h
docker system df
```

#### 解決方案
```bash
# 解決方案 1：清理並重新啟動
docker-compose down -v
docker system prune -f
docker-compose up -d --build

# 解決方案 2：更改端口 (如果端口衝突)
# 編輯 .env 檔案
FRONTEND_PORT=3001
BACKEND_PORT=5001

# 解決方案 3：釋放磁碟空間
docker system prune -a -f
docker volume prune -f

# 解決方案 4：重置 Docker (最後手段)
# Windows/macOS: 重置 Docker Desktop
# Linux: 重啟 Docker 服務
sudo systemctl restart docker
```

### 問題：環境變數配置錯誤

#### 症狀
- 服務啟動但無法連接到資料庫
- JWT 認證失敗
- 配置相關錯誤訊息

#### 診斷步驟
```bash
# 1. 檢查 .env 檔案是否存在
ls -la .env

# 2. 驗證環境變數載入
docker-compose config

# 3. 檢查必要的環境變數
grep -E "(POSTGRES_|JWT_|REDIS_)" .env
```

#### 解決方案
```bash
# 1. 從範本建立 .env 檔案
cp .env.example .env

# 2. 生成安全的密鑰
openssl rand -base64 64  # JWT 密鑰
openssl rand -base64 32  # 資料庫密碼

# 3. 驗證配置
docker-compose config

# 4. 重新啟動服務
docker-compose up -d
```

## 🌐 前端問題

### 問題：前端應用無法載入

#### 症狀
- 瀏覽器顯示「無法連接」或空白頁面
- 載入時間過長
- JavaScript 錯誤

#### 診斷步驟
```bash
# 1. 檢查前端容器狀態
docker-compose logs adhd-frontend

# 2. 檢查 Nginx 設定
docker-compose logs adhd-nginx

# 3. 檢查網路連接
curl -I http://localhost

# 4. 瀏覽器開發者工具
# - 檢查 Console 錯誤
# - 檢查 Network 請求
# - 檢查 Application 儲存
```

#### 解決方案
```bash
# 解決方案 1：重建前端容器
docker-compose build adhd-frontend
docker-compose up -d adhd-frontend

# 解決方案 2：清除瀏覽器快取
# 在瀏覽器中：Ctrl+Shift+R (硬性重新載入)

# 解決方案 3：檢查 Nginx 設定
docker-compose exec adhd-nginx nginx -t
docker-compose restart adhd-nginx

# 解決方案 4：檢查前端建構
cd frontend/
npm run build
```

### 問題：API 請求失敗

#### 症狀
- 前端載入但無法獲取資料
- CORS 錯誤
- 401/403 認證錯誤

#### 診斷步驟
```bash
# 1. 檢查 API 端點
curl http://localhost/api/health

# 2. 檢查 CORS 設定
curl -H "Origin: http://localhost:3000" \
     -H "Access-Control-Request-Method: GET" \
     -H "Access-Control-Request-Headers: X-Requested-With" \
     -X OPTIONS \
     http://localhost/api/tasks

# 3. 檢查認證令牌
# 在瀏覽器開發者工具中檢查 localStorage
```

#### 解決方案
```bash
# 解決方案 1：更新 CORS 設定
# 在 .env 檔案中：
ALLOWED_ORIGINS=http://localhost,http://localhost:3000,http://localhost:5173

# 解決方案 2：檢查 API 設定
docker-compose logs adhd-backend | grep -i cors

# 解決方案 3：清除認證狀態
# 在瀏覽器 Console 中：
localStorage.clear()
```

## 🔌 後端 API 問題

### 問題：API 回應時間過慢

#### 症狀
- API 請求需要數秒才能回應
- 超時錯誤
- 資料庫連接池耗盡

#### 診斷步驟
```bash
# 1. 檢查 API 效能
time curl http://localhost/api/tasks

# 2. 檢查資料庫連接
docker-compose exec adhd-postgres psql -U adhd_user -d adhd_productivity -c "SELECT count(*) FROM pg_stat_activity;"

# 3. 檢查慢查詢
docker-compose exec adhd-postgres psql -U adhd_user -d adhd_productivity -c "SELECT query, mean_time, calls FROM pg_stat_statements ORDER BY mean_time DESC LIMIT 10;"

# 4. 檢查記憶體使用
docker stats --no-stream
```

#### 解決方案
```bash
# 解決方案 1：重啟服務
docker-compose restart adhd-backend

# 解決方案 2：增加資料庫連接池
# 在 appsettings.json 中：
"ConnectionStrings": {
    "DefaultConnection": "...;Pooling=true;MinPoolSize=5;MaxPoolSize=100;"
}

# 解決方案 3：啟用快取
# 檢查 Redis 是否正常運作
docker-compose exec adhd-redis redis-cli ping

# 解決方案 4：分析慢查詢
docker-compose exec adhd-postgres psql -U adhd_user -d adhd_productivity -c "EXPLAIN ANALYZE SELECT * FROM tasks WHERE user_id = 'some-uuid';"
```

### 問題：資料庫遷移失敗

#### 症狀
- 應用程式啟動時資料庫錯誤
- 缺少表格或欄位
- 外鍵約束錯誤

#### 診斷步驟
```bash
# 1. 檢查資料庫連接
docker-compose exec adhd-postgres psql -U adhd_user -d adhd_productivity -c "\dt"

# 2. 檢查遷移歷史
docker-compose exec adhd-postgres psql -U adhd_user -d adhd_productivity -c "SELECT * FROM __EFMigrationsHistory ORDER BY migration_id;"

# 3. 檢查資料庫日誌
docker-compose logs adhd-postgres | grep -i error
```

#### 解決方案
```bash
# 解決方案 1：手動執行遷移
docker-compose exec adhd-backend dotnet ef database update

# 解決方案 2：重建資料庫 (開發環境)
docker-compose down
docker volume rm adhd_postgres_data
docker-compose up -d

# 解決方案 3：回滾遷移
docker-compose exec adhd-backend dotnet ef database update PreviousMigration

# 解決方案 4：修復損壞的遷移
docker-compose exec adhd-postgres psql -U adhd_user -d adhd_productivity -c "DELETE FROM __EFMigrationsHistory WHERE migration_id = 'problem_migration';"
```

## 💾 資料庫問題

### 問題：PostgreSQL 連接失敗

#### 症狀
- 應用程式無法連接到資料庫
- "Connection refused" 錯誤
- 認證失敗

#### 診斷步驟
```bash
# 1. 檢查 PostgreSQL 容器狀態
docker-compose ps adhd-postgres

# 2. 檢查 PostgreSQL 日誌
docker-compose logs adhd-postgres

# 3. 測試連接
docker-compose exec adhd-postgres psql -U adhd_user -d adhd_productivity

# 4. 檢查網路
docker network inspect adhd-productivity-network
```

#### 解決方案
```bash
# 解決方案 1：重啟 PostgreSQL
docker-compose restart adhd-postgres

# 解決方案 2：檢查憑證
# 確認 .env 檔案中的資料庫憑證正確

# 解決方案 3：重建資料庫容器
docker-compose down
docker volume rm adhd_postgres_data
docker-compose up -d adhd-postgres

# 解決方案 4：手動建立使用者和資料庫
docker-compose exec adhd-postgres psql -U postgres -c "CREATE USER adhd_user WITH PASSWORD 'your_password';"
docker-compose exec adhd-postgres psql -U postgres -c "CREATE DATABASE adhd_productivity OWNER adhd_user;"
```

### 問題：Redis 連接問題

#### 症狀
- 快取功能不工作
- 會話遺失
- Redis 連接錯誤

#### 診斷步驟
```bash
# 1. 檢查 Redis 容器狀態
docker-compose ps adhd-redis

# 2. 測試 Redis 連接
docker-compose exec adhd-redis redis-cli ping

# 3. 檢查 Redis 日誌
docker-compose logs adhd-redis

# 4. 檢查記憶體使用
docker-compose exec adhd-redis redis-cli info memory
```

#### 解決方案
```bash
# 解決方案 1：重啟 Redis
docker-compose restart adhd-redis

# 解決方案 2：清除 Redis 資料
docker-compose exec adhd-redis redis-cli flushall

# 解決方案 3：檢查 Redis 設定
docker-compose exec adhd-redis redis-cli config get "*"

# 解決方案 4：增加 Redis 記憶體限制
# 在 docker-compose.yml 中：
command: redis-server --maxmemory 512mb --maxmemory-policy allkeys-lru
```

## ⚡ 效能問題

### 問題：系統回應緩慢

#### 症狀
- 頁面載入時間超過 3 秒
- API 請求超時
- 使用者介面卡頓

#### 診斷步驟
```bash
# 1. 檢查系統資源
docker stats

# 2. 檢查 API 效能
curl -w "@curl-format.txt" -o /dev/null -s http://localhost/api/tasks

# 3. 分析資料庫效能
docker-compose exec adhd-postgres psql -U adhd_user -d adhd_productivity -c "SELECT query, total_time, mean_time, calls FROM pg_stat_statements ORDER BY total_time DESC LIMIT 10;"

# 4. 檢查網路延遲
ping localhost
```

#### 解決方案
```bash
# 解決方案 1：啟用快取
# 檢查 Redis 快取是否正常工作
docker-compose exec adhd-redis redis-cli monitor

# 解決方案 2：最佳化資料庫查詢
# 添加索引
docker-compose exec adhd-postgres psql -U adhd_user -d adhd_productivity -c "CREATE INDEX CONCURRENTLY idx_tasks_user_status ON tasks(user_id, status) WHERE is_archived = false;"

# 解決方案 3：增加資源限制
# 在 docker-compose.yml 中為服務添加資源限制
deploy:
  resources:
    limits:
      memory: 1G
      cpus: '0.5'

# 解決方案 4：啟用壓縮
# 在 nginx.conf 中：
gzip on;
gzip_types text/plain application/json application/javascript text/css;
```

### 問題：記憶體使用過高

#### 症狀
- 容器記憶體使用超過 80%
- 系統變慢或當機
- Out of Memory 錯誤

#### 診斷步驟
```bash
# 1. 檢查記憶體使用
docker stats --no-stream

# 2. 檢查程序記憶體
docker-compose exec adhd-backend ps aux --sort=-%mem

# 3. 檢查 .NET 記憶體
docker-compose exec adhd-backend dotnet-counters monitor --process-id 1

# 4. 檢查 PostgreSQL 記憶體
docker-compose exec adhd-postgres psql -U adhd_user -d adhd_productivity -c "SELECT * FROM pg_stat_activity WHERE state = 'active';"
```

#### 解決方案
```bash
# 解決方案 1：調整 .NET 垃圾回收
# 在 Dockerfile 中添加環境變數：
ENV DOTNET_GCConserveMemory=1
ENV DOTNET_GCHeapCount=2

# 解決方案 2：調整 PostgreSQL 設定
# 在 postgresql.conf 中：
shared_buffers = 128MB
effective_cache_size = 512MB
work_mem = 4MB

# 解決方案 3：限制容器記憶體
docker-compose down
# 在 docker-compose.yml 中添加：
deploy:
  resources:
    limits:
      memory: 512M

# 解決方案 4：重啟服務
docker-compose restart
```

## 🔐 認證和授權問題

### 問題：JWT 令牌問題

#### 症狀
- 登入後立即被登出
- "Invalid token" 錯誤
- 認證狀態不一致

#### 診斷步驟
```bash
# 1. 檢查 JWT 設定
docker-compose exec adhd-backend printenv | grep JWT

# 2. 驗證令牌格式
# 在瀏覽器開發者工具中檢查 localStorage 的令牌

# 3. 檢查時間同步
date
docker-compose exec adhd-backend date

# 4. 檢查認證日誌
docker-compose logs adhd-backend | grep -i authentication
```

#### 解決方案
```bash
# 解決方案 1：重新生成 JWT 密鑰
openssl rand -base64 64
# 更新 .env 檔案並重啟服務

# 解決方案 2：同步系統時間
sudo ntpdate -s time.nist.gov

# 解決方案 3：調整令牌過期時間
# 在 .env 中：
JWT_EXPIRY_MINUTES=60

# 解決方案 4：清除用戶端認證資料
# 在瀏覽器 Console 中：
localStorage.removeItem('access_token')
localStorage.removeItem('refresh_token')
```

### 問題：CORS 跨域問題

#### 症狀
- 瀏覽器 Console 出現 CORS 錯誤
- API 請求被阻擋
- Preflight 請求失敗

#### 診斷步驟
```bash
# 1. 檢查 CORS 設定
docker-compose logs adhd-backend | grep -i cors

# 2. 測試 CORS
curl -H "Origin: http://localhost:3000" \
     -H "Access-Control-Request-Method: POST" \
     -H "Access-Control-Request-Headers: Content-Type" \
     -X OPTIONS \
     http://localhost/api/auth/login

# 3. 檢查允許的來源
docker-compose exec adhd-backend printenv | grep ALLOWED_ORIGINS
```

#### 解決方案
```bash
# 解決方案 1：更新 CORS 設定
# 在 .env 中：
ALLOWED_ORIGINS=http://localhost,http://localhost:3000,http://localhost:5173,https://yourdomain.com

# 解決方案 2：重啟後端服務
docker-compose restart adhd-backend

# 解決方案 3：檢查 Nginx 設定
# 確保 Nginx 不會覆蓋 CORS 標頭

# 解決方案 4：使用代理 (開發環境)
# 在 vite.config.ts 中：
export default defineConfig({
  server: {
    proxy: {
      '/api': 'http://localhost:5000'
    }
  }
})
```

## 🌐 網路和連接問題

### 問題：服務間無法通信

#### 症狀
- 後端無法連接到資料庫
- 前端無法連接到後端
- 服務發現失敗

#### 診斷步驟
```bash
# 1. 檢查 Docker 網路
docker network ls
docker network inspect adhd-productivity-network

# 2. 測試服務連接
docker-compose exec adhd-backend ping adhd-postgres
docker-compose exec adhd-backend nslookup adhd-postgres

# 3. 檢查端口綁定
docker-compose ps

# 4. 檢查防火牆設定
sudo iptables -L
sudo ufw status
```

#### 解決方案
```bash
# 解決方案 1：重建網路
docker-compose down
docker network prune
docker-compose up -d

# 解決方案 2：檢查服務名稱
# 確保連接字串使用容器名稱而非 localhost

# 解決方案 3：重啟 Docker
sudo systemctl restart docker

# 解決方案 4：檢查 DNS 解析
docker-compose exec adhd-backend cat /etc/resolv.conf
```

### 問題：外部 API 連接失敗

#### 症狀
- 無法連接到外部服務
- 網路超時
- DNS 解析失敗

#### 診斷步驟
```bash
# 1. 測試外部連接
docker-compose exec adhd-backend curl -I https://api.external-service.com

# 2. 檢查 DNS 解析
docker-compose exec adhd-backend nslookup api.external-service.com

# 3. 檢查代理設定
docker-compose exec adhd-backend printenv | grep -i proxy

# 4. 檢查 SSL 憑證
docker-compose exec adhd-backend openssl s_client -connect api.external-service.com:443
```

#### 解決方案
```bash
# 解決方案 1：配置代理 (如果需要)
# 在 docker-compose.yml 中：
environment:
  - HTTP_PROXY=http://proxy.company.com:8080
  - HTTPS_PROXY=http://proxy.company.com:8080

# 解決方案 2：更新 CA 憑證
docker-compose exec adhd-backend update-ca-certificates

# 解決方案 3：使用自訂 DNS
# 在 docker-compose.yml 中：
dns:
  - 8.8.8.8
  - 1.1.1.1

# 解決方案 4：檢查防火牆規則
sudo iptables -L OUTPUT
```

## 📊 日誌和監控

### 問題：日誌過多或過少

#### 症狀
- 磁碟空間被日誌占滿
- 重要錯誤被忽略
- 日誌格式不正確

#### 診斷步驟
```bash
# 1. 檢查日誌大小
du -sh logs/
docker system df

# 2. 檢查日誌設定
docker-compose logs adhd-backend | head -20

# 3. 檢查日誌級別
docker-compose exec adhd-backend printenv | grep LOG_LEVEL
```

#### 解決方案
```bash
# 解決方案 1：調整日誌級別
# 在 .env 中：
LOG_LEVEL=Warning  # 生產環境
LOG_LEVEL=Information  # 測試環境
LOG_LEVEL=Debug  # 開發環境

# 解決方案 2：設定日誌輪轉
# 在 docker-compose.yml 中：
logging:
  driver: "json-file"
  options:
    max-size: "10m"
    max-file: "3"

# 解決方案 3：清理舊日誌
find logs/ -name "*.txt" -mtime +7 -delete
docker system prune -f

# 解決方案 4：使用外部日誌服務
# 配置 ELK Stack 或 Seq
```

### 問題：監控和警報不工作

#### 症狀
- 健康檢查失敗
- 指標收集中斷
- 警報未觸發

#### 診斷步驟
```bash
# 1. 檢查健康檢查端點
curl http://localhost/health

# 2. 檢查 Prometheus 指標
curl http://localhost:5000/metrics

# 3. 檢查 Grafana 連接
curl http://localhost:3000

# 4. 檢查警報規則
# 查看 Prometheus 的 alerts 頁面
```

#### 解決方案
```bash
# 解決方案 1：重啟監控服務
docker-compose restart prometheus grafana

# 解決方案 2：檢查指標端點
# 確保 /metrics 端點可訪問

# 解決方案 3：更新 Grafana 資料源
# 在 Grafana 中重新配置 Prometheus 資料源

# 解決方案 4：驗證警報配置
# 檢查 alert.rules.yml 檔案語法
```

## 🔢 常見錯誤代碼

### HTTP 狀態碼

| 狀態碼 | 含義 | 常見原因 | 解決方案 |
|--------|------|----------|----------|
| 400 | Bad Request | 請求格式錯誤 | 檢查請求體格式和參數 |
| 401 | Unauthorized | 認證失敗 | 檢查 JWT 令牌 |
| 403 | Forbidden | 權限不足 | 檢查使用者權限 |
| 404 | Not Found | 資源不存在 | 檢查 URL 和路由設定 |
| 429 | Too Many Requests | 請求過頻繁 | 等待或調整速率限制 |
| 500 | Internal Server Error | 伺服器錯誤 | 檢查伺服器日誌 |
| 502 | Bad Gateway | 代理錯誤 | 檢查 Nginx 設定 |
| 503 | Service Unavailable | 服務不可用 | 檢查服務狀態 |

### 應用程式錯誤代碼

| 錯誤代碼 | 含義 | 解決方案 |
|----------|------|----------|
| ADHD_AUTH_001 | JWT 令牌過期 | 重新登入 |
| ADHD_AUTH_002 | 無效的刷新令牌 | 清除認證狀態並重新登入 |
| ADHD_DB_001 | 資料庫連接失敗 | 檢查資料庫服務 |
| ADHD_DB_002 | 查詢超時 | 優化查詢或增加超時時間 |
| ADHD_CACHE_001 | Redis 連接失敗 | 檢查 Redis 服務 |
| ADHD_API_001 | 外部 API 調用失敗 | 檢查網路連接和 API 狀態 |

## 🔄 升級和遷移問題

### 問題：系統升級失敗

#### 症狀
- 新版本無法啟動
- 資料庫遷移失敗
- 配置不相容

#### 診斷步驟
```bash
# 1. 檢查版本相容性
docker-compose --version
docker --version

# 2. 檢查映像版本
docker images | grep adhd

# 3. 檢查遷移狀態
docker-compose exec adhd-postgres psql -U adhd_user -d adhd_productivity -c "SELECT * FROM __EFMigrationsHistory ORDER BY migration_id DESC LIMIT 5;"

# 4. 備份驗證
ls -la backups/
```

#### 解決方案
```bash
# 解決方案 1：回滾到上一版本
docker-compose down
git checkout previous-tag
docker-compose up -d

# 解決方案 2：手動執行遷移
docker-compose exec adhd-backend dotnet ef database update

# 解決方案 3：從備份還原
docker-compose down
docker volume rm adhd_postgres_data
# 還原備份
cat backup.sql | docker-compose exec -T adhd-postgres psql -U adhd_user -d adhd_productivity
docker-compose up -d

# 解決方案 4：清理並重新部署
docker-compose down -v
docker system prune -a -f
# 重新部署
```

## 🛠️ 開發環境問題

### 問題：熱重載不工作

#### 症狀
- 代碼變更後需要手動重啟
- 前端熱重載失效
- 後端不會自動重新編譯

#### 診斷步驟
```bash
# 1. 檢查開發模式設定
docker-compose exec adhd-backend printenv ASPNETCORE_ENVIRONMENT
docker-compose exec adhd-frontend printenv NODE_ENV

# 2. 檢查 Volume 掛載
docker-compose exec adhd-backend ls -la /app

# 3. 檢查檔案權限
ls -la src/
```

#### 解決方案
```bash
# 解決方案 1：使用開發配置
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d

# 解決方案 2：檢查 Volume 設定
# 在 docker-compose.override.yml 中：
volumes:
  - ./src:/app/src
  - ./frontend/src:/app/src

# 解決方案 3：重啟開發伺服器
docker-compose restart adhd-backend adhd-frontend

# 解決方案 4：本地開發模式
cd backend && dotnet watch run
cd frontend && npm run dev
```

### 問題：IDE 偵錯不工作

#### 症狀
- 無法附加偵錯器
- 斷點不會觸發
- 變數檢視失效

#### 診斷步驟
```bash
# 1. 檢查偵錯模式設定
docker-compose ps

# 2. 檢查端口映射
netstat -tlnp | grep 5000

# 3. 檢查偵錯符號
docker-compose exec adhd-backend ls -la /app/*.pdb
```

#### 解決方案
```bash
# 解決方案 1：啟用偵錯模式
# 在 docker-compose.override.yml 中：
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - DOTNET_USE_POLLING_FILE_WATCHER=1
ports:
  - "5000:5000"
  - "5001:5001"  # HTTPS

# 解決方案 2：使用遠端偵錯
# 在 Dockerfile.debug 中：
RUN dotnet tool install --global vsdbg

# 解決方案 3：本地運行
cd backend
dotnet run --project src/AdhdProductivitySystem.Api

# 解決方案 4：檢查 IDE 設定
# 確保 IDE 指向正確的端口和協定
```

## 📞 取得協助

### 自助資源
1. **檢查文檔**: 查看 [README.md](../README.md) 和相關文檔
2. **搜尋日誌**: 使用關鍵字搜尋錯誤訊息
3. **社群支援**: 檢查 GitHub Issues 的相似問題

### 提交問題報告
當提交問題報告時，請包含：

```bash
# 系統資訊
echo "=== 系統資訊 ==="
uname -a
docker --version
docker-compose --version

echo "=== 服務狀態 ==="
docker-compose ps

echo "=== 錯誤日誌 ==="
docker-compose logs --tail=50

echo "=== 配置檢查 ==="
docker-compose config

echo "=== 資源使用 ==="
docker stats --no-stream
```

### 緊急聯絡方式
- **GitHub Issues**: [專案 Issues 頁面]
- **Discord/Slack**: [開發團隊聊天室]
- **電子郵件**: support@adhd-productivity.dev

---

**版本**: 1.0.0  
**最後更新**: 2024年12月22日  
**維護者**: ADHD 生產力系統開發團隊