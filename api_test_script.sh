#!/bin/bash

# API 端點測試腳本
# 用於驗證 ADHD Productivity System API 的基本功能

BASE_URL="http://localhost:5000"
API_URL="$BASE_URL/api"

echo "=== ADHD Productivity System API 端點測試 ==="
echo "測試目標: $API_URL"
echo "日期: $(date)"
echo ""

# 顏色定義
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 測試函數
test_endpoint() {
    local method=$1
    local endpoint=$2
    local expected_status=$3
    local description=$4
    local data=$5
    local auth_header=$6
    
    echo -n "測試: $description ... "
    
    if [ ! -z "$data" ]; then
        if [ ! -z "$auth_header" ]; then
            response=$(curl -s -w "%{http_code}" -X $method \
                -H "Content-Type: application/json" \
                -H "Authorization: $auth_header" \
                -d "$data" \
                "$API_URL$endpoint" \
                -o /tmp/api_response.json)
        else
            response=$(curl -s -w "%{http_code}" -X $method \
                -H "Content-Type: application/json" \
                -d "$data" \
                "$API_URL$endpoint" \
                -o /tmp/api_response.json)
        fi
    else
        if [ ! -z "$auth_header" ]; then
            response=$(curl -s -w "%{http_code}" -X $method \
                -H "Authorization: $auth_header" \
                "$API_URL$endpoint" \
                -o /tmp/api_response.json)
        else
            response=$(curl -s -w "%{http_code}" -X $method \
                "$API_URL$endpoint" \
                -o /tmp/api_response.json)
        fi
    fi
    
    if [ "$response" = "$expected_status" ]; then
        echo -e "${GREEN}通過${NC} (狀態碼: $response)"
    else
        echo -e "${RED}失敗${NC} (期望: $expected_status, 實際: $response)"
        if [ -f /tmp/api_response.json ]; then
            echo "回應內容: $(cat /tmp/api_response.json)"
        fi
    fi
}

# 檢查 API 服務是否運行
echo "1. 檢查 API 服務狀態..."
if curl -s "$BASE_URL/health" > /dev/null 2>&1; then
    echo -e "${GREEN}API 服務正在運行${NC}"
else
    echo -e "${RED}API 服務未運行或無法連接${NC}"
    echo "請確保 API 服務在 $BASE_URL 上運行"
    exit 1
fi

echo ""
echo "2. API 端點功能測試"
echo "=================================="

# AuthController 測試
echo ""
echo "### AuthController 測試 ###"

# 測試使用者註冊
test_endpoint "POST" "/auth/register" "201" "使用者註冊" '{
    "name": "測試使用者",
    "email": "test@example.com",
    "password": "Test123!@#",
    "adhdType": "Combined",
    "timeZone": "Asia/Taipei"
}'

# 測試使用者登入
test_endpoint "POST" "/auth/login" "200" "使用者登入" '{
    "email": "test@example.com",
    "password": "Test123!@#"
}'

# 如果登入成功，取得 token
if [ -f /tmp/api_response.json ]; then
    ACCESS_TOKEN=$(cat /tmp/api_response.json | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)
    if [ ! -z "$ACCESS_TOKEN" ]; then
        echo "取得 Access Token: ${ACCESS_TOKEN:0:20}..."
        AUTH_HEADER="Bearer $ACCESS_TOKEN"
    fi
fi

# 測試取得當前使用者資訊
if [ ! -z "$AUTH_HEADER" ]; then
    test_endpoint "GET" "/auth/me" "200" "取得當前使用者資訊" "" "$AUTH_HEADER"
fi

# TasksController 測試
echo ""
echo "### TasksController 測試 ###"

