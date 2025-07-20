# ADHD 生產力系統 - 監控和日誌指南

## 概述

本文檔詳細說明 ADHD 生產力系統的完整監控和日誌解決方案，包括 Prometheus 指標收集、Grafana 視覺化、AlertManager 警報系統，以及結構化日誌記錄。

## 🏗️ 監控架構

### 核心元件

1. **Prometheus** - 指標收集和儲存
2. **Grafana** - 資料視覺化和儀表板
3. **AlertManager** - 警報管理和通知
4. **Exporters** - 各種服務的指標匯出器
5. **Serilog** - 結構化日誌記錄

### 服務埠口對應

| 服務 | 埠口 | 說明 |
|------|------|------|
| Prometheus | 9090 | 指標收集和查詢 |
| Grafana | 3001 | 視覺化儀表板 |
| AlertManager | 9093 | 警報管理 |
| PostgreSQL Exporter | 9187 | 資料庫指標 |
| Redis Exporter | 9121 | 快取指標 |
| Node Exporter | 9100 | 系統資源指標 |
| cAdvisor | 8080 | 容器指標 |
| 主應用程式 Metrics | /metrics | 應用程式指標 |

## 🚀 快速開始

### 1. 啟動監控系統

```bash
# 使用監控啟動腳本
./scripts/start-monitoring.sh

# 或手動啟動
docker-compose --env-file .env.monitoring --profile monitoring up -d
```

### 2. 驗證監控系統

```bash
# 執行監控驗證腳本
./scripts/validate-monitoring.sh
```

### 3. 訪問監控介面

- **主應用程式**: http://localhost
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3001 (admin/admin123)
- **AlertManager**: http://localhost:9093

## 📊 Grafana 儀表板

### 預設儀表板

1. **ADHD 系統概覽** (`adhd-system-overview`)
   - 系統 CPU 和記憶體使用率
   - HTTP 請求率和回應時間
   - 資料庫和快取連線數

### 自訂儀表板

在 `monitoring/grafana/dashboards/` 目錄中新增 JSON 儀表板檔案，系統會自動載入。

## 🔍 監控指標

### 應用程式指標

#### HTTP 相關指標
- `adhd_http_requests_total` - HTTP 請求總數
- `adhd_http_request_duration_seconds` - HTTP 請求持續時間

#### 任務相關指標
- `adhd_tasks_created_total` - 建立的任務總數
- `adhd_active_tasks_count` - 目前活躍任務數量
- `adhd_tasks_completed_total` - 完成的任務總數

#### 快取相關指標
- `adhd_cache_hits_total` - 快取命中總數
- `adhd_cache_misses_total` - 快取未命中總數
- `adhd_cache_operation_duration_seconds` - 快取操作持續時間

#### 資料庫相關指標
- `adhd_database_queries_total` - 資料庫查詢總數
- `adhd_database_query_duration_seconds` - 資料庫查詢持續時間
- `adhd_database_connections_total` - 資料庫連線總數

#### SignalR 相關指標
- `adhd_signalr_connections_count` - SignalR 連線數
- `adhd_signalr_messages_total` - SignalR 訊息總數

#### 計時器相關指標
- `adhd_timer_sessions_total` - 計時器會話總數
- `adhd_timer_session_duration_seconds` - 計時器會話持續時間
- `adhd_active_timer_sessions_count` - 目前活躍計時器會話數

### 系統指標

#### 資源監控
- CPU 使用率 (`node_cpu_seconds_total`)
- 記憶體使用率 (`node_memory_*`)
- 磁碟使用率 (`node_filesystem_*`)
- 網路 I/O (`node_network_*`)

#### PostgreSQL 指標
- 連線數 (`pg_stat_activity_count`)
- 查詢效能 (`pg_stat_statements_*`)
- 資料庫大小 (`pg_database_size_bytes`)

#### Redis 指標
- 記憶體使用 (`redis_memory_used_bytes`)
- 連線數 (`redis_connected_clients`)
- 命令統計 (`redis_commands_total`)

#### .NET Runtime 指標
- 垃圾回收 (`dotnet_collection_count_total`)
- 執行緒池 (`dotnet_threadpool_*`)
- 異常 (`dotnet_exceptions_total`)

## 🚨 警報系統

### 警報規則

警報規則定義在 `monitoring/rules/adhd-system-alerts.yml`：

