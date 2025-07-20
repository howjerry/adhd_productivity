#!/bin/bash

# ADHD 生產力系統 - 開發環境啟動驗證腳本
# 此腳本驗證開發環境是否正確設置並可以正常運行

set -e  # 任何命令失敗時退出

echo "========================================"
echo "ADHD 生產力系統 - 開發環境驗證"
echo "========================================"

# 顏色定義
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 函數：打印狀態
print_status() {
    local status=$1
    local message=$2
    case $status in
        "OK")
            echo -e "[${GREEN}✓${NC}] $message"
            ;;
        "WARN")
            echo -e "[${YELLOW}⚠${NC}] $message"
            ;;
        "ERROR")
            echo -e "[${RED}✗${NC}] $message"
            ;;
        "INFO")
            echo -e "[${BLUE}ℹ${NC}] $message"
            ;;
    esac
}

# 檢查必要工具
print_status "INFO" "檢查開發環境必要工具..."

check_tool() {
    if command -v $1 &> /dev/null; then
        print_status "OK" "$1 已安裝"
        return 0
    else
        print_status "ERROR" "$1 未安裝"
        return 1
    fi
}

# 工具檢查
TOOLS_OK=true
check_tool "docker" || TOOLS_OK=false
check_tool "docker-compose" || TOOLS_OK=false
check_tool "dotnet" || TOOLS_OK=false
check_tool "curl" || TOOLS_OK=false

if [ "$TOOLS_OK" = false ]; then
    print_status "ERROR" "缺少必要工具，請安裝後重試"
    exit 1
fi

# 檢查 .env 檔案
print_status "INFO" "檢查環境配置..."
if [ -f ".env" ]; then
    print_status "OK" ".env 檔案存在"
    
    # 檢查重要環境變數
    source .env
    if [ -n "$POSTGRES_PASSWORD" ]; then
        print_status "OK" "POSTGRES_PASSWORD 已設定"
    else
        print_status "WARN" "POSTGRES_PASSWORD 未設定"
    fi
    
    if [ -n "$JWT_SECRET_KEY" ]; then
        print_status "OK" "JWT_SECRET_KEY 已設定"
    else
        print_status "WARN" "JWT_SECRET_KEY 未設定"
    fi
else
    print_status "ERROR" ".env 檔案不存在"
    exit 1
fi

# 檢查 Docker 服務
print_status "INFO" "檢查 Docker 服務狀態..."

docker_ps_output=$(docker-compose ps --format "table {{.Name}}\t{{.State}}" 2>/dev/null || echo "")

check_service() {
    local service_name=$1
    if echo "$docker_ps_output" | grep -q "$service_name.*Up"; then
        print_status "OK" "$service_name 服務運行中"
        return 0
    elif echo "$docker_ps_output" | grep -q "$service_name"; then
        print_status "WARN" "$service_name 服務存在但未運行"
        return 1
    else
        print_status "WARN" "$service_name 服務未啟動"
        return 1
    fi
}

# 啟動基礎服務
print_status "INFO" "啟動資料庫和快取服務..."
docker-compose up -d adhd-postgres adhd-redis

# 等待服務啟動
print_status "INFO" "等待服務啟動..."
sleep 5

# 檢查服務健康狀態
check_service "adhd-postgres"
check_service "adhd-redis"

# 檢查資料庫連線
print_status "INFO" "測試資料庫連線..."
if docker exec adhd-postgres pg_isready -U adhd_user -d adhd_productivity &> /dev/null; then
    print_status "OK" "PostgreSQL 資料庫可連線"
else
    print_status "ERROR" "PostgreSQL 資料庫連線失敗"
fi

# 檢查 Redis 連線
print_status "INFO" "測試 Redis 連線..."
if docker exec adhd-redis redis-cli ping &> /dev/null; then
    print_status "OK" "Redis 快取可連線"
else
    print_status "ERROR" "Redis 快取連線失敗"
fi

# 測試後端編譯
print_status "INFO" "測試後端專案編譯..."
cd backend/src/AdhdProductivitySystem.Api
if dotnet build --verbosity quiet &> /dev/null; then
    print_status "OK" "後端專案編譯成功"
else
    print_status "ERROR" "後端專案編譯失敗"
    cd ../../..
    exit 1
fi
cd ../../..

# 測試後端啟動（簡短測試）
print_status "INFO" "測試後端服務啟動..."
cd backend/src/AdhdProductivitySystem.Api

# 設定環境變數並啟動服務
export $(grep -v '^#' ../../../.env | xargs)
timeout 10 dotnet run --urls=http://localhost:5001 &> /tmp/backend_startup.log &
BACKEND_PID=$!

# 等待啟動
sleep 8

# 檢查進程是否還在運行
if kill -0 $BACKEND_PID 2>/dev/null; then
    print_status "OK" "後端服務成功啟動（PID: $BACKEND_PID）"
    
    # 嘗試連接（簡單檢查）
    if curl -f -s --max-time 5 http://localhost:5001/health &> /dev/null; then
        print_status "OK" "健康檢查端點可存取"
    else
        print_status "WARN" "健康檢查端點無回應（可能正在初始化）"
    fi
    
    # 停止測試服務
    kill $BACKEND_PID 2>/dev/null || true
    wait $BACKEND_PID 2>/dev/null || true
else
    print_status "ERROR" "後端服務啟動失敗"
    echo "啟動日誌："
    tail -20 /tmp/backend_startup.log
fi

cd ../../..

# 總結
echo ""
echo "========================================"
print_status "INFO" "開發環境驗證完成"
echo "========================================"

echo ""
echo "如要啟動完整開發環境，請執行："
echo "1. docker-compose up -d adhd-postgres adhd-redis"
echo "2. cd backend/src/AdhdProductivitySystem.Api"
echo "3. export \$(grep -v '^#' ../../../.env | xargs)"
echo "4. dotnet run --urls=http://localhost:5001"
echo ""
echo "API 文檔將可在 http://localhost:5001 存取"
echo ""

print_status "OK" "開發環境設置驗證完成！"