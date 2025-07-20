#!/bin/bash

# ADHD 生產力系統 - 監控系統啟動腳本
# 此腳本啟動完整的監控堆疊，包含 Prometheus、Grafana、AlertManager 等

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
COMPOSE_FILE="$PROJECT_ROOT/docker-compose.yml"
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

# 檢查 Docker 和 Docker Compose
check_prerequisites() {
    log_info "檢查系統先決條件..."
    
    if ! command -v docker &> /dev/null; then
        log_error "Docker 未安裝或不在 PATH 中"
        exit 1
    fi
    
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        log_error "Docker Compose 未安裝或不在 PATH 中"
        exit 1
    fi
    
    # 檢查 Docker daemon 是否運行
    if ! docker info &> /dev/null; then
        log_error "Docker daemon 未運行，請啟動 Docker"
        exit 1
    fi
    
    log_success "系統先決條件檢查通過"
}

# 檢查環境檔案
check_env_file() {
    log_info "檢查環境配置檔案..."
    
    if [[ ! -f "$ENV_FILE" ]]; then
        log_warning "環境檔案 $ENV_FILE 不存在"
        
        if [[ -f "$PROJECT_ROOT/.env.monitoring" ]]; then
            log_info "使用 .env.monitoring 作為環境檔案"
            ENV_FILE="$PROJECT_ROOT/.env.monitoring"
        else
            log_warning "創建預設環境檔案"
            cp "$PROJECT_ROOT/.env.monitoring" "$ENV_FILE"
        fi
    fi
    
    log_success "環境配置檔案檢查完成"
}

# 創建必要目錄
create_directories() {
    log_info "創建必要的目錄結構..."
    
    local dirs=(
        "$PROJECT_ROOT/logs"
        "$PROJECT_ROOT/logs/errors"
        "$PROJECT_ROOT/logs/performance"
        "$PROJECT_ROOT/logs/structured"
        "$PROJECT_ROOT/monitoring/grafana/dashboards"
        "$PROJECT_ROOT/monitoring/grafana/provisioning/dashboards"
        "$PROJECT_ROOT/monitoring/grafana/provisioning/datasources"
        "$PROJECT_ROOT/monitoring/rules"
    )
    
    for dir in "${dirs[@]}"; do
        if [[ ! -d "$dir" ]]; then
            mkdir -p "$dir"
            log_info "創建目錄: $dir"
        fi
    done
    
    # 設定 Grafana 目錄權限
    if [[ -d "$PROJECT_ROOT/monitoring/grafana" ]]; then
        chmod -R 755 "$PROJECT_ROOT/monitoring/grafana"
    fi
    
    log_success "目錄結構創建完成"
}