#### 基礎設施警報
- **ServiceDown** - 服務停機
- **HighCPUUsage** - CPU 使用率過高 (>80%)
- **HighMemoryUsage** - 記憶體使用率過高 (>85%)
- **LowDiskSpace** - 磁碟空間不足 (>85%)

#### 資料庫警報
- **PostgreSQLTooManyConnections** - 連線數過多
- **PostgreSQLSlowQueries** - 查詢執行過慢
- **PostgreSQLDeadlocks** - 發生死鎖

#### 快取警報
- **RedisHighMemoryUsage** - Redis 記憶體使用率過高
- **RedisTooManyConnections** - Redis 連線數過多
- **RedisKeyEviction** - Redis 開始回收 key

#### 應用程式警報
- **HighHTTPErrorRate** - HTTP 5xx 錯誤率過高
- **HighResponseTime** - 回應時間過長
- **HighGCPressure** - 垃圾回收壓力過高
- **ThreadPoolStarvation** - 執行緒池佇列過長

### 警報通知

警報通知配置在 `monitoring/alertmanager.yml`：

- **critical-alerts** - 嚴重警報（立即通知）
- **database-alerts** - 資料庫相關警報
- **cache-alerts** - 快取相關警報
- **backend-alerts** - 後端應用程式警報

## 📝 日誌系統

### 日誌類型

1. **主要日誌** (`logs/adhd-productivity-system-*.txt`)
   - 所有 Information 級別以上的日誌
   - 包含詳細的上下文資訊

2. **錯誤日誌** (`logs/errors/adhd-errors-*.txt`)
   - 僅 Error 級別的日誌
   - 用於快速定位問題

3. **效能日誌** (`logs/performance/adhd-performance-*.txt`)
   - Warning 級別以上的效能相關日誌
   - 包含慢查詢、高 CPU 使用等

4. **結構化日誌** (`logs/structured/adhd-structured-*.json`)
   - JSON 格式的結構化日誌
   - 便於程式化分析和搜尋

### 日誌欄位

每個日誌條目包含以下欄位：

```json
{
  "@t": "2024-07-20T12:00:00.000Z",
  "@l": "Information",
  "@mt": "HTTP Request Completed",
  "RequestId": "abc123",
  "CorrelationId": "def456",
  "Method": "GET",
  "Path": "/api/tasks",
  "StatusCode": 200,
  "ElapsedMs": 150,
  "ClientIP": "192.168.1.100",
  "UserId": "user123",
  "Properties": {}
}
```

### 日誌查詢

#### 使用 grep 查詢日誌

```bash
# 查詢特定時間的錯誤
grep "2024-07-20 12:" logs/errors/adhd-errors-*.txt

# 查詢特定使用者的請求
grep "UserId.*user123" logs/structured/adhd-structured-*.json

# 查詢慢請求
grep "ElapsedMs.*[5-9][0-9][0-9][0-9]" logs/structured/adhd-structured-*.json
```

#### 使用 jq 查詢結構化日誌

```bash
# 查詢所有錯誤日誌
cat logs/structured/adhd-structured-*.json | jq 'select(.@l == "Error")'

# 查詢特定時間範圍的請求
cat logs/structured/adhd-structured-*.json | jq 'select(.@t > "2024-07-20T12:00:00Z")'

# 統計請求狀態碼
cat logs/structured/adhd-structured-*.json | jq '.StatusCode' | sort | uniq -c
```

## 🔧 故障排除

### 常見問題

#### 1. Prometheus 無法抓取指標

**症狀**: Prometheus targets 顯示 "down"

**解決方案**:
```bash
# 檢查應用程式 metrics 端點
curl http://localhost/metrics

# 檢查網路連通性
docker exec adhd-prometheus wget -qO- http://adhd-backend:5000/metrics

# 檢查防火牆設定
```

#### 2. Grafana 無法載入儀表板

**症狀**: Grafana 顯示 "No data"

**解決方案**:
```bash
# 檢查 Prometheus 資料源
curl -u admin:admin123 http://localhost:3001/api/datasources

# 重新載入儀表板
docker-compose --env-file .env.monitoring restart adhd-grafana
```

#### 3. 日誌檔案過大

**症狀**: 磁碟空間不足

