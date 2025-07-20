#!/bin/bash

# ADHD 生產力系統 - 安全標頭測試腳本
# 驗證和測試 HTTP 安全標頭配置

set -e

# 顏色定義
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m'

# 配置
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
TEST_URL="${TEST_URL:-http://localhost}"
HTTPS_TEST_URL="${HTTPS_TEST_URL:-https://localhost}"
REPORT_FILE="${PROJECT_ROOT}/security/reports/security_headers_test_$(date +%Y%m%d_%H%M%S).md"

# 創建報告目錄
mkdir -p "$(dirname "$REPORT_FILE")"

# 日誌函數
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[PASS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[FAIL]${NC} $1"
}

log_test() {
    echo -e "${PURPLE}[TEST]${NC} $1"
}

# 顯示橫幅
show_banner() {
    echo -e "${CYAN}"
    echo "╔══════════════════════════════════════════════════════════╗"
    echo "║           ADHD 生產力系統 安全標頭測試工具              ║"
    echo "║                                                          ║"
    echo "║  🔍 安全標頭驗證 | 📊 合規性檢查 | 📋 詳細報告          ║"
    echo "╚══════════════════════════════════════════════════════════╝"
    echo -e "${NC}"
}

# 檢查依賴
check_dependencies() {
    local deps=("curl" "jq")
    local missing=()
    
    for dep in "${deps[@]}"; do
        if ! command -v "$dep" &> /dev/null; then
            missing+=("$dep")
        fi
    done
    
    if [ ${#missing[@]} -ne 0 ]; then
        log_error "缺少必要的依賴: ${missing[*]}"
        echo "請安裝缺少的依賴後再重試"
        exit 1
    fi
}

# 測試基礎安全標頭
test_basic_security_headers() {
    log_test "測試基礎安全標頭..."
    
    local url="$1"
    local response_headers
    
    # 獲取響應標頭
    response_headers=$(curl -s -I "$url" 2>/dev/null || echo "")
    
    if [ -z "$response_headers" ]; then
        log_error "無法連接到 $url"
        return 1
    fi
    
    echo "## 基礎安全標頭測試" >> "$REPORT_FILE"
    echo "" >> "$REPORT_FILE"
    echo "測試 URL: $url" >> "$REPORT_FILE"
    echo "測試時間: $(date)" >> "$REPORT_FILE"
    echo "" >> "$REPORT_FILE"
    
    # 測試各個安全標頭
    test_header "$response_headers" "X-Content-Type-Options" "nosniff" "防止 MIME 類型混淆"
    test_header "$response_headers" "X-Frame-Options" "(DENY|SAMEORIGIN)" "防止點擊劫持"
    test_header "$response_headers" "X-XSS-Protection" "1.*mode=block" "XSS 保護"
    test_header "$response_headers" "Referrer-Policy" ".*" "Referrer 策略"
    test_header "$response_headers" "Content-Security-Policy" ".*" "內容安全策略"
    test_header "$response_headers" "Permissions-Policy" ".*" "權限策略"
    
    # 檢查不應該存在的標頭
    test_header_absent "$response_headers" "Server" "隱藏服務器信息"
    test_header_absent "$response_headers" "X-Powered-By" "隱藏技術棧信息"
    test_header_absent "$response_headers" "X-AspNet-Version" "隱藏 ASP.NET 版本"
}

# 測試 HTTPS 相關標頭
test_https_headers() {
    log_test "測試 HTTPS 安全標頭..."
    
    local url="$1"
    local response_headers
    
    if [[ "$url" != https://* ]]; then
        log_warning "跳過 HTTPS 標頭測試（非 HTTPS URL）"
        return 0
    fi
    
    response_headers=$(curl -s -I "$url" --insecure 2>/dev/null || echo "")
    
    if [ -z "$response_headers" ]; then
        log_error "無法連接到 $url"
        return 1
    fi
    
    echo "" >> "$REPORT_FILE"
    echo "## HTTPS 安全標頭測試" >> "$REPORT_FILE"
    echo "" >> "$REPORT_FILE"
    
    test_header "$response_headers" "Strict-Transport-Security" "max-age=.*" "HSTS 強制 HTTPS"
    test_header "$response_headers" "Expect-CT" ".*" "Certificate Transparency"
}

# 測試單個標頭
test_header() {
    local headers="$1"
    local header_name="$2"
    local expected_pattern="$3"
    local description="$4"
    
    local header_value
    header_value=$(echo "$headers" | grep -i "^$header_name:" | sed 's/^[^:]*: *//' | tr -d '\r')
    
    if [ -n "$header_value" ]; then
        if echo "$header_value" | grep -qE "$expected_pattern"; then
            log_success "$header_name: $header_value"
            echo "- ✅ **$header_name**: $header_value ($description)" >> "$REPORT_FILE"
        else
            log_warning "$header_name: $header_value (不符合預期模式: $expected_pattern)"
            echo "- ⚠️ **$header_name**: $header_value (不符合預期模式)" >> "$REPORT_FILE"
        fi
    else
        log_error "$header_name: 缺失"
        echo "- ❌ **$header_name**: 缺失 ($description)" >> "$REPORT_FILE"
    fi
}

# 測試標頭不存在
test_header_absent() {
    local headers="$1"
    local header_name="$2"
    local description="$3"
    
    local header_value
    header_value=$(echo "$headers" | grep -i "^$header_name:" | sed 's/^[^:]*: *//' | tr -d '\r')
    
    if [ -z "$header_value" ]; then
        log_success "$header_name: 正確隱藏"
        echo "- ✅ **$header_name**: 正確隱藏 ($description)" >> "$REPORT_FILE"
    else
        log_warning "$header_name: $header_value (應該隱藏)"
        echo "- ⚠️ **$header_name**: $header_value (應該隱藏)" >> "$REPORT_FILE"
    fi
}

# 測試 CSP 策略
test_csp_policy() {
    log_test "分析 CSP 策略..."
    
    local url="$1"
    local csp_header
    
    csp_header=$(curl -s -I "$url" 2>/dev/null | grep -i "Content-Security-Policy:" | sed 's/^[^:]*: *//' | tr -d '\r')
    
    if [ -z "$csp_header" ]; then
        log_error "CSP 標頭缺失"
        echo "" >> "$REPORT_FILE"
        echo "## CSP 策略分析" >> "$REPORT_FILE"
        echo "" >> "$REPORT_FILE"
        echo "❌ **CSP 標頭缺失**" >> "$REPORT_FILE"
        return 1
    fi
    
    echo "" >> "$REPORT_FILE"
    echo "## CSP 策略分析" >> "$REPORT_FILE"
    echo "" >> "$REPORT_FILE"
    echo "**完整 CSP 策略**:" >> "$REPORT_FILE"
    echo "\`\`\`" >> "$REPORT_FILE"
    echo "$csp_header" >> "$REPORT_FILE"
    echo "\`\`\`" >> "$REPORT_FILE"
    echo "" >> "$REPORT_FILE"
    
    # 分析 CSP 指令
    analyze_csp_directive "$csp_header" "default-src" "預設來源"
    analyze_csp_directive "$csp_header" "script-src" "腳本來源"
    analyze_csp_directive "$csp_header" "style-src" "樣式來源"
    analyze_csp_directive "$csp_header" "img-src" "圖片來源"
    analyze_csp_directive "$csp_header" "connect-src" "連接來源"
    analyze_csp_directive "$csp_header" "font-src" "字體來源"
    analyze_csp_directive "$csp_header" "object-src" "物件來源"
    analyze_csp_directive "$csp_header" "frame-ancestors" "框架祖先"
    
    # 檢查危險配置
    check_dangerous_csp_configs "$csp_header"
}

# 分析 CSP 指令
analyze_csp_directive() {
    local csp_policy="$1"
    local directive="$2"
    local description="$3"
    
    local directive_value
    directive_value=$(echo "$csp_policy" | grep -oE "$directive [^;]*" | sed "s/$directive //")
    
    if [ -n "$directive_value" ]; then
        echo "- **$directive**: $directive_value ($description)" >> "$REPORT_FILE"
        log_info "$directive: $directive_value"
    else
        echo "- **$directive**: 未設置 ($description)" >> "$REPORT_FILE"
        log_warning "$directive: 未設置"
    fi
}

# 檢查危險的 CSP 配置
check_dangerous_csp_configs() {
    local csp_policy="$1"
    
    echo "" >> "$REPORT_FILE"
    echo "### CSP 安全性評估" >> "$REPORT_FILE"
    echo "" >> "$REPORT_FILE"
    
    # 檢查 unsafe-eval
    if echo "$csp_policy" | grep -q "unsafe-eval"; then
        log_warning "CSP 包含 'unsafe-eval'，可能存在安全風險"
        echo "- ⚠️ **警告**: 包含 'unsafe-eval'，可能存在 XSS 風險" >> "$REPORT_FILE"
    else
        log_success "CSP 不包含 'unsafe-eval'"
        echo "- ✅ **良好**: 不包含 'unsafe-eval'" >> "$REPORT_FILE"
    fi
    
    # 檢查 unsafe-inline
    if echo "$csp_policy" | grep -q "unsafe-inline"; then
        log_warning "CSP 包含 'unsafe-inline'，可能存在安全風險"
        echo "- ⚠️ **警告**: 包含 'unsafe-inline'，建議使用 nonce 或 hash" >> "$REPORT_FILE"
    else
        log_success "CSP 不包含 'unsafe-inline'"
        echo "- ✅ **良好**: 不包含 'unsafe-inline'" >> "$REPORT_FILE"
    fi
    
    # 檢查 wildcard
    if echo "$csp_policy" | grep -qE "\s\*(\s|$|;)"; then
        log_warning "CSP 包含通配符 '*'，安全性較弱"
        echo "- ⚠️ **警告**: 包含通配符 '*'，建議限制來源" >> "$REPORT_FILE"
    else
        log_success "CSP 不包含危險的通配符"
        echo "- ✅ **良好**: 不包含危險的通配符" >> "$REPORT_FILE"
    fi
}

# 測試 API 端點的安全標頭
test_api_security_headers() {
    log_test "測試 API 端點安全標頭..."
    
    local base_url="$1"
    local api_endpoints=("/api/health" "/api/auth/me")
    
    echo "" >> "$REPORT_FILE"
    echo "## API 端點安全標頭測試" >> "$REPORT_FILE"
    echo "" >> "$REPORT_FILE"
    
    for endpoint in "${api_endpoints[@]}"; do
        local full_url="$base_url$endpoint"
        echo "### 測試端點: $endpoint" >> "$REPORT_FILE"
        echo "" >> "$REPORT_FILE"
        
        local response_headers
        response_headers=$(curl -s -I "$full_url" 2>/dev/null || echo "")
        
        if [ -n "$response_headers" ]; then
            # API 特定的標頭檢查
            test_header "$response_headers" "Cache-Control" "(no-store|no-cache)" "API 快取控制"
            test_header "$response_headers" "X-Content-Type-Options" "nosniff" "MIME 類型保護"
        else
            echo "- ❌ **錯誤**: 無法連接到 $endpoint" >> "$REPORT_FILE"
            log_error "無法連接到 $endpoint"
        fi
        
        echo "" >> "$REPORT_FILE"
    done
}

# 生成安全評分
generate_security_score() {
    log_test "計算安全評分..."
    
    local total_score=0
    local max_score=100
    
    # 從報告文件計算分數
    local pass_count
    local fail_count
    local warn_count
    
    pass_count=$(grep -c "✅" "$REPORT_FILE" 2>/dev/null || echo "0")
    fail_count=$(grep -c "❌" "$REPORT_FILE" 2>/dev/null || echo "0")
    warn_count=$(grep -c "⚠️" "$REPORT_FILE" 2>/dev/null || echo "0")
    
    # 計算評分
    total_score=$(( (pass_count * 10) - (fail_count * 5) - (warn_count * 2) ))
    
    # 確保評分在 0-100 範圍內
    if [ "$total_score" -lt 0 ]; then
        total_score=0
    elif [ "$total_score" -gt "$max_score" ]; then
        total_score=$max_score
    fi
    
    # 確定等級
    local grade
    if [ "$total_score" -ge 90 ]; then
        grade="A+"
    elif [ "$total_score" -ge 80 ]; then
        grade="A"
    elif [ "$total_score" -ge 70 ]; then
        grade="B"
    elif [ "$total_score" -ge 60 ]; then
        grade="C"
    else
        grade="D"
    fi
    
    echo "" >> "$REPORT_FILE"
    echo "## 安全評分" >> "$REPORT_FILE"
    echo "" >> "$REPORT_FILE"
    echo "**總分**: $total_score / $max_score (**等級**: $grade)" >> "$REPORT_FILE"
    echo "" >> "$REPORT_FILE"
    echo "- ✅ 通過檢查: $pass_count" >> "$REPORT_FILE"
    echo "- ⚠️ 警告項目: $warn_count" >> "$REPORT_FILE"
    echo "- ❌ 失敗項目: $fail_count" >> "$REPORT_FILE"
    
    log_info "安全評分: $total_score/$max_score (等級: $grade)"
    
    # 根據評分給出建議
    echo "" >> "$REPORT_FILE"
    echo "## 改進建議" >> "$REPORT_FILE"
    echo "" >> "$REPORT_FILE"
    
    if [ "$total_score" -lt 70 ]; then
        echo "- 🚨 **緊急**: 安全配置需要立即改進" >> "$REPORT_FILE"
        echo "- 建議檢查並修復所有失敗的安全標頭" >> "$REPORT_FILE"
    elif [ "$total_score" -lt 85 ]; then
        echo "- ⚠️ **建議**: 安全配置有改進空間" >> "$REPORT_FILE"
        echo "- 建議解決警告項目以提高安全性" >> "$REPORT_FILE"
    else
        echo "- ✅ **良好**: 安全配置符合最佳實踐" >> "$REPORT_FILE"
        echo "- 建議定期檢查和更新安全配置" >> "$REPORT_FILE"
    fi
    
    echo "" >> "$REPORT_FILE"
    echo "---" >> "$REPORT_FILE"
    echo "*報告生成時間: $(date)*" >> "$REPORT_FILE"
    echo "*測試工具: ADHD 生產力系統安全標頭測試工具*" >> "$REPORT_FILE"
}

# 顯示幫助
show_help() {
    echo "ADHD 生產力系統 - 安全標頭測試工具"
    echo ""
    echo "使用方法:"
    echo "  $0 [選項]"
    echo ""
    echo "選項:"
    echo "  -u, --url URL       測試指定的 URL (預設: http://localhost)"
    echo "  -s, --https URL     測試指定的 HTTPS URL (預設: https://localhost)"
    echo "  -o, --output FILE   指定報告輸出文件"
    echo "  -h, --help          顯示此幫助信息"
    echo ""
    echo "範例:"
    echo "  # 測試本地開發環境"
    echo "  $0"
    echo ""
    echo "  # 測試指定 URL"
    echo "  $0 -u http://example.com -s https://example.com"
    echo ""
    echo "  # 指定報告輸出文件"
    echo "  $0 -o /tmp/security_report.md"
}

# 主函數
main() {
    show_banner
    check_dependencies
    
    # 解析命令行參數
    while [[ $# -gt 0 ]]; do
        case $1 in
            -u|--url)
                TEST_URL="$2"
                shift 2
                ;;
            -s|--https)
                HTTPS_TEST_URL="$2"
                shift 2
                ;;
            -o|--output)
                REPORT_FILE="$2"
                shift 2
                ;;
            -h|--help)
                show_help
                exit 0
                ;;
            *)
                log_error "未知選項: $1"
                show_help
                exit 1
                ;;
        esac
    done
    
    log_info "開始安全標頭測試..."
    log_info "HTTP URL: $TEST_URL"
    log_info "HTTPS URL: $HTTPS_TEST_URL"
    log_info "報告文件: $REPORT_FILE"
    
    # 初始化報告文件
    cat > "$REPORT_FILE" << EOF
# ADHD 生產力系統 - 安全標頭測試報告

**測試時間**: $(date)  
**測試工具**: 安全標頭測試腳本 v1.0  
**HTTP URL**: $TEST_URL  
**HTTPS URL**: $HTTPS_TEST_URL  

EOF
    
    # 執行測試
    test_basic_security_headers "$TEST_URL"
    test_https_headers "$HTTPS_TEST_URL"
    test_csp_policy "$TEST_URL"
    test_api_security_headers "$TEST_URL"
    generate_security_score
    
    log_success "安全標頭測試完成"
    log_info "詳細報告: $REPORT_FILE"
    
    # 顯示簡要結果
    echo ""
    echo "=== 測試摘要 ==="
    grep -E "(✅|❌|⚠️)" "$REPORT_FILE" | head -10
    echo "..."
    echo ""
    echo "完整報告請查看: $REPORT_FILE"
}

main "$@"