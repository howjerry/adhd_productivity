#!/bin/bash

# ADHD 生產力系統 - 安全事件響應腳本
# 自動化處理安全威脅和攻擊事件

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
BLOCK_LIST_FILE="${PROJECT_ROOT}/security/blacklist.conf"
WHITELIST_FILE="${PROJECT_ROOT}/security/whitelist.conf"
INCIDENT_LOG="${PROJECT_ROOT}/security/incident.log"

# 日誌函數
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
    echo "$(date): [INFO] $1" >> "$INCIDENT_LOG"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
    echo "$(date): [SUCCESS] $1" >> "$INCIDENT_LOG"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
    echo "$(date): [WARNING] $1" >> "$INCIDENT_LOG"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
    echo "$(date): [ERROR] $1" >> "$INCIDENT_LOG"
}

log_critical() {
    echo -e "${RED}[CRITICAL]${NC} $1"
    echo "$(date): [CRITICAL] $1" >> "$INCIDENT_LOG"
}

# 顯示橫幅
show_banner() {
    echo -e "${CYAN}"
    echo "╔══════════════════════════════════════════════════════════╗"
    echo "║            ADHD 生產力系統 安全事件響應器               ║"
    echo "║                                                          ║"
    echo "║  🚨 自動化威脅響應 | 🛡️ IP 封鎖管理 | 📋 事件記錄      ║"
    echo "╚══════════════════════════════════════════════════════════╝"
    echo -e "${NC}"
}

# 初始化安全文件
initialize_security_files() {
    mkdir -p "$(dirname "$BLOCK_LIST_FILE")"
    
    # 創建黑名單文件
    if [ ! -f "$BLOCK_LIST_FILE" ]; then
        cat > "$BLOCK_LIST_FILE" << 'EOF'
# ADHD 生產力系統 - IP 黑名單
# 格式: IP地址 封鎖原因 封鎖時間
# 範例: 192.168.1.100 "SQL injection attack" "2024-01-20 10:30:00"

EOF
        log_info "創建黑名單文件: $BLOCK_LIST_FILE"
    fi
    
    # 創建白名單文件
    if [ ! -f "$WHITELIST_FILE" ]; then
        cat > "$WHITELIST_FILE" << 'EOF'
# ADHD 生產力系統 - IP 白名單
# 受信任的 IP 地址，不會被自動封鎖
127.0.0.1
::1
10.0.0.0/8
172.16.0.0/12
192.168.0.0/16

EOF
        log_info "創建白名單文件: $WHITELIST_FILE"
    fi
    
    # 創建事件日誌
    if [ ! -f "$INCIDENT_LOG" ]; then
        touch "$INCIDENT_LOG"
        log_info "創建事件日誌文件: $INCIDENT_LOG"
    fi
}

