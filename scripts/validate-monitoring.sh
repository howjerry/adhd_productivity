#!/bin/bash

# ADHD 生產力系統 - 監控系統驗證腳本
# 此腳本驗證監控系統各個元件的運行狀態

set -euo pipefail

# 設定顏色輸出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 設定變數
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
ENV_FILE="$PROJECT_ROOT/.env.monitoring"

# 日誌函數
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 檢查服務健康狀態
check_service_health() {
    local service_name="$1"
    local url="$2"
    local expected_status="${3:-200}"
    
    log_info "檢查 $service_name 健康狀態..."
    
    if curl -s -o /dev/null -w "%{http_code}" "$url" | grep -q "$expected_status"; then
        log_success "$service_name 運行正常"
        return 0
    else
        log_error "$service_name 不可用或回應異常"
        return 1
    fi
}

# 檢查 Prometheus 目標狀態
check_prometheus_targets() {
    log_info "檢查 Prometheus 目標狀態..."
    
    local prometheus_url="http://localhost:9090"
    local targets_url="$prometheus_url/api/v1/targets"
    
    if ! curl -s "$targets_url" &> /dev/null; then
        log_error "無法連接到 Prometheus API"
        return 1
    fi
    
    local targets_response
    targets_response=$(curl -s "$targets_url")
    
    # 檢查各個目標的狀態
    local targets=(
        "prometheus"
        "adhd-backend"
        "adhd-postgres"
        "adhd-redis"
        "node-exporter"
        "cadvisor"
    )
    
    local healthy_targets=0
    local total_targets=${#targets[@]}
    
    for target in "${targets[@]}"; do
        if echo "$targets_response" | grep -q "\"job\":\"$target\"" && echo "$targets_response" | grep -A 10 "\"job\":\"$target\"" | grep -q "\"health\":\"up\""; then
            log_success "目標 $target 狀態正常"
            ((healthy_targets++))
        else
            log_warning "目標 $target 狀態異常或無法找到"
        fi
    done
    
    log_info "Prometheus 目標狀態: $healthy_targets/$total_targets 正常"
    
    if [[ $healthy_targets -lt $total_targets ]]; then
        log_warning "部分 Prometheus 目標狀態異常"
        return 1
    fi
    
    return 0
}

# 檢查 Grafana 資料源
check_grafana_datasources() {
    log_info "檢查 Grafana 資料源..."
    
    local grafana_url="http://localhost:3001"
    local datasources_url="$grafana_url/api/datasources"
    
    # 使用預設的 admin 憑證
    if ! curl -s -u "admin:admin123" "$datasources_url" &> /dev/null; then
        log_error "無法連接到 Grafana API"
        return 1
    fi
    
    local datasources_response
    datasources_response=$(curl -s -u "admin:admin123" "$datasources_url")
    
    if echo "$datasources_response" | grep -q "Prometheus"; then
        log_success "Grafana Prometheus 資料源配置正常"
        return 0
    else
        log_warning "Grafana Prometheus 資料源未配置或異常"
        return 1
    fi
}

# 檢查指標收集
check_metrics_collection() {
    log_info "檢查應用程式指標收集..."
    
    local metrics_url="http://localhost/metrics"
    
    if ! curl -s "$metrics_url" &> /dev/null; then
        log_error "無法訪問應用程式指標端點"
        return 1
    fi
    
    local metrics_response
    metrics_response=$(curl -s "$metrics_url")
    
    # 檢查關鍵指標是否存在
    local expected_metrics=(
        "adhd_http_requests_total"
        "adhd_http_request_duration_seconds"
        "adhd_cache_hits_total"
        "adhd_cache_misses_total"
        "adhd_database_queries_total"
        "process_cpu_seconds_total"
        "dotnet_collection_count_total"
    )
    
    local found_metrics=0
    local total_metrics=${#expected_metrics[@]}
    
    for metric in "${expected_metrics[@]}"; do
        if echo "$metrics_response" | grep -q "$metric"; then
            log_success "指標 $metric 收集正常"
            ((found_metrics++))
        else
            log_warning "指標 $metric 未找到"
        fi
    done
    
    log_info "應用程式指標收集: $found_metrics/$total_metrics 正常"
    
    if [[ $found_metrics -lt 5 ]]; then  # 至少需要 5 個關鍵指標
        log_warning "應用程式指標收集不完整"
        return 1
    fi
    
    return 0
}

# 檢查日誌檔案
check_log_files() {
    log_info "檢查日誌檔案..."
    
    local log_dirs=(
        "$PROJECT_ROOT/logs"
        "$PROJECT_ROOT/logs/errors"
        "$PROJECT_ROOT/logs/performance"
        "$PROJECT_ROOT/logs/structured"
    )
    
    local all_logs_ok=true
    
    for log_dir in "${log_dirs[@]}"; do
        if [[ -d "$log_dir" ]]; then
            local log_count
            log_count=$(find "$log_dir" -name "*.txt" -o -name "*.json" | wc -l)
            
            if [[ $log_count -gt 0 ]]; then
                log_success "日誌目錄 $log_dir 包含 $log_count 個日誌檔案"
            else
                log_warning "日誌目錄 $log_dir 沒有日誌檔案"
                all_logs_ok=false
            fi
        else
            log_error "日誌目錄 $log_dir 不存在"
            all_logs_ok=false
        fi
    done
    
    if $all_logs_ok; then
        return 0
    else
        return 1
    fi
}

# 檢查警報規則
check_alert_rules() {
    log_info "檢查警報規則..."
    
    local prometheus_url="http://localhost:9090"
    local rules_url="$prometheus_url/api/v1/rules"
    
    if ! curl -s "$rules_url" &> /dev/null; then
        log_error "無法連接到 Prometheus 規則 API"
        return 1
    fi
    
    local rules_response
    rules_response=$(curl -s "$rules_url")
    
    if echo "$rules_response" | grep -q "\"type\":\"alerting\""; then
        log_success "警報規則已載入"
        
        # 統計警報規則數量
        local rule_count
        rule_count=$(echo "$rules_response" | grep -o "\"type\":\"alerting\"" | wc -l)
        log_info "發現 $rule_count 個警報規則"
        
        return 0
    else
        log_warning "未找到警報規則"
        return 1
    fi
}

# 檢查容器狀態
check_container_status() {
    log_info "檢查容器狀態..."
    
    local compose_file="$PROJECT_ROOT/docker-compose.yml"
    
    if [[ ! -f "$compose_file" ]]; then
        log_error "Docker Compose 檔案不存在: $compose_file"
        return 1
    fi
    
    # 檢查基礎服務
    local base_services=(
        "adhd-postgres"
        "adhd-redis"
        "adhd-backend"
        "adhd-frontend"
        "adhd-nginx"
    )
    
    # 檢查監控服務
    local monitoring_services=(
        "adhd-prometheus"
        "adhd-grafana"
        "adhd-alertmanager"
        "adhd-postgres-exporter"
        "adhd-redis-exporter"
        "adhd-node-exporter"
        "adhd-cadvisor"
    )
    
    local all_services=("${base_services[@]}" "${monitoring_services[@]}")
    local running_services=0
    local total_services=${#all_services[@]}
    
    for service in "${all_services[@]}"; do
        if docker-compose -f "$compose_file" --env-file "$ENV_FILE" ps "$service" 2>/dev/null | grep -q "Up"; then
            log_success "容器 $service 運行中"
            ((running_services++))
        else
            log_warning "容器 $service 未運行"
        fi
    done
    
    log_info "容器狀態: $running_services/$total_services 運行中"
    
    if [[ $running_services -lt $((total_services - 2)) ]]; then  # 允許最多 2 個服務異常
        log_warning "太多容器未運行"
        return 1
    fi
    
    return 0
}

# 生成驗證報告
generate_report() {
    local start_time="$1"
    local end_time
    end_time=$(date)
    
    echo
    echo "========================================"
    echo "ADHD 生產力系統監控驗證報告"
    echo "========================================"
    echo "開始時間: $start_time"
    echo "結束時間: $end_time"
    echo
    
    echo "=== 驗證結果摘要 ==="
    echo "✅ 通過的檢查: $passed_checks"
    echo "⚠️  警告的檢查: $warning_checks"
    echo "❌ 失敗的檢查: $failed_checks"
    echo "📊 總檢查項目: $total_checks"
    echo
    
    local success_rate=$((passed_checks * 100 / total_checks))
    echo "成功率: $success_rate%"
    echo
    
    if [[ $failed_checks -eq 0 && $warning_checks -le 2 ]]; then
        log_success "🎉 監控系統驗證通過！系統運行正常"
        return 0
    elif [[ $failed_checks -le 2 ]]; then
        log_warning "⚠️ 監控系統部分功能異常，但整體可用"
        return 1
    else
        log_error "❌ 監控系統存在嚴重問題，請檢查配置"
        return 2
    fi
}

# 主要執行流程
main() {
    local start_time
    start_time=$(date)
    
    echo "========================================"
    echo "ADHD 生產力系統 - 監控系統驗證"
    echo "========================================"
    echo
    
    # 初始化計數器
    passed_checks=0
    warning_checks=0
    failed_checks=0
    total_checks=0
    
    # 執行各項檢查
    local checks=(
        "check_container_status:容器狀態"
        "check_service_health http://localhost/health:主應用程式健康檢查"
        "check_service_health http://localhost:9090/-/healthy:Prometheus 健康檢查"
        "check_service_health http://localhost:3001/api/health:Grafana 健康檢查"
        "check_service_health http://localhost:9093/-/healthy:AlertManager 健康檢查"
        "check_prometheus_targets:Prometheus 目標狀態"
        "check_grafana_datasources:Grafana 資料源"
        "check_metrics_collection:應用程式指標收集"
        "check_log_files:日誌檔案"
        "check_alert_rules:警報規則"
    )
    
    for check in "${checks[@]}"; do
        local check_func="${check%%:*}"
        local check_desc="${check##*:}"
        
        ((total_checks++))
        
        echo
        log_info "執行檢查: $check_desc"
        
        if eval "$check_func"; then
            ((passed_checks++))
        else
            local exit_code=$?
            if [[ $exit_code -eq 1 ]]; then
                ((warning_checks++))
            else
                ((failed_checks++))
            fi
        fi
    done
    
    generate_report "$start_time"
}

# 執行主函數
main "$@"