# 檢查服務狀態
check_services() {
    log_info "檢查現有服務狀態..."
    
    local running_services
    running_services=$(docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" ps --services --filter "status=running" 2>/dev/null || true)
    
    if [[ -n "$running_services" ]]; then
        log_warning "發現正在運行的服務:"
        echo "$running_services"
        
        read -p "是否要重新啟動服務? (y/N): " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            log_info "停止現有服務..."
            docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" down
        else
            log_info "取消啟動"
            exit 0
        fi
    fi
}

# 啟動基礎服務
start_base_services() {
    log_info "啟動基礎服務 (PostgreSQL, Redis, Backend, Frontend, Nginx)..."
    
    docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" up -d \
        adhd-postgres \
        adhd-redis \
        adhd-backend \
        adhd-frontend \
        adhd-nginx
    
    log_info "等待基礎服務啟動..."
    sleep 10
    
    # 檢查基礎服務健康狀態
    local max_attempts=30
    local attempt=1
    
    while [[ $attempt -le $max_attempts ]]; do
        local healthy_services=0
        local total_services=5
        
        # 檢查各服務狀態
        for service in adhd-postgres adhd-redis adhd-backend adhd-frontend adhd-nginx; do
            if docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" ps "$service" 2>/dev/null | grep -q "healthy\|Up"; then
                ((healthy_services++))
            fi
        done
        
        if [[ $healthy_services -eq $total_services ]]; then
            log_success "基礎服務啟動完成"
            return 0
        fi
        
        log_info "等待服務啟動... ($attempt/$max_attempts) - 健康服務: $healthy_services/$total_services"
        sleep 5
        ((attempt++))
    done
    
    log_warning "部分基礎服務可能未完全啟動，繼續啟動監控服務"
}

# 啟動監控服務
start_monitoring_services() {
    log_info "啟動監控服務 (Prometheus, Grafana, AlertManager, Exporters)..."
    
    docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" \
        --profile monitoring up -d
    
    log_info "等待監控服務啟動..."
    sleep 15
    
    # 檢查監控服務健康狀態
    local monitoring_services=(
        "adhd-prometheus"
        "adhd-grafana"
        "adhd-alertmanager"
        "adhd-postgres-exporter"
        "adhd-redis-exporter"
        "adhd-node-exporter"
        "adhd-cadvisor"
    )
    
    local max_attempts=30
    local attempt=1
    
    while [[ $attempt -le $max_attempts ]]; do
        local healthy_services=0
        local total_services=${#monitoring_services[@]}
        
        for service in "${monitoring_services[@]}"; do
            if docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" ps "$service" 2>/dev/null | grep -q "healthy\|Up"; then
                ((healthy_services++))
            fi
        done
        
        if [[ $healthy_services -eq $total_services ]]; then
            log_success "監控服務啟動完成"
            return 0
        fi
        
        log_info "等待監控服務啟動... ($attempt/$max_attempts) - 健康服務: $healthy_services/$total_services"
        sleep 5
        ((attempt++))
    done
    
    log_warning "部分監控服務可能未完全啟動"
}

# 顯示服務資訊
show_service_info() {
    log_info "服務資訊:"
    echo
    echo "=== 主要服務 ==="
    echo "🌐 主應用程式:      http://localhost"
    echo "🏥 健康檢查:        http://localhost/health"
    echo "📊 API 文檔:        http://localhost/swagger"
    echo
    echo "=== 監控服務 ==="
    echo "📈 Prometheus:     http://localhost:9090"
    echo "📊 Grafana:        http://localhost:3001 (admin/admin123)"
    echo "🚨 AlertManager:   http://localhost:9093"
    echo "📉 Metrics:        http://localhost/metrics"
    echo
    echo "=== 管理工具 ==="
    echo "🗄️  pgAdmin:        http://localhost:5050 (需要 --profile admin)"
    echo
    echo "=== 日誌檔案 ==="
    echo "📝 主要日誌:        $PROJECT_ROOT/logs/"
    echo "❌ 錯誤日誌:        $PROJECT_ROOT/logs/errors/"
    echo "⚡ 效能日誌:        $PROJECT_ROOT/logs/performance/"
    echo "📋 結構化日誌:      $PROJECT_ROOT/logs/structured/"
    echo
}

# 顯示監控指令
show_monitoring_commands() {
    echo "=== 監控相關指令 ==="
    echo "📊 查看服務狀態:    docker-compose --env-file $ENV_FILE ps"
    echo "📋 查看日誌:        docker-compose --env-file $ENV_FILE logs -f [service_name]"
    echo "🛑 停止所有服務:    docker-compose --env-file $ENV_FILE down"
    echo "🔄 重啟監控服務:    docker-compose --env-file $ENV_FILE --profile monitoring restart"
    echo "🧹 清理資源:        docker-compose --env-file $ENV_FILE down -v"
    echo
}

# 主要執行流程
main() {
    echo "========================================"
    echo "ADHD 生產力系統 - 監控系統啟動腳本"
    echo "========================================"
    echo
    
    check_prerequisites
    check_env_file
    create_directories
    check_services
    
    log_info "開始啟動 ADHD 生產力系統..."
    
    start_base_services
    start_monitoring_services
    
    echo
    log_success "🎉 ADHD 生產力系統監控環境啟動完成!"
    echo
    
    show_service_info
    show_monitoring_commands
    
    log_info "系統正在運行中... 使用 Ctrl+C 可查看即時日誌"
    
    # 可選：跟蹤主要服務日誌
    read -p "是否要查看即時日誌? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" logs -f
    fi
}

# 執行主函數
main "$@"