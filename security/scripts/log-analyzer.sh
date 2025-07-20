#!/bin/bash

# ADHD 生產力系統 - 日誌分析和安全監控腳本
# 分析 Nginx WAF 日誌並生成安全報告

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
LOG_DIR="/var/log/nginx"
REPORT_DIR="${PROJECT_ROOT}/security/reports"
ALERT_THRESHOLD=10
ALERT_EMAIL="${ALERT_EMAIL:-admin@adhd-productivity.dev}"

# 確保報告目錄存在
mkdir -p "$REPORT_DIR"

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

# 顯示橫幅
show_banner() {
    echo -e "${CYAN}"
    echo "╔══════════════════════════════════════════════════════════╗"
    echo "║             ADHD 生產力系統 安全日誌分析器              ║"
    echo "║                                                          ║"
    echo "║  📊 分析攻擊模式 | 🚨 生成安全警報 | 📈 統計報告        ║"
    echo "╚══════════════════════════════════════════════════════════╝"
    echo -e "${NC}"
}

# 檢查是否在 Docker 容器中運行
check_environment() {
    if [ -f /.dockerenv ]; then
        LOG_DIR="/var/log/nginx"
    elif docker ps | grep -q "adhd-nginx"; then
        # 如果是在主機上運行，通過 Docker 訪問日誌
        USE_DOCKER=true
    else
        log_warning "未檢測到 Docker 環境，使用本地日誌路徑"
    fi
}

# 從 Docker 容器複製日誌
copy_logs_from_docker() {
    if [ "$USE_DOCKER" = true ]; then
        log_info "從 Docker 容器複製日誌文件..."
        
        local temp_log_dir="/tmp/nginx_logs_$$"
        mkdir -p "$temp_log_dir"
        
        docker cp adhd-nginx:/var/log/nginx/. "$temp_log_dir/"
        LOG_DIR="$temp_log_dir"
    fi
}

# 分析攻擊統計
analyze_attacks() {
    local log_file="$1"
    local output_file="$2"
    
    log_info "分析攻擊統計..."
    
    cat > "$output_file" << 'EOF'
# ADHD 生產力系統 - 安全攻擊分析報告
# 生成時間: $(date)

## 攻擊類型統計

EOF
    
    # SQL 注入攻擊
    local sql_count=0
    if [ -f "$log_file" ]; then
        sql_count=$(grep -c "sql_injection=1" "$log_file" 2>/dev/null || echo "0")
    fi
    echo "- SQL 注入攻擊: $sql_count 次" >> "$output_file"
    
    # XSS 攻擊
    local xss_count=0
    if [ -f "$log_file" ]; then
        xss_count=$(grep -c "xss_attack=1" "$log_file" 2>/dev/null || echo "0")
    fi
    echo "- XSS 攻擊: $xss_count 次" >> "$output_file"
    
    # 路徑遍歷攻擊
    local path_count=0
    if [ -f "$log_file" ]; then
        path_count=$(grep -c "path_traversal=1" "$log_file" 2>/dev/null || echo "0")
    fi
    echo "- 路徑遍歷攻擊: $path_count 次" >> "$output_file"
    
    # 文件包含攻擊
    local file_count=0
    if [ -f "$log_file" ]; then
        file_count=$(grep -c "file_inclusion=1" "$log_file" 2>/dev/null || echo "0")
    fi
    echo "- 文件包含攻擊: $file_count 次" >> "$output_file"
    
    echo "" >> "$output_file"
    echo "## 威脅等級分布" >> "$output_file"
    
    if [ -f "$log_file" ]; then
        # 威脅等級統計
        for level in 1 2 3 4; do
            local threat_count
            threat_count=$(grep -c "threat_score=$level" "$log_file" 2>/dev/null || echo "0")
            echo "- 威脅等級 $level: $threat_count 次" >> "$output_file"
        done
    fi
    
    # 計算總攻擊次數
    local total_attacks=$((sql_count + xss_count + path_count + file_count))
    echo "" >> "$output_file"
    echo "**總攻擊次數: $total_attacks**" >> "$output_file"
    
    # 如果攻擊次數超過閾值，發送警報
    if [ "$total_attacks" -gt "$ALERT_THRESHOLD" ]; then
        send_alert "檢測到高頻攻擊" "在過去的時間段內檢測到 $total_attacks 次攻擊，超過警報閾值 $ALERT_THRESHOLD"
    fi
}

# 分析 IP 統計
analyze_ips() {
    local log_file="$1"
    local output_file="$2"
    
    log_info "分析 IP 統計..."
    
    echo "" >> "$output_file"
    echo "## 可疑 IP 地址統計" >> "$output_file"
    echo "" >> "$output_file"
    
    if [ -f "$log_file" ]; then
        # 提取前 10 個最活躍的惡意 IP
        echo "### 前 10 個惡意 IP 地址:" >> "$output_file"
        grep "threat_score=[1-9]" "$log_file" 2>/dev/null | \
        awk '{print $1}' | \
        sort | uniq -c | sort -nr | head -10 | \
        while read count ip; do
            echo "- $ip: $count 次" >> "$output_file"
        done
        
        # 被阻擋的 IP
        echo "" >> "$output_file"
        echo "### 被阻擋的 IP 地址:" >> "$output_file"
        grep "blocked_ip=1" "$log_file" 2>/dev/null | \
        awk '{print $1}' | \
        sort | uniq -c | sort -nr | head -10 | \
        while read count ip; do
            echo "- $ip: $count 次" >> "$output_file"
        done
    fi
}