if [ ! -z "$AUTH_HEADER" ]; then
    # 測試創建任務
    test_endpoint "POST" "/tasks" "201" "創建新任務" '{
        "title": "測試任務",
        "description": "這是一個測試任務",
        "priority": "Medium",
        "estimatedMinutes": 60,
        "tags": "測試,API"
    }' "$AUTH_HEADER"
    
    # 如果創建成功，取得任務 ID
    if [ -f /tmp/api_response.json ]; then
        TASK_ID=$(cat /tmp/api_response.json | grep -o '"id":"[^"]*' | cut -d'"' -f4)
        if [ ! -z "$TASK_ID" ]; then
            echo "創建的任務 ID: $TASK_ID"
        fi
    fi
    
    # 測試取得任務列表
    test_endpoint "GET" "/tasks" "200" "取得任務列表" "" "$AUTH_HEADER"
    
    # 測試取得特定任務
    if [ ! -z "$TASK_ID" ]; then
        test_endpoint "GET" "/tasks/$TASK_ID" "200" "根據ID取得任務" "" "$AUTH_HEADER"
    fi
    
    # 測試更新任務狀態
    if [ ! -z "$TASK_ID" ]; then
        test_endpoint "PATCH" "/tasks/$TASK_ID/status" "200" "更新任務狀態" '"InProgress"' "$AUTH_HEADER"
    fi
else
    echo -e "${YELLOW}跳過 TasksController 測試 - 未取得有效的授權 token${NC}"
fi

# 測試未授權存取
echo ""
echo "### 權限驗證測試 ###"
test_endpoint "GET" "/tasks" "401" "未授權存取任務列表"
test_endpoint "GET" "/auth/me" "401" "未授權存取使用者資訊"

echo ""
echo "3. 錯誤處理測試"
echo "=================================="

# 測試無效的註冊資料
test_endpoint "POST" "/auth/register" "400" "無效註冊資料 - 弱密碼" '{
    "name": "測試使用者",
    "email": "test2@example.com",
    "password": "123",
    "adhdType": "Combined"
}'

# 測試無效的登入資料
test_endpoint "POST" "/auth/login" "401" "無效登入資料" '{
    "email": "nonexistent@example.com",
    "password": "wrongpassword"
}'

# 測試無效的任務 ID
if [ ! -z "$AUTH_HEADER" ]; then
    test_endpoint "GET" "/tasks/invalid-guid" "400" "無效的任務ID格式" "" "$AUTH_HEADER"
    test_endpoint "GET" "/tasks/00000000-0000-0000-0000-000000000000" "404" "不存在的任務" "" "$AUTH_HEADER"
fi

echo ""
echo "4. 效能測試"
echo "=================================="

if [ ! -z "$AUTH_HEADER" ]; then
    echo "測試 GetTaskById 快取效果..."
    
    # 第一次請求 (冷啟動)
    start_time=$(date +%s%3N)
    test_endpoint "GET" "/tasks" "200" "任務列表請求 #1 (冷啟動)" "" "$AUTH_HEADER"
    end_time=$(date +%s%3N)
    cold_time=$((end_time - start_time))
    
    # 第二次請求 (可能命中快取)
    start_time=$(date +%s%3N)
    test_endpoint "GET" "/tasks" "200" "任務列表請求 #2 (快取)" "" "$AUTH_HEADER"
    end_time=$(date +%s%3N)
    cached_time=$((end_time - start_time))
    
    echo "冷啟動時間: ${cold_time}ms"
    echo "快取時間: ${cached_time}ms"
    
    if [ $cached_time -lt $cold_time ]; then
        echo -e "${GREEN}快取效果顯著${NC}"
    else
        echo -e "${YELLOW}快取效果不明顯或網路延遲影響${NC}"
    fi
fi

echo ""
echo "5. API 文檔測試"
echo "=================================="

# 測試 Swagger/OpenAPI 文檔
test_endpoint "GET" "/swagger/v1/swagger.json" "200" "OpenAPI 規格文檔"

echo ""
echo "=== 測試完成 ==="
echo "完成時間: $(date)"

# 清理臨時檔案
rm -f /tmp/api_response.json

echo ""
echo "注意: 此測試需要 API 服務在 $BASE_URL 上運行"
echo "如果所有測試都失敗，請檢查:"
echo "1. API 服務是否正在運行"
echo "2. 資料庫連接是否正常"
echo "3. 防火牆或網路設定"