# 檢查 IP 是否在白名單中
is_whitelisted() {
    local ip="$1"
    
    if [ -f "$WHITELIST_FILE" ]; then
        # 檢查精確匹配
        if grep -q "^$ip$" "$WHITELIST_FILE"; then
            return 0
        fi
        
        # 檢查 CIDR 範圍
        while read -r range; do
            # 跳過註釋和空行
            if [[ "$range" =~ ^[[:space:]]*# ]] || [[ -z "$range" ]]; then
                continue
            fi
            
            # 簡單的 CIDR 檢查（需要 ipcalc 或其他工具進行完整檢查）
            if [[ "$range" =~ "/" ]]; then
                # 這裡應該使用更精確的 CIDR 檢查
                local network="${range%/*}"
                if [[ "$ip" =~ ^"$network" ]]; then
                    return 0
                fi
            fi
        done < "$WHITELIST_FILE"
    fi
    
    return 1
}

# 檢查 IP 是否已被封鎖
is_blocked() {
    local ip="$1"
    
    if [ -f "$BLOCK_LIST_FILE" ]; then
        grep -q "^$ip " "$BLOCK_LIST_FILE"
    else
        return 1
    fi
}

# 封鎖 IP 地址
block_ip() {
    local ip="$1"
    local reason="${2:-Unknown threat}"
    local duration="${3:-permanent}"
    
    # 檢查是否為有效 IP
    if ! [[ "$ip" =~ ^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$ ]]; then
        log_error "無效的 IP 地址格式: $ip"
        return 1
    fi
    
    # 檢查是否在白名單中
    if is_whitelisted "$ip"; then
        log_warning "IP $ip 在白名單中，跳過封鎖"
        return 0
    fi
    
    # 檢查是否已被封鎖
    if is_blocked "$ip"; then
        log_warning "IP $ip 已在黑名單中"
        return 0
    fi
    
    # 添加到黑名單
    local timestamp=$(date)
    echo "$ip \"$reason\" \"$timestamp\" $duration" >> "$BLOCK_LIST_FILE"
    log_warning "封鎖 IP: $ip (原因: $reason)"
    
    # 應用 iptables 規則
    apply_iptables_block "$ip" "$reason"
    
    # 更新 Nginx 配置
    update_nginx_blocklist
    
    # 記錄事件
    log_critical "SECURITY_BLOCK: IP $ip 已被封鎖 (原因: $reason)"
}

# 解除封鎖 IP 地址
unblock_ip() {
    local ip="$1"
    
    if ! is_blocked "$ip"; then
        log_warning "IP $ip 不在黑名單中"
        return 0
    fi
    
    # 從黑名單移除
    if [ -f "$BLOCK_LIST_FILE" ]; then
        grep -v "^$ip " "$BLOCK_LIST_FILE" > "${BLOCK_LIST_FILE}.tmp"
        mv "${BLOCK_LIST_FILE}.tmp" "$BLOCK_LIST_FILE"
    fi
    
    # 移除 iptables 規則
    remove_iptables_block "$ip"
    
    # 更新 Nginx 配置
    update_nginx_blocklist
    
    log_success "解除封鎖 IP: $ip"
}

# 應用 iptables 規則
apply_iptables_block() {
    local ip="$1"
    local reason="$2"
    
    # 檢查 iptables 是否可用
    if command -v iptables >/dev/null 2>&1; then
        # 創建自定義鏈（如果不存在）
        iptables -N ADHD_BLOCK 2>/dev/null || true
        
        # 添加規則到自定義鏈
        iptables -A ADHD_BLOCK -s "$ip" -j DROP
        
        # 確保自定義鏈在 INPUT 鏈中被調用
        if ! iptables -C INPUT -j ADHD_BLOCK 2>/dev/null; then
            iptables -I INPUT -j ADHD_BLOCK
        fi
        
        log_info "已應用 iptables 規則封鎖 $ip"
    elif docker ps >/dev/null 2>&1; then
        # 在 Docker 環境中通過 Nginx 封鎖
        log_info "Docker 環境中，通過 Nginx 配置封鎖 $ip"
    else
        log_warning "無法應用 iptables 規則，僅使用 Nginx 封鎖"
    fi
}

# 移除 iptables 規則
remove_iptables_block() {
    local ip="$1"
    
    if command -v iptables >/dev/null 2>&1; then
        # 移除規則
        iptables -D ADHD_BLOCK -s "$ip" -j DROP 2>/dev/null || true
        log_info "已移除 iptables 規則解除封鎖 $ip"
    fi
}

# 更新 Nginx 黑名單配置
update_nginx_blocklist() {
    local nginx_blocklist="${PROJECT_ROOT}/nginx/conf.d/dynamic-blocklist.conf"
    
    log_info "更新 Nginx 黑名單配置..."
    
    cat > "$nginx_blocklist" << 'EOF'
# ADHD 生產力系統 - 動態 IP 黑名單
# 此文件由安全事件響應腳本自動生成，請勿手動編輯

map $remote_addr $dynamic_blocked_ip {
    default 0;
EOF
    
    # 從黑名單文件生成 Nginx 配置
    if [ -f "$BLOCK_LIST_FILE" ]; then
        while read -r line; do
            # 跳過註釋和空行
            if [[ "$line" =~ ^[[:space:]]*# ]] || [[ -z "$line" ]]; then
                continue
            fi
            
            local ip=$(echo "$line" | awk '{print $1}')
            if [[ "$ip" =~ ^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$ ]]; then
                echo "    \"$ip\" 1;" >> "$nginx_blocklist"
            fi
        done < "$BLOCK_LIST_FILE"
    fi
    
    echo "}" >> "$nginx_blocklist"
    
    # 重載 Nginx 配置
    reload_nginx_config
}

# 重載 Nginx 配置
reload_nginx_config() {
    if docker ps | grep -q "adhd-nginx"; then
        log_info "重載 Nginx 配置..."
        if docker exec adhd-nginx nginx -t; then
            docker exec adhd-nginx nginx -s reload
            log_success "Nginx 配置重載成功"
        else
            log_error "Nginx 配置檢查失敗，未重載"
        fi
    else
        log_warning "Nginx 容器未運行，跳過配置重載"
    fi
}

# 自動威脅檢測和響應
auto_threat_response() {
    local log_file="${1:-/var/log/nginx/security.log}"
    local threshold="${2:-5}"
    
    log_info "啟動自動威脅檢測 (閾值: $threshold)"
    
    if [ ! -f "$log_file" ]; then
        log_error "日誌文件不存在: $log_file"
        return 1
    fi
    
    # 分析最近 5 分鐘的日誌
    local recent_logs=$(find "$log_file" -mmin -5 -type f 2>/dev/null)
    
    if [ -z "$recent_logs" ]; then
        log_info "沒有最近的日誌更新"
        return 0
    fi
    
    # 檢測高頻攻擊的 IP
    tail -n 1000 "$log_file" | \
    grep "threat_score=[2-4]" | \
    awk '{print $1}' | \
    sort | uniq -c | \
    while read count ip; do
        if [ "$count" -ge "$threshold" ]; then
            if ! is_whitelisted "$ip" && ! is_blocked "$ip"; then
                local attack_types=$(tail -n 1000 "$log_file" | grep "$ip" | grep -o -E "(sql_injection|xss_attack|path_traversal)=1" | sort | uniq | tr '\n' ',' | sed 's/,$//')
                block_ip "$ip" "Automated block: High threat activity ($count attacks: $attack_types)" "24h"
            fi
        fi
    done
    
    # 檢測 SQL 注入攻擊
    local sql_attackers=$(tail -n 1000 "$log_file" | grep "sql_injection=1" | awk '{print $1}' | sort | uniq -c | awk '$1 >= 3 {print $2}')
    for ip in $sql_attackers; do
        if ! is_whitelisted "$ip" && ! is_blocked "$ip"; then
            block_ip "$ip" "Automated block: SQL injection attack detected" "24h"
        fi
    done
    
    # 檢測 XSS 攻擊
    local xss_attackers=$(tail -n 1000 "$log_file" | grep "xss_attack=1" | awk '{print $1}' | sort | uniq -c | awk '$1 >= 3 {print $2}')
    for ip in $xss_attackers; do
        if ! is_whitelisted "$ip" && ! is_blocked "$ip"; then
            block_ip "$ip" "Automated block: XSS attack detected" "24h"
        fi
    done
}

# 清理過期的封鎖
cleanup_expired_blocks() {
    log_info "清理過期的 IP 封鎖..."
    
    if [ ! -f "$BLOCK_LIST_FILE" ]; then
        return 0
    fi
    
    local current_time=$(date +%s)
    local temp_file="${BLOCK_LIST_FILE}.tmp"
    
    while read -r line; do
        # 跳過註釋和空行
        if [[ "$line" =~ ^[[:space:]]*# ]] || [[ -z "$line" ]]; then
            echo "$line" >> "$temp_file"
            continue
        fi
        
        local ip=$(echo "$line" | awk '{print $1}')
        local duration=$(echo "$line" | awk '{print $4}')
        local block_time=$(echo "$line" | grep -o '"[^"]*"' | tail -n 1 | tr -d '"')
        
        # 檢查是否為永久封鎖
        if [ "$duration" = "permanent" ]; then
            echo "$line" >> "$temp_file"
            continue
        fi
        
        # 解析持續時間
        local duration_seconds=0
        if [[ "$duration" =~ ([0-9]+)h ]]; then
            duration_seconds=$((${BASH_REMATCH[1]} * 3600))
        elif [[ "$duration" =~ ([0-9]+)d ]]; then
            duration_seconds=$((${BASH_REMATCH[1]} * 86400))
        fi
        
        # 計算過期時間
        local block_timestamp=$(date -d "$block_time" +%s 2>/dev/null || echo 0)
        local expire_time=$((block_timestamp + duration_seconds))
        
        if [ "$current_time" -lt "$expire_time" ]; then
            # 尚未過期，保留
            echo "$line" >> "$temp_file"
        else
            # 已過期，解除封鎖
            log_info "自動解除過期封鎖: $ip"
            remove_iptables_block "$ip"
        fi
    done < "$BLOCK_LIST_FILE"
    
    mv "$temp_file" "$BLOCK_LIST_FILE"
    update_nginx_blocklist
}

# 生成安全報告
generate_security_report() {
    local report_file="${PROJECT_ROOT}/security/reports/incident_report_$(date +%Y%m%d_%H%M%S).md"
    
    mkdir -p "$(dirname "$report_file")"
    
    cat > "$report_file" << EOF
# ADHD 生產力系統 - 安全事件報告

**生成時間**: $(date)

## 當前封鎖狀態

### 活躍的 IP 封鎖
EOF
    
    if [ -f "$BLOCK_LIST_FILE" ]; then
        local blocked_count=$(grep -v "^#" "$BLOCK_LIST_FILE" | grep -v "^$" | wc -l)
        echo "- **總計封鎖 IP**: $blocked_count" >> "$report_file"
        echo "" >> "$report_file"
        
        if [ "$blocked_count" -gt 0 ]; then
            echo "| IP 地址 | 封鎖原因 | 封鎖時間 | 持續時間 |" >> "$report_file"
            echo "|---------|----------|----------|----------|" >> "$report_file"
            
            while read -r line; do
                if [[ "$line" =~ ^[[:space:]]*# ]] || [[ -z "$line" ]]; then
                    continue
                fi
                
                local ip=$(echo "$line" | awk '{print $1}')
                local reason=$(echo "$line" | grep -o '"[^"]*"' | head -n 1 | tr -d '"')
                local time=$(echo "$line" | grep -o '"[^"]*"' | tail -n 1 | tr -d '"')
                local duration=$(echo "$line" | awk '{print $4}')
                
                echo "| $ip | $reason | $time | $duration |" >> "$report_file"
            done < "$BLOCK_LIST_FILE"
        fi
    else
        echo "- **總計封鎖 IP**: 0" >> "$report_file"
    fi
    
    echo "" >> "$report_file"
    echo "## 最近安全事件" >> "$report_file"
    echo "" >> "$report_file"
    
    if [ -f "$INCIDENT_LOG" ]; then
        echo "\`\`\`" >> "$report_file"
        tail -n 20 "$INCIDENT_LOG" >> "$report_file"
        echo "\`\`\`" >> "$report_file"
    fi
    
    echo "" >> "$report_file"
    echo "---" >> "$report_file"
    echo "*報告由 ADHD 安全事件響應系統自動生成*" >> "$report_file"
    
    log_success "安全報告已生成: $report_file"
}

# 顯示幫助
show_help() {
    echo "ADHD 生產力系統 - 安全事件響應器"
    echo ""
    echo "使用方法:"
    echo "  $0 [命令] [參數]"
    echo ""
    echo "命令:"
    echo "  block <IP> [原因] [持續時間]  封鎖指定的 IP 地址"
    echo "  unblock <IP>                 解除封鎖指定的 IP 地址"
    echo "  list                         顯示當前的封鎖清單"
    echo "  auto [閾值]                  啟動自動威脅檢測 (預設閾值: 5)"
    echo "  cleanup                      清理過期的封鎖"
    echo "  report                       生成安全報告"
    echo "  monitor                      啟動持續監控模式"
    echo "  help                         顯示此幫助信息"
    echo ""
    echo "範例:"
    echo "  # 封鎖惡意 IP"
    echo "  $0 block 192.168.1.100 \"SQL injection attack\" \"24h\""
    echo ""
    echo "  # 解除封鎖"
    echo "  $0 unblock 192.168.1.100"
    echo ""
    echo "  # 啟動自動檢測 (閾值 3)"
    echo "  $0 auto 3"
    echo ""
    echo "  # 清理過期封鎖"
    echo "  $0 cleanup"
}

# 主函數
main() {
    show_banner
    initialize_security_files
    
    case "${1:-help}" in
        "block")
            if [ -z "$2" ]; then
                log_error "請指定要封鎖的 IP 地址"
                exit 1
            fi
            block_ip "$2" "${3:-Manual block}" "${4:-permanent}"
            ;;
        "unblock")
            if [ -z "$2" ]; then
                log_error "請指定要解除封鎖的 IP 地址"
                exit 1
            fi
            unblock_ip "$2"
            ;;
        "list")
            if [ -f "$BLOCK_LIST_FILE" ]; then
                echo "當前封鎖的 IP 地址:"
                cat "$BLOCK_LIST_FILE"
            else
                echo "沒有封鎖的 IP 地址"
            fi
            ;;
        "auto")
            auto_threat_response "/var/log/nginx/security.log" "${2:-5}"
            ;;
        "cleanup")
            cleanup_expired_blocks
            ;;
        "report")
            generate_security_report
            ;;
        "monitor")
            log_info "啟動持續監控模式..."
            while true; do
                auto_threat_response "/var/log/nginx/security.log" 5
                cleanup_expired_blocks
                sleep 300  # 每 5 分鐘檢查一次
            done
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
}

main "$@"