# 分析 User-Agent 統計
analyze_user_agents() {
    local log_file="$1"
    local output_file="$2"
    
    log_info "分析 User-Agent 統計..."
    
    echo "" >> "$output_file"
    echo "## 可疑 User-Agent 統計" >> "$output_file"
    echo "" >> "$output_file"
    
    if [ -f "$log_file" ]; then
        echo "### 被阻擋的 User-Agent:" >> "$output_file"
        grep "blocked_ua=1" "$log_file" 2>/dev/null | \
        sed -n 's/.*"\([^"]*\)" blocked_ua=1.*/\1/p' | \
        sort | uniq -c | sort -nr | head -10 | \
        while read count ua; do
            echo "- $ua: $count 次" >> "$output_file"
        done
    fi
}

# 分析時間模式
analyze_time_patterns() {
    local log_file="$1"
    local output_file="$2"
    
    log_info "分析時間模式..."
    
    echo "" >> "$output_file"
    echo "## 攻擊時間模式分析" >> "$output_file"
    echo "" >> "$output_file"
    
    if [ -f "$log_file" ]; then
        echo "### 按小時統計的攻擊分布:" >> "$output_file"
        grep "threat_score=[1-9]" "$log_file" 2>/dev/null | \
        awk '{print $4}' | \
        sed 's/\[//g' | \
        cut -d: -f2 | \
        sort | uniq -c | sort -nr | \
        while read count hour; do
            echo "- ${hour}:00-${hour}:59: $count 次" >> "$output_file"
        done
    fi
}

# 生成建議
generate_recommendations() {
    local output_file="$1"
    
    echo "" >> "$output_file"
    echo "## 安全建議" >> "$output_file"
    echo "" >> "$output_file"
    echo "基於以上分析，建議採取以下安全措施:" >> "$output_file"
    echo "" >> "$output_file"
    echo "1. **IP 封鎖**: 對重複攻擊的 IP 地址實施長期封鎖" >> "$output_file"
    echo "2. **速率限制**: 加強對高頻攻擊時段的速率限制" >> "$output_file"
    echo "3. **WAF 規則**: 根據攻擊模式更新 WAF 規則" >> "$output_file"
    echo "4. **監控增強**: 對異常 User-Agent 增加監控" >> "$output_file"
    echo "5. **定期巡檢**: 每日檢查安全日誌和攻擊趨勢" >> "$output_file"
    echo "" >> "$output_file"
    echo "---" >> "$output_file"
    echo "*報告生成時間: $(date)*" >> "$output_file"
}

# 生成 JSON 格式報告
generate_json_report() {
    local log_file="$1"
    local output_file="$2"
    
    log_info "生成 JSON 格式報告..."
    
    local sql_count=0
    local xss_count=0
    local path_count=0
    local file_count=0
    
    if [ -f "$log_file" ]; then
        sql_count=$(grep -c "sql_injection=1" "$log_file" 2>/dev/null || echo "0")
        xss_count=$(grep -c "xss_attack=1" "$log_file" 2>/dev/null || echo "0")
        path_count=$(grep -c "path_traversal=1" "$log_file" 2>/dev/null || echo "0")
        file_count=$(grep -c "file_inclusion=1" "$log_file" 2>/dev/null || echo "0")
    fi
    
    cat > "$output_file" << EOF
{
  "report_date": "$(date -Iseconds)",
  "analysis_period": "last_24_hours",
  "attack_statistics": {
    "sql_injection": $sql_count,
    "xss_attack": $xss_count,
    "path_traversal": $path_count,
    "file_inclusion": $file_count,
    "total_attacks": $((sql_count + xss_count + path_count + file_count))
  },
  "threat_levels": {
EOF

    if [ -f "$log_file" ]; then
        for level in 1 2 3 4; do
            local threat_count
            threat_count=$(grep -c "threat_score=$level" "$log_file" 2>/dev/null || echo "0")
            echo "    \"level_$level\": $threat_count," >> "$output_file"
        done
    fi
    
    # 移除最後一個逗號
    sed -i '$ s/,$//' "$output_file"
    
    cat >> "$output_file" << EOF
  },
  "status": "completed",
  "recommendations": [
    "Review and block frequently attacking IPs",
    "Update WAF rules based on attack patterns",
    "Implement additional rate limiting during peak attack hours",
    "Monitor suspicious User-Agents"
  ]
}
EOF
}

