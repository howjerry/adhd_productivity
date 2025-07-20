# ADHD 生產力系統 - 運維手冊

## 📋 目錄

1. [系統概覽](#系統概覽)
2. [日常運維操作](#日常運維操作)
3. [監控和警報](#監控和警報)
4. [備份和恢復](#備份和恢復)
5. [故障排除指南](#故障排除指南)
6. [效能調優](#效能調優)
7. [安全管理](#安全管理)
8. [緊急回應程序](#緊急回應程序)
9. [維護計劃](#維護計劃)
10. [聯絡資訊](#聯絡資訊)

---

## 🎯 系統概覽

### 架構簡介

ADHD 生產力系統採用容器化微服務架構，包含以下主要組件：

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Nginx Proxy   │────│  React Frontend │────│ ASP.NET Backend │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                                        │
                       ┌─────────────────┐    ┌─────────────────┐
                       │   Redis Cache   │────│   PostgreSQL    │
                       └─────────────────┘    └─────────────────┘
                                │
                       ┌─────────────────┐    ┌─────────────────┐
                       │   Prometheus    │────│     Grafana     │
                       └─────────────────┘    └─────────────────┘
```

### 服務端口對應

| 服務 | 內部端口 | 外部端口 | 用途 |
|------|----------|----------|------|
| Nginx | 80, 443 | 80, 443 | 反向代理和 SSL 終止 |
| Frontend | 80 | - | React 應用程式 |
| Backend | 5000 | - | ASP.NET Core API |
| PostgreSQL | 5432 | - | 主資料庫 |
| Redis | 6379 | - | 快取和會話儲存 |
| Prometheus | 9090 | 9090 | 指標收集 |
| Grafana | 3000 | 3000 | 監控儀表板 |

### 資料目錄結構

```
/opt/adhd-productivity/
├── docker-compose.production.yml
├── .env.production
├── nginx/
│   ├── nginx.production.conf
│   └── conf.d/
├── certs/
│   ├── adhd-productivity.crt
│   └── adhd-productivity.key
├── logs/
├── backups/
└── monitoring/
```

---

## 🔧 日常運維操作

### 系統啟動和停止

```bash
# 啟動所有服務
cd /opt/adhd-productivity
docker-compose -f docker-compose.production.yml up -d

# 停止所有服務
docker-compose -f docker-compose.production.yml down

# 重啟特定服務
docker-compose -f docker-compose.production.yml restart adhd-backend-prod

# 查看服務狀態
docker-compose -f docker-compose.production.yml ps

# 查看服務日誌
docker-compose -f docker-compose.production.yml logs -f adhd-backend-prod
```

### 容器管理

```bash
# 查看所有容器狀態
docker ps -a

# 進入容器執行命令
docker exec -it adhd-backend-prod bash
docker exec -it adhd-postgres-prod psql -U adhd_prod_user -d adhd_productivity_prod

# 查看容器資源使用
docker stats

# 清理未使用的資源
docker system prune -f
docker volume prune -f
```

### 日誌管理

```bash
# 查看應用程式日誌
tail -f /opt/adhd-productivity/logs/adhd-system.log

# 查看 Nginx 日誌
tail -f /var/log/nginx/access.log
tail -f /var/log/nginx/error.log

# 搜尋錯誤日誌
grep "ERROR\|FATAL" /opt/adhd-productivity/logs/adhd-system.log

# 清理舊日誌 (手動)
find /opt/adhd-productivity/logs -name "*.log" -mtime +30 -delete
```

### 資料庫維護

```bash
# 連接到資料庫
docker exec -it adhd-postgres-prod psql -U adhd_prod_user -d adhd_productivity_prod

# 常用資料庫操作
-- 查看資料庫大小
SELECT pg_size_pretty(pg_database_size('adhd_productivity_prod'));

-- 查看活躍連接
SELECT count(*) FROM pg_stat_activity WHERE state = 'active';

-- 查看慢查詢
SELECT query, calls, total_time, mean_time 
FROM pg_stat_statements 
ORDER BY mean_time DESC LIMIT 10;

-- 執行 VACUUM ANALYZE
VACUUM ANALYZE;

# 資料庫備份
docker exec adhd-postgres-prod pg_dump -U adhd_prod_user adhd_productivity_prod | gzip > backup.sql.gz
```

### 快取管理

```bash
# 連接到 Redis
docker exec -it adhd-redis-prod redis-cli -a $REDIS_PASSWORD

# Redis 常用命令
redis-cli> INFO memory
redis-cli> INFO stats
redis-cli> KEYS pattern*
redis-cli> FLUSHDB  # 清空當前資料庫
redis-cli> CONFIG GET maxmemory
```

---

## 📊 監控和警報

### Prometheus 指標監控

**存取方式：** http://your-domain:9090

**重要指標：**

```promql
# CPU 使用率
100 - (avg by(instance) (irate(node_cpu_seconds_total{mode="idle"}[5m])) * 100)

# 記憶體使用率
(1 - (node_memory_MemAvailable_bytes / node_memory_MemTotal_bytes)) * 100

# HTTP 請求率
rate(http_requests_total[5m])

# 錯誤率
rate(http_requests_total{status=~"5.."}[5m]) / rate(http_requests_total[5m]) * 100

# 資料庫連接數
postgres_stat_database_numbackends
```

### Grafana 儀表板

**存取方式：** http://your-domain:3000

**預設登入：** admin / admin123 (請立即更改)

**重要儀表板：**
- System Overview - 系統整體監控
- Application Metrics - 應用程式效能
- Database Performance - 資料庫監控
- Security Dashboard - 安全事件監控

### 警報設定

警報會發送到配置的 Slack 頻道和郵件地址。

**嚴重警報：**
- 服務停止運行
- CPU 使用率 > 90%
- 記憶體使用率 > 95%
- 磁碟空間 < 10%
- 錯誤率 > 10%

**警告警報：**
- CPU 使用率 > 80%
- 記憶體使用率 > 85%
- 回應時間 > 2 秒
- SSL 憑證 30 天內到期

---

## 💾 備份和恢復

### 自動備份

備份服務每日自動執行：

```bash
# 檢查備份狀態
docker logs adhd-backup-service

# 手動執行備份
cd /opt/adhd-productivity/scripts/backup
./backup-database.sh
./backup-redis.sh
```

### 備份驗證

```bash
# 檢查最新備份
ls -la /opt/adhd-productivity/backups/

# 驗證備份完整性
cd /opt/adhd-productivity/scripts/backup
./backup-verify.sh

# 測試資料庫備份恢復
./restore-database.sh backup_file.sql.gz.enc --dry-run
```

### 災難恢復程序

**緊急恢復步驟：**

1. **評估損壞範圍**
   ```bash
   # 檢查服務狀態
   docker ps -a
   docker-compose -f docker-compose.production.yml ps
   ```

2. **停止受影響的服務**
   ```bash
   docker-compose -f docker-compose.production.yml down
   ```

3. **恢復資料庫**
   ```bash
   cd /opt/adhd-productivity/scripts/backup
   ./restore-database.sh latest_backup.sql.gz.enc --force
   ```

4. **恢復 Redis 資料**
   ```bash
   ./restore-redis.sh latest_redis_backup.rdb.gz.enc
   ```

5. **重啟所有服務**
   ```bash
   docker-compose -f docker-compose.production.yml up -d
   ```

6. **驗證系統功能**
   ```bash
   curl -f http://localhost/health
   curl -f http://localhost/api/health
   ```

### RTO/RPO 目標

- **RTO (Recovery Time Objective):** 4 小時
- **RPO (Recovery Point Objective):** 24 小時
- **資料備份頻率：** 每日
- **備份保留期限：** 30 天

---

## 🔧 故障排除指南

### 常見問題診斷

#### 1. 服務無法啟動

**症狀：** 容器啟動失敗或不斷重啟

**診斷步驟：**
```bash
# 查看容器狀態
docker ps -a

# 查看容器日誌
docker logs adhd-backend-prod
docker logs adhd-postgres-prod

# 查看系統資源
df -h
free -h
docker system df
```

**常見原因和解決方案：**
- **磁碟空間不足：** 清理舊檔案，擴展磁碟
- **記憶體不足：** 重啟系統，調整容器記憶體限制
- **端口衝突：** 檢查端口使用情況，修改配置
- **配置錯誤：** 檢查環境變數和配置檔案

#### 2. 資料庫連接失敗

**症狀：** 應用程式無法連接到資料庫

**診斷步驟：**
```bash
# 檢查 PostgreSQL 容器狀態
docker ps | grep postgres

# 測試資料庫連接
docker exec adhd-postgres-prod pg_isready -U adhd_prod_user

# 查看資料庫日誌
docker logs adhd-postgres-prod

# 檢查網路連通性
docker exec adhd-backend-prod ping adhd-postgres-prod
```

**解決方案：**
- 重啟資料庫容器
- 檢查資料庫憑證
- 驗證網路配置
- 檢查防火牆設定

#### 3. 效能問題

**症狀：** 回應時間慢，系統卡頓

**診斷步驟：**
```bash
# 檢查系統負載
top
htop
iostat 1

# 檢查資料庫效能
docker exec adhd-postgres-prod psql -U adhd_prod_user -d adhd_productivity_prod -c "
SELECT query, calls, total_time, mean_time 
FROM pg_stat_statements 
ORDER BY mean_time DESC LIMIT 10;"

# 檢查 Redis 效能
docker exec adhd-redis-prod redis-cli -a $REDIS_PASSWORD INFO stats
```

**優化措施：**
- 增加系統資源
- 優化慢查詢
- 調整快取策略
- 檢查索引使用情況

#### 4. SSL 憑證問題

**症狀：** HTTPS 存取失敗或憑證警告

**診斷步驟：**
```bash
# 檢查憑證有效期
openssl x509 -in /opt/adhd-productivity/certs/adhd-productivity.crt -noout -dates

# 測試 SSL 配置
openssl s_client -connect your-domain:443 -servername your-domain

# 檢查 Nginx 配置
nginx -t
```

**解決方案：**
- 更新 SSL 憑證
- 檢查 Nginx SSL 配置
- 驗證域名配置

### 緊急聯絡清單

```bash
# 檢查系統狀態的快速命令
cat > /usr/local/bin/system-status << 'EOF'
#!/bin/bash
echo "=== ADHD System Status ==="
echo "Date: $(date)"
echo ""

echo "=== Container Status ==="
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
echo ""

echo "=== System Resources ==="
echo "CPU: $(top -bn1 | grep "Cpu(s)" | awk '{print $2}')"
echo "Memory: $(free -h | grep Mem | awk '{print $3"/"$2}')"
echo "Disk: $(df -h / | tail -1 | awk '{print $5}')"
echo ""

echo "=== Recent Errors ==="
tail -5 /opt/adhd-productivity/logs/adhd-system.log | grep -i error || echo "No recent errors"
EOF

chmod +x /usr/local/bin/system-status
```

---

## ⚡ 效能調優

### 系統層級優化

```bash
# 調整系統參數
echo 'vm.max_map_count=262144' >> /etc/sysctl.conf
echo 'fs.file-max=65536' >> /etc/sysctl.conf
sysctl -p

# 調整檔案描述符限制
echo '* soft nofile 65536' >> /etc/security/limits.conf
echo '* hard nofile 65536' >> /etc/security/limits.conf
```

### PostgreSQL 調優

```sql
-- 在 PostgreSQL 中執行
-- 調整記憶體設定
ALTER SYSTEM SET shared_buffers = '256MB';
ALTER SYSTEM SET effective_cache_size = '1GB';
ALTER SYSTEM SET work_mem = '8MB';

-- 調整檢查點設定
ALTER SYSTEM SET checkpoint_completion_target = 0.9;
ALTER SYSTEM SET checkpoint_timeout = '10min';

-- 重載配置
SELECT pg_reload_conf();

-- 查看慢查詢
SELECT query, calls, total_time, mean_time, rows
FROM pg_stat_statements 
WHERE mean_time > 1000 
ORDER BY mean_time DESC 
LIMIT 10;
```

### Redis 調優

```bash
# 調整 Redis 設定
docker exec adhd-redis-prod redis-cli -a $REDIS_PASSWORD CONFIG SET maxmemory-policy allkeys-lru
docker exec adhd-redis-prod redis-cli -a $REDIS_PASSWORD CONFIG SET save "900 1 300 10 60 10000"

# 監控 Redis 效能
docker exec adhd-redis-prod redis-cli -a $REDIS_PASSWORD INFO memory
docker exec adhd-redis-prod redis-cli -a $REDIS_PASSWORD INFO stats
```

### 應用程式調優

```bash
# 調整容器資源限制
# 編輯 docker-compose.production.yml
deploy:
  resources:
    limits:
      memory: 2G
      cpus: '1.0'
    reservations:
      memory: 1G
      cpus: '0.5'
```

---

## 🔒 安全管理

### 定期安全檢查

```bash
# 執行安全掃描
cd /opt/adhd-productivity/scripts/security
./security-scanner.sh

# 檢查容器安全
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  aquasec/trivy image adhd-backend:latest

# 更新系統和套件
apt update && apt upgrade -y
npm audit fix
dotnet list package --vulnerable
```

### 憑證管理

```bash
# 檢查憑證到期時間
openssl x509 -in /opt/adhd-productivity/certs/adhd-productivity.crt -noout -dates

# 使用 Let's Encrypt 更新憑證 (如果使用)
certbot renew --dry-run
certbot renew

# 重載 Nginx 配置
docker exec adhd-nginx-prod nginx -s reload
```

### 使用者管理

```bash
# 檢查系統使用者
cat /etc/passwd | grep -E "adhd|app"

# 檢查 SSH 登入
tail -20 /var/log/auth.log

# 更改應用程式密碼
# 編輯 .env.production 檔案並重啟服務
```

---

## 🚨 緊急回應程序

### 安全事件回應

**1. 立即行動 (15 分鐘內):**
```bash
# 隔離受影響的系統
docker-compose -f docker-compose.production.yml stop

# 收集證據
cp -r /opt/adhd-productivity/logs /tmp/incident-$(date +%Y%m%d_%H%M%S)/
docker logs adhd-backend-prod > /tmp/backend-logs-$(date +%Y%m%d_%H%M%S).log

# 通知團隊
# 發送警報到 Slack/郵件群組
```

**2. 評估和分析 (1 小時內):**
```bash
# 執行安全掃描
cd /opt/adhd-productivity/scripts/security
./security-scanner.sh

# 分析日誌
cd /opt/adhd-productivity/scripts/logging
./audit-log-analyzer.sh
```

**3. 恢復和加固 (4 小時內):**
```bash
# 從備份恢復 (如果需要)
cd /opt/adhd-productivity/scripts/backup
./restore-database.sh latest_backup.sql.gz.enc

# 應用安全補丁
# 更新配置和密碼
# 重啟服務

# 驗證系統安全
./security-scanner.sh
```

### 系統故障回應

**嚴重等級定義：**
- **P0 (嚴重):** 系統完全無法使用
- **P1 (高):** 核心功能不可用
- **P2 (中):** 部分功能受影響
- **P3 (低):** 輕微影響或建議改進

**回應時間要求：**
- P0: 15 分鐘內回應，1 小時內解決
- P1: 30 分鐘內回應，4 小時內解決
- P2: 2 小時內回應，24 小時內解決
- P3: 24 小時內回應，計劃修復

---

## 📅 維護計劃

### 每日維護

```bash
# 自動執行的日常檢查
cat > /usr/local/bin/daily-maintenance << 'EOF'
#!/bin/bash
# 檢查系統狀態
system-status

# 檢查磁碟空間
df -h | awk '$5 > 80 {print "Warning: " $0}'

# 檢查備份狀態
find /opt/adhd-productivity/backups -name "*.enc" -mtime -1 | wc -l

# 檢查日誌錯誤
tail -100 /opt/adhd-productivity/logs/adhd-system.log | grep -i error | tail -5

# 檢查容器健康狀態
docker ps --filter health=unhealthy
EOF

chmod +x /usr/local/bin/daily-maintenance

# 加入 crontab
echo "0 8 * * * /usr/local/bin/daily-maintenance" | crontab -
```

### 每週維護

- 檢查和清理日誌檔案
- 更新系統套件
- 檢查 SSL 憑證狀態
- 執行效能分析
- 檢查備份完整性

### 每月維護

- 執行完整安全掃描
- 檢查和優化資料庫
- 更新監控儀表板
- 檢查災難恢復程序
- 進行效能基準測試

### 每季維護

- 進行滲透測試
- 檢查合規性要求
- 更新災難恢復計劃
- 進行容量規劃
- 團隊安全培訓

---

## 📞 聯絡資訊

### 緊急聯絡人

| 角色 | 姓名 | 電話 | 郵件 | 備註 |
|------|------|------|------|------|
| 技術負責人 | [姓名] | [電話] | [郵件] | 主要技術決策 |
| 系統管理員 | [姓名] | [電話] | [郵件] | 24/7 值班 |
| 資安負責人 | [姓名] | [電話] | [郵件] | 安全事件 |
| 產品經理 | [姓名] | [電話] | [郵件] | 業務決策 |

### 外部服務聯絡

| 服務商 | 聯絡方式 | 帳號/合約 | 用途 |
|--------|----------|-----------|------|
| 雲端服務商 | [支援電話/郵件] | [帳號ID] | 基礎設施 |
| 域名註冊商 | [聯絡資訊] | [帳號] | 域名管理 |
| SSL 憑證商 | [聯絡資訊] | [憑證ID] | SSL 憑證 |
| 監控服務 | [聯絡資訊] | [帳號] | 外部監控 |

### 內部工具和服務

```bash
# 快速存取常用工具
alias logs='tail -f /opt/adhd-productivity/logs/adhd-system.log'
alias status='system-status'
alias backup='cd /opt/adhd-productivity/scripts/backup'
alias security='cd /opt/adhd-productivity/scripts/security'
alias db='docker exec -it adhd-postgres-prod psql -U adhd_prod_user -d adhd_productivity_prod'
alias redis='docker exec -it adhd-redis-prod redis-cli -a $REDIS_PASSWORD'
```

---

## 📋 附錄

### 常用指令速查

```bash
# 系統狀態檢查
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
docker stats --no-stream
free -h && df -h

# 服務管理
docker-compose -f docker-compose.production.yml up -d
docker-compose -f docker-compose.production.yml restart [service]
docker-compose -f docker-compose.production.yml logs -f [service]

# 資料庫操作
docker exec -it adhd-postgres-prod psql -U adhd_prod_user -d adhd_productivity_prod
docker exec adhd-postgres-prod pg_dump -U adhd_prod_user adhd_productivity_prod

# 備份和恢復
cd /opt/adhd-productivity/scripts/backup
./backup-database.sh
./restore-database.sh [backup_file] --dry-run

# 安全檢查
cd /opt/adhd-productivity/scripts/security
./security-scanner.sh
```

### 版本歷史

| 版本 | 日期 | 變更內容 | 作者 |
|------|------|----------|------|
| 1.0.0 | 2024-12-22 | 初始版本 | DevOps Team |

---

**最後更新：** 2024年12月22日  
**文件版本：** 1.0.0  
**維護團隊：** ADHD 生產力系統 DevOps 團隊

> 此文件應定期更新，確保與系統實際配置保持一致。如有疑問或建議，請聯絡技術團隊。