**解決方案**:
```bash
# 壓縮舊日誌
find logs/ -name "*.txt" -mtime +7 -exec gzip {} \;

# 刪除超過 30 天的日誌
find logs/ -name "*.gz" -mtime +30 -delete
```

#### 4. 記憶體使用率過高

**症狀**: 系統回應緩慢

**檢查步驟**:
```bash
# 檢查容器記憶體使用
docker stats

# 檢查 Prometheus 指標
curl -s http://localhost:9090/api/v1/query?query=container_memory_usage_bytes

# 檢查垃圾回收壓力
curl -s http://localhost/metrics | grep dotnet_collection
```

### 監控健康檢查

#### 自動化檢查腳本

```bash
# 執行完整的監控系統檢查
./scripts/validate-monitoring.sh

# 檢查特定服務
docker-compose --env-file .env.monitoring ps adhd-prometheus
```

#### 手動檢查清單

1. ✅ 所有容器正在運行
2. ✅ Prometheus 可以抓取所有目標
3. ✅ Grafana 可以查詢 Prometheus 資料
4. ✅ AlertManager 正在接收警報
5. ✅ 日誌檔案正在生成
6. ✅ 應用程式指標正在收集

## 📈 效能調優

### Prometheus 設定

```yaml
# prometheus.yml
global:
  scrape_interval: 15s     # 調整抓取間隔
  evaluation_interval: 15s # 調整評估間隔

scrape_configs:
  - job_name: 'adhd-backend'
    scrape_interval: 10s   # 高頻率監控關鍵服務
```

### Grafana 最佳化

1. **限制查詢時間範圍** - 避免查詢過長時間範圍
2. **使用適當的時間間隔** - 根據資料密度調整
3. **啟用查詢快取** - 減少重複查詢負載

### 日誌最佳化

1. **日誌輪轉** - 自動壓縮和刪除舊日誌
2. **選擇性記錄** - 只記錄必要的資訊
3. **異步寫入** - 使用異步日誌寫入提升效能

## 🔐 安全考量

### 訪問控制

1. **Grafana 認證** - 使用強密碼並啟用 HTTPS
2. **網路隔離** - 監控服務僅在內部網路可訪問
3. **資料加密** - 敏感資料傳輸加密

### 資料保護

1. **日誌脫敏** - 避免記錄敏感資訊
2. **資料保留** - 定期清理過期資料
3. **備份策略** - 定期備份監控配置

## 📚 進階功能

### 自訂指標

```csharp
// 在應用程式中添加自訂指標
public static class CustomMetrics
{
    private static readonly Counter UserRegistrations = Metrics
        .CreateCounter("adhd_user_registrations_total", "使用者註冊總數");
    
    public static void IncrementUserRegistrations()
    {
        UserRegistrations.Inc();
    }
}
```

### 自訂警報

```yaml
# 添加到 monitoring/rules/custom-alerts.yml
groups:
  - name: custom_alerts
    rules:
      - alert: HighUserRegistrationRate
        expr: rate(adhd_user_registrations_total[5m]) > 10
        for: 2m
        labels:
          severity: warning
        annotations:
          summary: "使用者註冊率異常"
          description: "過去 5 分鐘使用者註冊率超過 10/分鐘"
```

### 監控儀表板自動化

```bash
# 使用 API 自動建立儀表板
curl -X POST \
  -H "Content-Type: application/json" \
  -u admin:admin123 \
  -d @dashboard.json \
  http://localhost:3001/api/dashboards/db
```

## 🎯 最佳實踐

### 監控策略

1. **四個黃金信號** - 延遲、流量、錯誤、飽和度
2. **分層監控** - 應用程式、服務、基礎設施
3. **主動監控** - 預防性而非反應性

### 警報設計

1. **可操作性** - 每個警報都應該有明確的響應行動
2. **避免疲勞** - 減少誤報和重複警報
3. **分級處理** - 根據嚴重程度分級響應

### 日誌管理

1. **結構化日誌** - 使用一致的結構格式
2. **上下文豐富** - 包含足夠的上下文資訊
3. **效能考量** - 平衡詳細程度和效能影響

---

如需更多協助，請參考：
- [Prometheus 官方文檔](https://prometheus.io/docs/)
- [Grafana 官方文檔](https://grafana.com/docs/)
- [Serilog 官方文檔](https://serilog.net/)