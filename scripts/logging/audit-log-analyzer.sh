#!/bin/bash

# ADHD 生產力系統 - 審計日誌分析腳本
# 功能：分析安全審計日誌，檢測異常行為和安全事件

set -euo pipefail

# ===========================================
# 配置變數
# ===========================================
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOG_DIR="/app/logs"
SECURITY_LOG_DIR="$LOG_DIR/security"
AUDIT_REPORT_DIR="/reports/audit"
DATE=$(date +%Y%m%d)
REPORT_FILE="$AUDIT_REPORT_DIR/audit-analysis-$DATE.html"

# 分析時間範圍 (預設為昨天)
START_DATE="${1:-$(date -d 'yesterday' +%Y-%m-%d)}"
END_DATE="${2:-$(date +%Y-%m-%d)}"

# ===========================================
# 日誌函數
# ===========================================
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1"
}

error_exit() {
    log "錯誤: $1"
    exit 1
}

# ===========================================
# 建立報告目錄
# ===========================================
log "開始審計日誌分析 ($START_DATE 至 $END_DATE)"

if [[ ! -d "$AUDIT_REPORT_DIR" ]]; then
    mkdir -p "$AUDIT_REPORT_DIR" || error_exit "無法創建報告目錄"
fi

# ===========================================
# HTML 報告樣板
# ===========================================
create_html_header() {
    cat > "$REPORT_FILE" << EOF
<!DOCTYPE html>
<html lang="zh-TW">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>ADHD 生產力系統 - 審計日誌分析報告</title>
    <style>
        body { font-family: 'Microsoft YaHei', sans-serif; margin: 20px; background-color: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        h1 { color: #2c3e50; border-bottom: 3px solid #3498db; padding-bottom: 10px; }
        h2 { color: #34495e; margin-top: 30px; }
        .summary { background: #ecf0f1; padding: 15px; border-radius: 5px; margin: 20px 0; }
        .alert { background: #e74c3c; color: white; padding: 10px; border-radius: 5px; margin: 10px 0; }
        .warning { background: #f39c12; color: white; padding: 10px; border-radius: 5px; margin: 10px 0; }
        .info { background: #3498db; color: white; padding: 10px; border-radius: 5px; margin: 10px 0; }
        .success { background: #27ae60; color: white; padding: 10px; border-radius: 5px; margin: 10px 0; }
        table { width: 100%; border-collapse: collapse; margin: 20px 0; }
        th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }
        th { background-color: #3498db; color: white; }
        tr:nth-child(even) { background-color: #f2f2f2; }
        .metric { display: inline-block; margin: 10px; padding: 15px; background: #3498db; color: white; border-radius: 5px; text-align: center; min-width: 120px; }
        .chart { margin: 20px 0; }
        .timestamp { font-size: 0.9em; color: #7f8c8d; }
    </style>
</head>
<body>
    <div class="container">
        <h1>ADHD 生產力系統 - 審計日誌分析報告</h1>
        <div class="summary">
            <strong>分析期間：</strong> $START_DATE 至 $END_DATE<br>
            <strong>生成時間：</strong> $(date)<br>
            <strong>分析範圍：</strong> 安全事件、使用者活動、系統存取、異常行為
        </div>
EOF
}

# ===========================================
# 分析登入活動
# ===========================================
analyze_login_activity() {
    log "分析登入活動..."
    
    cat >> "$REPORT_FILE" << EOF
        <h2>🔐 登入活動分析</h2>
EOF
    
    # 統計登入次數
    local total_logins=$(grep -r "login_success" "$SECURITY_LOG_DIR" 2>/dev/null | \
        grep -E "$START_DATE|$END_DATE" | wc -l || echo "0")
    
    local failed_logins=$(grep -r "login_failed" "$SECURITY_LOG_DIR" 2>/dev/null | \
        grep -E "$START_DATE|$END_DATE" | wc -l || echo "0")
    
    local unique_users=$(grep -r "login_success" "$SECURITY_LOG_DIR" 2>/dev/null | \
        grep -E "$START_DATE|$END_DATE" | \
        grep -o '"user_id":"[^"]*"' | sort -u | wc -l || echo "0")
    
    cat >> "$REPORT_FILE" << EOF
        <div class="metric">
            <div>總登入次數</div>
            <div style="font-size: 2em;">$total_logins</div>
        </div>
        <div class="metric">
            <div>失敗登入</div>
            <div style="font-size: 2em;">$failed_logins</div>
        </div>
        <div class="metric">
            <div>活躍使用者</div>
            <div style="font-size: 2em;">$unique_users</div>
        </div>
EOF
    
    # 檢查可疑登入活動
    if [[ $failed_logins -gt 50 ]]; then
        cat >> "$REPORT_FILE" << EOF
        <div class="alert">
            <strong>警告：</strong> 檢測到異常高的登入失敗次數 ($failed_logins 次)，可能存在暴力破解攻擊。
        </div>
EOF
    fi
    
    # 顯示失敗登入詳情
    if [[ $failed_logins -gt 0 ]]; then
        cat >> "$REPORT_FILE" << EOF
        <h3>失敗登入詳情</h3>
        <table>
            <tr><th>時間</th><th>IP 地址</th><th>使用者</th><th>原因</th></tr>
EOF
        
        grep -r "login_failed" "$SECURITY_LOG_DIR" 2>/dev/null | \
            grep -E "$START_DATE|$END_DATE" | head -20 | \
            while IFS= read -r line; do
                timestamp=$(echo "$line" | grep -o '"timestamp":"[^"]*"' | cut -d'"' -f4)
                ip=$(echo "$line" | grep -o '"ip_address":"[^"]*"' | cut -d'"' -f4)
                user=$(echo "$line" | grep -o '"user_id":"[^"]*"' | cut -d'"' -f4)
                reason=$(echo "$line" | grep -o '"reason":"[^"]*"' | cut -d'"' -f4)
                
                echo "<tr><td>$timestamp</td><td>$ip</td><td>$user</td><td>$reason</td></tr>" >> "$REPORT_FILE"
            done
        
        echo "</table>" >> "$REPORT_FILE"
    fi
}

# ===========================================
# 分析 API 存取
# ===========================================
analyze_api_access() {
    log "分析 API 存取模式..."
    
    cat >> "$REPORT_FILE" << EOF
        <h2>🔗 API 存取分析</h2>
EOF
    
    # 分析 API 呼叫頻率
    local total_requests=$(grep -r "api_request" "$LOG_DIR" 2>/dev/null | \
        grep -E "$START_DATE|$END_DATE" | wc -l || echo "0")
    
    local error_requests=$(grep -r "api_request" "$LOG_DIR" 2>/dev/null | \
        grep -E "$START_DATE|$END_DATE" | \
        grep -E '"status_code":"[45][0-9][0-9]"' | wc -l || echo "0")
    
    cat >> "$REPORT_FILE" << EOF
        <div class="metric">
            <div>API 請求總數</div>
            <div style="font-size: 2em;">$total_requests</div>
        </div>
        <div class="metric">
            <div>錯誤請求</div>
            <div style="font-size: 2em;">$error_requests</div>
        </div>
EOF
    
    # 計算錯誤率
    if [[ $total_requests -gt 0 ]]; then
        local error_rate=$((error_requests * 100 / total_requests))
        
        cat >> "$REPORT_FILE" << EOF
        <div class="metric">
            <div>錯誤率</div>
            <div style="font-size: 2em;">$error_rate%</div>
        </div>
EOF
        
        if [[ $error_rate -gt 10 ]]; then
            cat >> "$REPORT_FILE" << EOF
        <div class="warning">
            <strong>注意：</strong> API 錯誤率較高 ($error_rate%)，建議檢查系統穩定性。
        </div>
EOF
        fi
    fi
    
    # 顯示最常存取的端點
    cat >> "$REPORT_FILE" << EOF
        <h3>最常存取的 API 端點</h3>
        <table>
            <tr><th>端點</th><th>請求次數</th><th>平均回應時間</th></tr>
EOF
    
    grep -r "api_request" "$LOG_DIR" 2>/dev/null | \
        grep -E "$START_DATE|$END_DATE" | \
        grep -o '"endpoint":"[^"]*"' | \
        sort | uniq -c | sort -nr | head -10 | \
        while read count endpoint; do
            endpoint_clean=$(echo "$endpoint" | cut -d'"' -f4)
            echo "<tr><td>$endpoint_clean</td><td>$count</td><td>-</td></tr>" >> "$REPORT_FILE"
        done
    
    echo "</table>" >> "$REPORT_FILE"
}

# ===========================================
# 分析安全事件
# ===========================================
analyze_security_events() {
    log "分析安全事件..."
    
    cat >> "$REPORT_FILE" << EOF
        <h2>🛡️ 安全事件分析</h2>
EOF
    
    # 檢查各類安全事件
    local sql_injection=$(grep -r "sql_injection" "$SECURITY_LOG_DIR" 2>/dev/null | \
        grep -E "$START_DATE|$END_DATE" | wc -l || echo "0")
    
    local xss_attempts=$(grep -r "xss_attempt" "$SECURITY_LOG_DIR" 2>/dev/null | \
        grep -E "$START_DATE|$END_DATE" | wc -l || echo "0")
    
    local rate_limit_hits=$(grep -r "rate_limit_exceeded" "$SECURITY_LOG_DIR" 2>/dev/null | \
        grep -E "$START_DATE|$END_DATE" | wc -l || echo "0")
    
    local unauthorized_access=$(grep -r "unauthorized_access" "$SECURITY_LOG_DIR" 2>/dev/null | \
        grep -E "$START_DATE|$END_DATE" | wc -l || echo "0")
    
    cat >> "$REPORT_FILE" << EOF
        <div class="metric">
            <div>SQL 注入嘗試</div>
            <div style="font-size: 2em;">$sql_injection</div>
        </div>
        <div class="metric">
            <div>XSS 攻擊嘗試</div>
            <div style="font-size: 2em;">$xss_attempts</div>
        </div>
        <div class="metric">
            <div>速率限制觸發</div>
            <div style="font-size: 2em;">$rate_limit_hits</div>
        </div>
        <div class="metric">
            <div>未授權存取</div>
            <div style="font-size: 2em;">$unauthorized_access</div>
        </div>
EOF
    
    # 檢查是否有嚴重安全事件
    local total_security_events=$((sql_injection + xss_attempts + unauthorized_access))
    
    if [[ $total_security_events -gt 0 ]]; then
        cat >> "$REPORT_FILE" << EOF
        <div class="alert">
            <strong>安全警報：</strong> 檢測到 $total_security_events 個安全事件，請立即檢查！
        </div>
EOF
    else
        cat >> "$REPORT_FILE" << EOF
        <div class="success">
            <strong>安全狀態良好：</strong> 未檢測到嚴重安全威脅。
        </div>
EOF
    fi
}

# ===========================================
# 分析使用者行為
# ===========================================
analyze_user_behavior() {
    log "分析使用者行為模式..."
    
    cat >> "$REPORT_FILE" << EOF
        <h2>👤 使用者行為分析</h2>
EOF
    
    # 分析使用者活動
    local task_created=$(grep -r "task_created" "$LOG_DIR" 2>/dev/null | \
        grep -E "$START_DATE|$END_DATE" | wc -l || echo "0")
    
    local task_completed=$(grep -r "task_completed" "$LOG_DIR" 2>/dev/null | \
        grep -E "$START_DATE|$END_DATE" | wc -l || echo "0")
    
    local timer_sessions=$(grep -r "timer_started" "$LOG_DIR" 2>/dev/null | \
        grep -E "$START_DATE|$END_DATE" | wc -l || echo "0")
    
    cat >> "$REPORT_FILE" << EOF
        <div class="metric">
            <div>任務創建</div>
            <div style="font-size: 2em;">$task_created</div>
        </div>
        <div class="metric">
            <div>任務完成</div>
            <div style="font-size: 2em;">$task_completed</div>
        </div>
        <div class="metric">
            <div>計時器使用</div>
            <div style="font-size: 2em;">$timer_sessions</div>
        </div>
EOF
    
    # 計算完成率
    if [[ $task_created -gt 0 ]]; then
        local completion_rate=$((task_completed * 100 / task_created))
        
        cat >> "$REPORT_FILE" << EOF
        <div class="metric">
            <div>任務完成率</div>
            <div style="font-size: 2em;">$completion_rate%</div>
        </div>
EOF
    fi
    
    # 顯示最活躍使用者
    cat >> "$REPORT_FILE" << EOF
        <h3>最活躍使用者</h3>
        <table>
            <tr><th>使用者 ID</th><th>活動次數</th><th>最後活動時間</th></tr>
EOF
    
    grep -r "user_action" "$LOG_DIR" 2>/dev/null | \
        grep -E "$START_DATE|$END_DATE" | \
        grep -o '"user_id":"[^"]*"' | \
        sort | uniq -c | sort -nr | head -10 | \
        while read count user; do
            user_clean=$(echo "$user" | cut -d'"' -f4)
            echo "<tr><td>$user_clean</td><td>$count</td><td>-</td></tr>" >> "$REPORT_FILE"
        done
    
    echo "</table>" >> "$REPORT_FILE"
}

# ===========================================
# 生成建議和摘要
# ===========================================
generate_recommendations() {
    log "生成安全建議..."
    
    cat >> "$REPORT_FILE" << EOF
        <h2>📋 安全建議與行動項目</h2>
        <div class="info">
            <h3>自動檢測結果：</h3>
            <ul>
EOF
    
    # 基於分析結果生成建議
    local failed_logins=$(grep -r "login_failed" "$SECURITY_LOG_DIR" 2>/dev/null | \
        grep -E "$START_DATE|$END_DATE" | wc -l || echo "0")
    
    if [[ $failed_logins -gt 20 ]]; then
        echo "<li>建議加強帳戶安全策略，考慮實施多因素認證</li>" >> "$REPORT_FILE"
    fi
    
    local rate_limit_hits=$(grep -r "rate_limit_exceeded" "$SECURITY_LOG_DIR" 2>/dev/null | \
        grep -E "$START_DATE|$END_DATE" | wc -l || echo "0")
    
    if [[ $rate_limit_hits -gt 100 ]]; then
        echo "<li>檢討速率限制設定，可能需要調整閾值</li>" >> "$REPORT_FILE"
    fi
    
    echo "<li>定期檢查和更新安全策略</li>" >> "$REPORT_FILE"
    echo "<li>持續監控異常使用者行為</li>" >> "$REPORT_FILE"
    echo "<li>確保所有安全補丁都已應用</li>" >> "$REPORT_FILE"
    
    cat >> "$REPORT_FILE" << EOF
            </ul>
        </div>
        
        <h2>📊 下一步行動</h2>
        <div class="warning">
            <ol>
                <li>檢查所有標記為高風險的事件</li>
                <li>驗證可疑 IP 地址和使用者帳戶</li>
                <li>更新安全監控規則</li>
                <li>與團隊分享此報告</li>
                <li>計劃下一次安全審核</li>
            </ol>
        </div>
EOF
}

# ===========================================
# 完成 HTML 報告
# ===========================================
complete_html_report() {
    cat >> "$REPORT_FILE" << EOF
        <div class="summary">
            <strong>報告生成完成時間：</strong> $(date)<br>
            <strong>下次分析建議：</strong> $(date -d '+1 day')<br>
            <strong>報告檔案：</strong> $REPORT_FILE
        </div>
    </div>
</body>
</html>
EOF
}

# ===========================================
# 主執行流程
# ===========================================
main() {
    create_html_header
    analyze_login_activity
    analyze_api_access
    analyze_security_events
    analyze_user_behavior
    generate_recommendations
    complete_html_report
    
    log "審計日誌分析完成"
    log "報告檔案：$REPORT_FILE"
    
    # 如果有重要安全事件，發送警報
    local total_security_events=$(grep -r -E "(sql_injection|xss_attempt|unauthorized_access)" "$SECURITY_LOG_DIR" 2>/dev/null | \
        grep -E "$START_DATE|$END_DATE" | wc -l || echo "0")
    
    if [[ $total_security_events -gt 0 ]]; then
        log "警告：檢測到 $total_security_events 個安全事件！"
        return 1
    fi
    
    return 0
}

# 執行主函數
main "$@"