# 發送警報
send_alert() {
    local subject="$1"
    local message="$2"
    
    log_warning "安全警報: $subject"
    
    # 寫入警報日誌
    echo "$(date): $subject - $message" >> "$REPORT_DIR/alerts.log"
    
    # 如果配置了郵件，發送郵件警報
    if command -v mail >/dev/null 2>&1 && [ -n "$ALERT_EMAIL" ]; then
        echo "$message" | mail -s "ADHD 安全警報: $subject" "$ALERT_EMAIL"
        log_info "警報郵件已發送到 $ALERT_EMAIL"
    fi
    
    # 如果在 Docker 環境中，嘗試發送 webhook 通知
    if [ -n "$WEBHOOK_URL" ]; then
        curl -X POST "$WEBHOOK_URL" \
             -H "Content-Type: application/json" \
             -d "{\"text\":\"🚨 ADHD 安全警報: $subject\\n$message\"}" \
             >/dev/null 2>&1 || true
    fi
}

# 實時監控模式
realtime_monitor() {
    log_info "啟動實時安全監控..."
    
    local security_log="$LOG_DIR/security.log"
    
    if [ ! -f "$security_log" ]; then
        log_error "安全日誌文件不存在: $security_log"
        return 1
    fi
    
    tail -f "$security_log" | while read line; do
        # 檢查高威脅評分
        if echo "$line" | grep -q "threat_score=[3-4]"; then
            local ip=$(echo "$line" | awk '{print $1}')
            log_warning "檢測到高威脅活動來自 IP: $ip"
            send_alert "高威脅活動檢測" "檢測到來自 $ip 的高威脅活動"
        fi
        
        # 檢查攻擊模式
        if echo "$line" | grep -q -E "(sql_injection=1|xss_attack=1)"; then
            local ip=$(echo "$line" | awk '{print $1}')
            local attack_type=""
            
            if echo "$line" | grep -q "sql_injection=1"; then
                attack_type="SQL 注入"
            elif echo "$line" | grep -q "xss_attack=1"; then
                attack_type="XSS 攻擊"
            fi
            
            log_warning "檢測到 $attack_type 攻擊來自 IP: $ip"
        fi
    done
}

# 清理舊報告
cleanup_old_reports() {
    log_info "清理超過 30 天的舊報告..."
    
    find "$REPORT_DIR" -name "*.md" -mtime +30 -delete 2>/dev/null || true
    find "$REPORT_DIR" -name "*.json" -mtime +30 -delete 2>/dev/null || true
    
    log_success "舊報告清理完成"
}

# 主分析函數
run_analysis() {
    local date_suffix=$(date +%Y%m%d_%H%M%S)
    local security_log="$LOG_DIR/security.log"
    local report_file="$REPORT_DIR/security_report_$date_suffix.md"
    local json_report="$REPORT_DIR/security_report_$date_suffix.json"
    
    if [ ! -f "$security_log" ]; then
        log_error "安全日誌文件不存在: $security_log"
        return 1
    fi
    
    log_info "開始分析安全日誌: $security_log"
    
    # 生成 Markdown 報告
    analyze_attacks "$security_log" "$report_file"
    analyze_ips "$security_log" "$report_file"
    analyze_user_agents "$security_log" "$report_file"
    analyze_time_patterns "$security_log" "$report_file"
    generate_recommendations "$report_file"
    
    # 生成 JSON 報告
    generate_json_report "$security_log" "$json_report"
    
    log_success "安全分析完成"
    echo "  Markdown 報告: $report_file"
    echo "  JSON 報告: $json_report"
    
    # 清理舊報告
    cleanup_old_reports
}

# 顯示幫助
show_help() {
    echo "ADHD 生產力系統 - 安全日誌分析器"
    echo ""
    echo "使用方法:"
    echo "  $0 [命令] [選項]"
    echo ""
    echo "命令:"
    echo "  analyze     分析安全日誌並生成報告"
    echo "  monitor     啟動實時安全監控"
    echo "  cleanup     清理舊報告文件"
    echo "  help        顯示此幫助信息"
    echo ""
    echo "環境變數:"
    echo "  ALERT_EMAIL     警報郵件地址"
    echo "  WEBHOOK_URL     Slack/Discord webhook URL"
    echo "  ALERT_THRESHOLD 警報觸發閾值 (預設: 10)"
    echo ""
    echo "範例:"
    echo "  # 分析日誌"
    echo "  $0 analyze"
    echo ""
    echo "  # 實時監控"
    echo "  $0 monitor"
    echo ""
    echo "  # 設置警報"
    echo "  export ALERT_EMAIL=admin@adhd-productivity.com"
    echo "  $0 analyze"
}

# 主函數
main() {
    show_banner
    check_environment
    copy_logs_from_docker
    
    case "${1:-analyze}" in
        "analyze")
            run_analysis
            ;;
        "monitor")
            realtime_monitor
            ;;
        "cleanup")
            cleanup_old_reports
            ;;
        "help"|"--help"|"-h")
            show_help
            ;;
        *)
            log_error "未知命令: $1"
            show_help
            exit 1
            ;;
    esac
    
    # 清理臨時文件
    if [ "$USE_DOCKER" = true ] && [ -d "/tmp/nginx_logs_$$" ]; then
        rm -rf "/tmp/nginx_logs_$$"
    fi
}

main "$@"