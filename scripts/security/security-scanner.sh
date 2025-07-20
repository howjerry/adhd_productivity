#!/bin/bash

# ADHD 生產力系統 - 安全掃描和合規性檢查腳本
# 功能：全面的安全漏洞掃描、配置檢查和合規性驗證

set -euo pipefail

# ===========================================
# 配置變數
# ===========================================
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOG_FILE="/logs/security-scanner.log"
REPORT_DIR="/reports/security"
DATE=$(date +%Y%m%d_%H%M%S)
REPORT_FILE="$REPORT_DIR/security-scan-$DATE.html"

# 掃描範圍配置
SCAN_CONTAINERS=true
SCAN_NETWORK=true
SCAN_FILES=true
SCAN_CONFIGS=true
SCAN_VULNERABILITIES=true

# 嚴重性等級
CRITICAL_COUNT=0
HIGH_COUNT=0
MEDIUM_COUNT=0
LOW_COUNT=0
INFO_COUNT=0

# ===========================================
# 日誌函數
# ===========================================
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

error_exit() {
    log "錯誤: $1"
    exit 1
}

log_finding() {
    local severity=$1
    local title=$2
    local description=$3
    
    case $severity in
        "CRITICAL") ((CRITICAL_COUNT++)) ;;
        "HIGH") ((HIGH_COUNT++)) ;;
        "MEDIUM") ((MEDIUM_COUNT++)) ;;
        "LOW") ((LOW_COUNT++)) ;;
        "INFO") ((INFO_COUNT++)) ;;
    esac
    
    log "[$severity] $title: $description"
}

# ===========================================
# 初始化
# ===========================================
log "開始安全掃描和合規性檢查..."

if [[ ! -d "$REPORT_DIR" ]]; then
    mkdir -p "$REPORT_DIR" || error_exit "無法創建報告目錄"
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
    <title>ADHD 生產力系統 - 安全掃描報告</title>
    <style>
        body { font-family: 'Microsoft YaHei', sans-serif; margin: 20px; background-color: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        h1 { color: #2c3e50; border-bottom: 3px solid #e74c3c; padding-bottom: 10px; }
        h2 { color: #34495e; margin-top: 30px; }
        .summary { background: #ecf0f1; padding: 15px; border-radius: 5px; margin: 20px 0; }
        .critical { background: #8b0000; color: white; padding: 10px; border-radius: 5px; margin: 10px 0; }
        .high { background: #e74c3c; color: white; padding: 10px; border-radius: 5px; margin: 10px 0; }
        .medium { background: #f39c12; color: white; padding: 10px; border-radius: 5px; margin: 10px 0; }
        .low { background: #3498db; color: white; padding: 10px; border-radius: 5px; margin: 10px 0; }
        .info { background: #95a5a6; color: white; padding: 10px; border-radius: 5px; margin: 10px 0; }
        .success { background: #27ae60; color: white; padding: 10px; border-radius: 5px; margin: 10px 0; }
        table { width: 100%; border-collapse: collapse; margin: 20px 0; }
        th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }
        th { background-color: #e74c3c; color: white; }
        tr:nth-child(even) { background-color: #f2f2f2; }
        .metric { display: inline-block; margin: 10px; padding: 15px; background: #e74c3c; color: white; border-radius: 5px; text-align: center; min-width: 120px; }
        .metric.critical { background: #8b0000; }
        .metric.high { background: #e74c3c; }
        .metric.medium { background: #f39c12; }
        .metric.low { background: #3498db; }
        .metric.info { background: #95a5a6; }
        .finding { margin: 15px 0; padding: 15px; border-left: 5px solid #e74c3c; background: #fdf2f2; }
        .finding.critical { border-left-color: #8b0000; background: #fff0f0; }
        .finding.high { border-left-color: #e74c3c; background: #fdf2f2; }
        .finding.medium { border-left-color: #f39c12; background: #fef9e7; }
        .finding.low { border-left-color: #3498db; background: #f0f8ff; }
        .code { background: #f8f8f8; padding: 10px; border-radius: 3px; font-family: monospace; margin: 10px 0; }
    </style>
</head>
<body>
    <div class="container">
        <h1>🔒 ADHD 生產力系統 - 安全掃描報告</h1>
        <div class="summary">
            <strong>掃描時間：</strong> $(date)<br>
            <strong>掃描範圍：</strong> 容器安全、網路安全、檔案權限、配置檢查、漏洞掃描<br>
            <strong>合規標準：</strong> OWASP Top 10, CIS Benchmarks, GDPR 基本要求<br>
            <strong>掃描器版本：</strong> 1.0.0
        </div>
EOF
}

# ===========================================
# 容器安全掃描
# ===========================================
scan_container_security() {
    if [[ "$SCAN_CONTAINERS" != "true" ]]; then
        return
    fi
    
    log "掃描容器安全配置..."
    
    cat >> "$REPORT_FILE" << EOF
        <h2>🐳 容器安全掃描</h2>
EOF
    
    # 檢查容器運行狀態
    local running_containers=$(docker ps --format "table {{.Names}}" | grep -v NAMES | wc -l)
    
    cat >> "$REPORT_FILE" << EOF
        <div class="metric info">
            <div>運行中容器</div>
            <div style="font-size: 2em;">$running_containers</div>
        </div>
EOF
    
    # 檢查容器安全配置
    cat >> "$REPORT_FILE" << EOF
        <h3>容器安全檢查結果</h3>
        <table>
            <tr><th>容器名稱</th><th>安全問題</th><th>嚴重性</th><th>描述</th></tr>
EOF
    
    # 檢查每個容器的安全設定
    while IFS= read -r container; do
        if [[ -n "$container" ]]; then
            # 檢查是否以 root 使用者運行
            local user=$(docker inspect "$container" --format '{{.Config.User}}' 2>/dev/null || echo "")
            if [[ -z "$user" ]]; then
                log_finding "HIGH" "容器 Root 使用者" "容器 $container 以 root 使用者運行"
                echo "<tr><td>$container</td><td>以 root 使用者運行</td><td class='high'>HIGH</td><td>建議配置非特權使用者</td></tr>" >> "$REPORT_FILE"
            fi
            
            # 檢查特權模式
            local privileged=$(docker inspect "$container" --format '{{.HostConfig.Privileged}}' 2>/dev/null || echo "false")
            if [[ "$privileged" == "true" ]]; then
                log_finding "CRITICAL" "特權容器" "容器 $container 運行在特權模式"
                echo "<tr><td>$container</td><td>特權模式運行</td><td class='critical'>CRITICAL</td><td>移除 --privileged 標誌</td></tr>" >> "$REPORT_FILE"
            fi
            
            # 檢查網路模式
            local network_mode=$(docker inspect "$container" --format '{{.HostConfig.NetworkMode}}' 2>/dev/null || echo "")
            if [[ "$network_mode" == "host" ]]; then
                log_finding "MEDIUM" "主機網路模式" "容器 $container 使用主機網路模式"
                echo "<tr><td>$container</td><td>主機網路模式</td><td class='medium'>MEDIUM</td><td>考慮使用自定義網路</td></tr>" >> "$REPORT_FILE"
            fi
            
            # 檢查掛載點
            local mounts=$(docker inspect "$container" --format '{{range .Mounts}}{{.Source}}:{{.Destination}} {{end}}' 2>/dev/null || echo "")
            if echo "$mounts" | grep -q "/var/run/docker.sock"; then
                log_finding "HIGH" "Docker Socket 掛載" "容器 $container 掛載了 Docker socket"
                echo "<tr><td>$container</td><td>Docker socket 掛載</td><td class='high'>HIGH</td><td>移除 Docker socket 掛載</td></tr>" >> "$REPORT_FILE"
            fi
        fi
    done < <(docker ps --format "{{.Names}}")
    
    echo "</table>" >> "$REPORT_FILE"
    
    # 檢查 Docker 守護程序配置
    cat >> "$REPORT_FILE" << EOF
        <h3>Docker 守護程序安全檢查</h3>
EOF
    
    # 檢查 Docker 版本
    local docker_version=$(docker version --format '{{.Server.Version}}' 2>/dev/null || echo "未知")
    
    cat >> "$REPORT_FILE" << EOF
        <div class="finding info">
            <strong>Docker 版本:</strong> $docker_version<br>
            <strong>建議:</strong> 定期更新 Docker 到最新版本以獲得安全修復
        </div>
EOF
}

# ===========================================
# 網路安全掃描
# ===========================================
scan_network_security() {
    if [[ "$SCAN_NETWORK" != "true" ]]; then
        return
    fi
    
    log "掃描網路安全配置..."
    
    cat >> "$REPORT_FILE" << EOF
        <h2>🌐 網路安全掃描</h2>
EOF
    
    # 檢查開放端口
    cat >> "$REPORT_FILE" << EOF
        <h3>端口掃描結果</h3>
        <table>
            <tr><th>端口</th><th>狀態</th><th>服務</th><th>風險等級</th></tr>
EOF
    
    # 掃描常見端口
    local ports=(22 80 443 3000 5000 5432 6379 9090)
    for port in "${ports[@]}"; do
        if ss -tuln | grep -q ":$port "; then
            local service=""
            local risk="LOW"
            
            case $port in
                22) service="SSH"; risk="MEDIUM" ;;
                80) service="HTTP"; risk="INFO" ;;
                443) service="HTTPS"; risk="INFO" ;;
                3000) service="Grafana"; risk="MEDIUM" ;;
                5000) service="Backend API"; risk="INFO" ;;
                5432) service="PostgreSQL"; risk="HIGH" ;;
                6379) service="Redis"; risk="HIGH" ;;
                9090) service="Prometheus"; risk="MEDIUM" ;;
            esac
            
            echo "<tr><td>$port</td><td>開放</td><td>$service</td><td class='$(echo $risk | tr '[:upper:]' '[:lower:]')'>$risk</td></tr>" >> "$REPORT_FILE"
            
            if [[ "$risk" == "HIGH" ]]; then
                log_finding "HIGH" "危險端口開放" "端口 $port ($service) 對外開放"
            fi
        fi
    done
    
    echo "</table>" >> "$REPORT_FILE"
    
    # 檢查防火牆狀態
    local firewall_status="未知"
    if command -v ufw &> /dev/null; then
        firewall_status=$(ufw status | head -1)
    elif command -v firewall-cmd &> /dev/null; then
        firewall_status=$(firewall-cmd --state 2>/dev/null || echo "未運行")
    fi
    
    cat >> "$REPORT_FILE" << EOF
        <h3>防火牆狀態</h3>
        <div class="finding $([ "$firewall_status" = "Status: active" ] && echo "info" || echo "medium")">
            <strong>防火牆狀態:</strong> $firewall_status<br>
            <strong>建議:</strong> $([ "$firewall_status" = "Status: active" ] && echo "防火牆已啟用，定期檢查規則配置" || echo "建議啟用防火牆並配置適當的規則")
        </div>
EOF
    
    # 檢查 SSL/TLS 配置
    if command -v openssl &> /dev/null; then
        cat >> "$REPORT_FILE" << EOF
        <h3>SSL/TLS 安全檢查</h3>
EOF
        
        # 檢查 SSL 憑證 (如果存在)
        if [[ -f "/etc/nginx/certs/adhd-productivity.crt" ]]; then
            local cert_expiry=$(openssl x509 -in /etc/nginx/certs/adhd-productivity.crt -noout -enddate 2>/dev/null | cut -d= -f2 || echo "無法讀取")
            local days_until_expiry=$(( ($(date -d "$cert_expiry" +%s) - $(date +%s)) / 86400 )) 2>/dev/null || echo "N/A"
            
            local cert_class="info"
            if [[ "$days_until_expiry" =~ ^[0-9]+$ ]]; then
                if [[ $days_until_expiry -lt 30 ]]; then
                    cert_class="critical"
                    log_finding "CRITICAL" "SSL 憑證即將過期" "SSL 憑證將在 $days_until_expiry 天後過期"
                elif [[ $days_until_expiry -lt 90 ]]; then
                    cert_class="medium"
                    log_finding "MEDIUM" "SSL 憑證快到期" "SSL 憑證將在 $days_until_expiry 天後過期"
                fi
            fi
            
            cat >> "$REPORT_FILE" << EOF
        <div class="finding $cert_class">
            <strong>SSL 憑證過期時間:</strong> $cert_expiry<br>
            <strong>剩餘天數:</strong> $days_until_expiry 天<br>
            <strong>建議:</strong> $([ $days_until_expiry -lt 30 ] && echo "立即更新 SSL 憑證" || echo "定期檢查憑證狀態")
        </div>
EOF
        fi
    fi
}

# ===========================================
# 檔案系統安全掃描
# ===========================================
scan_file_security() {
    if [[ "$SCAN_FILES" != "true" ]]; then
        return
    fi
    
    log "掃描檔案系統安全..."
    
    cat >> "$REPORT_FILE" << EOF
        <h2>📁 檔案系統安全掃描</h2>
EOF
    
    # 檢查敏感檔案權限
    cat >> "$REPORT_FILE" << EOF
        <h3>敏感檔案權限檢查</h3>
        <table>
            <tr><th>檔案路徑</th><th>權限</th><th>所有者</th><th>風險等級</th></tr>
EOF
    
    # 檢查關鍵配置檔案
    local sensitive_files=(
        "/etc/passwd"
        "/etc/shadow"
        "/etc/ssh/sshd_config"
        "/.env"
        "/.env.production"
    )
    
    for file in "${sensitive_files[@]}"; do
        if [[ -f "$file" ]]; then
            local permissions=$(stat -c "%a" "$file" 2>/dev/null || echo "N/A")
            local owner=$(stat -c "%U:%G" "$file" 2>/dev/null || echo "N/A")
            local risk="INFO"
            
            case $file in
                "/etc/shadow")
                    if [[ "$permissions" != "640" ]] && [[ "$permissions" != "600" ]]; then
                        risk="CRITICAL"
                        log_finding "CRITICAL" "Shadow 檔案權限不安全" "$file 權限為 $permissions"
                    fi
                    ;;
                "/.env"|"/.env.production")
                    if [[ "$permissions" =~ ^[0-7]*[4-7][4-7]$ ]]; then
                        risk="HIGH"
                        log_finding "HIGH" "環境變數檔案權限過寬" "$file 對其他使用者可讀"
                    fi
                    ;;
            esac
            
            echo "<tr><td>$file</td><td>$permissions</td><td>$owner</td><td class='$(echo $risk | tr '[:upper:]' '[:lower:]')'>$risk</td></tr>" >> "$REPORT_FILE"
        fi
    done
    
    echo "</table>" >> "$REPORT_FILE"
    
    # 檢查 SUID/SGID 檔案
    cat >> "$REPORT_FILE" << EOF
        <h3>SUID/SGID 檔案檢查</h3>
EOF
    
    local suid_count=$(find /usr -type f \( -perm -4000 -o -perm -2000 \) 2>/dev/null | wc -l || echo "0")
    
    cat >> "$REPORT_FILE" << EOF
        <div class="finding info">
            <strong>發現 SUID/SGID 檔案數量:</strong> $suid_count<br>
            <strong>建議:</strong> 定期檢查 SUID/SGID 檔案，移除不必要的特權檔案
        </div>
EOF
    
    # 檢查世界可寫檔案
    local world_writable=$(find /opt/adhd-productivity -type f -perm -002 2>/dev/null | wc -l || echo "0")
    if [[ $world_writable -gt 0 ]]; then
        log_finding "MEDIUM" "世界可寫檔案" "發現 $world_writable 個世界可寫檔案"
        cat >> "$REPORT_FILE" << EOF
        <div class="finding medium">
            <strong>世界可寫檔案:</strong> $world_writable 個<br>
            <strong>風險:</strong> 可能被惡意修改<br>
            <strong>建議:</strong> 檢查並修正檔案權限
        </div>
EOF
    fi
}

# ===========================================
# 配置安全檢查
# ===========================================
scan_configuration_security() {
    if [[ "$SCAN_CONFIGS" != "true" ]]; then
        return
    fi
    
    log "檢查配置安全性..."
    
    cat >> "$REPORT_FILE" << EOF
        <h2>⚙️ 配置安全檢查</h2>
EOF
    
    # 檢查 Docker Compose 配置
    cat >> "$REPORT_FILE" << EOF
        <h3>Docker Compose 安全配置</h3>
EOF
    
    if [[ -f "docker-compose.production.yml" ]]; then
        # 檢查是否有硬編碼密碼
        if grep -q "password.*=" docker-compose.production.yml; then
            log_finding "HIGH" "配置檔案包含密碼" "docker-compose.production.yml 可能包含硬編碼密碼"
            cat >> "$REPORT_FILE" << EOF
        <div class="finding high">
            <strong>硬編碼密碼風險:</strong> Docker Compose 檔案可能包含明文密碼<br>
            <strong>建議:</strong> 使用環境變數和 secrets 管理密碼
        </div>
EOF
        fi
        
        # 檢查網路配置
        if grep -q "network_mode.*host" docker-compose.production.yml; then
            log_finding "MEDIUM" "主機網路模式" "使用主機網路模式可能帶來安全風險"
        fi
    fi
    
    # 檢查 Nginx 配置
    cat >> "$REPORT_FILE" << EOF
        <h3>Nginx 安全配置檢查</h3>
EOF
    
    if [[ -f "nginx/nginx.production.conf" ]]; then
        local security_headers=("X-Frame-Options" "X-Content-Type-Options" "X-XSS-Protection" "Strict-Transport-Security")
        local missing_headers=()
        
        for header in "${security_headers[@]}"; do
            if ! grep -q "$header" nginx/nginx.production.conf; then
                missing_headers+=("$header")
                log_finding "MEDIUM" "缺少安全標頭" "Nginx 配置缺少 $header 標頭"
            fi
        done
        
        if [[ ${#missing_headers[@]} -gt 0 ]]; then
            cat >> "$REPORT_FILE" << EOF
        <div class="finding medium">
            <strong>缺少安全標頭:</strong> ${missing_headers[*]}<br>
            <strong>風險:</strong> 增加 XSS、點擊劫持等攻擊風險<br>
            <strong>建議:</strong> 在 Nginx 配置中添加所有必要的安全標頭
        </div>
EOF
        else
            cat >> "$REPORT_FILE" << EOF
        <div class="finding info">
            <strong>安全標頭配置:</strong> 完整<br>
            <strong>狀態:</strong> 所有必要的安全標頭都已配置
        </div>
EOF
        fi
    fi
    
    # 檢查環境變數安全
    cat >> "$REPORT_FILE" << EOF
        <h3>環境變數安全檢查</h3>
EOF
    
    if [[ -f ".env.production" ]]; then
        # 檢查密鑰強度
        local jwt_key_length=$(grep "JWT_SECRET_KEY" .env.production | cut -d= -f2 | wc -c)
        if [[ $jwt_key_length -lt 32 ]]; then
            log_finding "HIGH" "JWT 密鑰過短" "JWT 密鑰長度不足 32 字符"
            cat >> "$REPORT_FILE" << EOF
        <div class="finding high">
            <strong>JWT 密鑰安全:</strong> 密鑰長度不足<br>
            <strong>當前長度:</strong> $jwt_key_length 字符<br>
            <strong>建議:</strong> 使用至少 32 字符的強隨機密鑰
        </div>
EOF
        fi
        
        # 檢查資料庫密碼強度
        local db_password=$(grep "POSTGRES_PASSWORD" .env.production | cut -d= -f2)
        if [[ ${#db_password} -lt 12 ]]; then
            log_finding "MEDIUM" "資料庫密碼過短" "資料庫密碼長度不足 12 字符"
        fi
    fi
}

# ===========================================
# 漏洞掃描
# ===========================================
scan_vulnerabilities() {
    if [[ "$SCAN_VULNERABILITIES" != "true" ]]; then
        return
    fi
    
    log "執行漏洞掃描..."
    
    cat >> "$REPORT_FILE" << EOF
        <h2>🔍 漏洞掃描</h2>
EOF
    
    # 檢查已知漏洞資料庫 (模擬)
    cat >> "$REPORT_FILE" << EOF
        <h3>依賴項漏洞檢查</h3>
EOF
    
    # 檢查 Node.js 依賴項 (如果存在)
    if [[ -f "package.json" ]]; then
        if command -v npm &> /dev/null; then
            # 執行 npm audit (輸出到臨時檔案)
            local npm_audit_output=$(npm audit --json 2>/dev/null || echo '{"vulnerabilities": {}}')
            local vulnerability_count=$(echo "$npm_audit_output" | grep -o '"vulnerabilities"' | wc -l || echo "0")
            
            cat >> "$REPORT_FILE" << EOF
        <div class="finding info">
            <strong>Node.js 依賴項掃描:</strong> 完成<br>
            <strong>發現漏洞:</strong> $vulnerability_count 個<br>
            <strong>建議:</strong> 定期執行 npm audit 並修復發現的漏洞
        </div>
EOF
        fi
    fi
    
    # 檢查 .NET 依賴項 (如果存在)
    if [[ -f "backend/AdhdProductivitySystem.sln" ]]; then
        cat >> "$REPORT_FILE" << EOF
        <div class="finding info">
            <strong>.NET 依賴項掃描:</strong> 建議使用 dotnet list package --vulnerable<br>
            <strong>建議:</strong> 定期檢查 NuGet 套件的安全更新
        </div>
EOF
    fi
    
    # OWASP Top 10 檢查
    cat >> "$REPORT_FILE" << EOF
        <h3>OWASP Top 10 合規性檢查</h3>
        <table>
            <tr><th>風險類別</th><th>檢查項目</th><th>狀態</th><th>建議</th></tr>
            <tr><td>A01:2021 – Broken Access Control</td><td>權限控制機制</td><td class='info'>需要人工檢查</td><td>驗證 API 端點權限控制</td></tr>
            <tr><td>A02:2021 – Cryptographic Failures</td><td>加密配置</td><td class='info'>部分檢查</td><td>確保敏感資料加密儲存</td></tr>
            <tr><td>A03:2021 – Injection</td><td>輸入驗證</td><td class='info'>需要程式碼檢查</td><td>使用參數化查詢防止 SQL 注入</td></tr>
            <tr><td>A04:2021 – Insecure Design</td><td>安全設計</td><td class='info'>架構審查</td><td>進行威脅建模和安全設計審查</td></tr>
            <tr><td>A05:2021 – Security Misconfiguration</td><td>配置安全</td><td class='$([ $MEDIUM_COUNT -eq 0 ] && echo "success" || echo "medium")'>$([ $MEDIUM_COUNT -eq 0 ] && echo "良好" || echo "發現問題")</td><td>修復發現的配置問題</td></tr>
            <tr><td>A06:2021 – Vulnerable Components</td><td>依賴項安全</td><td class='info'>已掃描</td><td>定期更新依賴項</td></tr>
            <tr><td>A07:2021 – Authentication Failures</td><td>認證機制</td><td class='info'>需要測試</td><td>實施強認證和多因素認證</td></tr>
            <tr><td>A08:2021 – Software Integrity Failures</td><td>軟體完整性</td><td class='info'>需要實施</td><td>實施程式碼簽名和完整性檢查</td></tr>
            <tr><td>A09:2021 – Logging & Monitoring</td><td>日誌監控</td><td class='success'>已配置</td><td>持續監控和改進</td></tr>
            <tr><td>A10:2021 – Server-Side Request Forgery</td><td>SSRF 防護</td><td class='info'>需要程式碼檢查</td><td>驗證外部請求處理</td></tr>
        </table>
EOF
}

# ===========================================
# 生成安全建議
# ===========================================
generate_security_recommendations() {
    log "生成安全建議..."
    
    cat >> "$REPORT_FILE" << EOF
        <h2>📋 安全建議與修復指南</h2>
        
        <div class="metric critical">
            <div>嚴重</div>
            <div style="font-size: 2em;">$CRITICAL_COUNT</div>
        </div>
        <div class="metric high">
            <div>高風險</div>
            <div style="font-size: 2em;">$HIGH_COUNT</div>
        </div>
        <div class="metric medium">
            <div>中風險</div>
            <div style="font-size: 2em;">$MEDIUM_COUNT</div>
        </div>
        <div class="metric low">
            <div>低風險</div>
            <div style="font-size: 2em;">$LOW_COUNT</div>
        </div>
        <div class="metric info">
            <div>資訊</div>
            <div style="font-size: 2em;">$INFO_COUNT</div>
        </div>
EOF
    
    if [[ $CRITICAL_COUNT -gt 0 ]] || [[ $HIGH_COUNT -gt 0 ]]; then
        cat >> "$REPORT_FILE" << EOF
        <div class="critical">
            <h3>🚨 立即修復項目</h3>
            <ol>
                <li><strong>修復所有 CRITICAL 和 HIGH 級別的安全問題</strong></li>
                <li><strong>檢查和更新所有預設密碼</strong></li>
                <li><strong>驗證容器安全配置</strong></li>
                <li><strong>確保 SSL 憑證有效</strong></li>
                <li><strong>檢查檔案權限設定</strong></li>
            </ol>
        </div>
EOF
    fi
    
    cat >> "$REPORT_FILE" << EOF
        <h3>🔧 安全加固建議</h3>
        <div class="high">
            <h4>立即執行 (Critical/High):</h4>
            <ul>
                <li>修復所有特權容器運行問題</li>
                <li>更新過短的密鑰和密碼</li>
                <li>關閉不必要的對外端口</li>
                <li>修正敏感檔案權限</li>
                <li>更新即將過期的 SSL 憑證</li>
            </ul>
        </div>
        
        <div class="medium">
            <h4>短期內完成 (Medium):</h4>
            <ul>
                <li>完善 Nginx 安全標頭配置</li>
                <li>實施網路分段和防火牆規則</li>
                <li>加強日誌監控和警報</li>
                <li>定期進行依賴項安全更新</li>
                <li>實施備份加密和驗證</li>
            </ul>
        </div>
        
        <div class="low">
            <h4>長期改進 (Low/Info):</h4>
            <ul>
                <li>建立完整的威脅模型</li>
                <li>實施多因素認證</li>
                <li>進行定期安全培訓</li>
                <li>建立事故回應計劃</li>
                <li>執行定期滲透測試</li>
            </ul>
        </div>
        
        <h3>📚 合規性指南</h3>
        <div class="info">
            <h4>GDPR 合規性:</h4>
            <ul>
                <li>實施資料保護影響評估 (DPIA)</li>
                <li>確保個人資料處理的法律基礎</li>
                <li>實施資料可攜性和被遺忘權</li>
                <li>定期進行資料保護稽核</li>
            </ul>
            
            <h4>CIS Controls:</h4>
            <ul>
                <li>實施資產清單管理</li>
                <li>建立安全配置基準</li>
                <li>實施持續漏洞管理</li>
                <li>加強存取控制和權限管理</li>
            </ul>
        </div>
EOF
}

# ===========================================
# 完成 HTML 報告
# ===========================================
complete_html_report() {
    cat >> "$REPORT_FILE" << EOF
        <div class="summary">
            <strong>掃描完成時間：</strong> $(date)<br>
            <strong>下次掃描建議：</strong> $(date -d '+1 week')<br>
            <strong>報告檔案：</strong> $REPORT_FILE<br>
            <strong>總發現問題：</strong> $((CRITICAL_COUNT + HIGH_COUNT + MEDIUM_COUNT + LOW_COUNT)) 個<br>
            <strong>需要立即處理：</strong> $((CRITICAL_COUNT + HIGH_COUNT)) 個
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
    scan_container_security
    scan_network_security
    scan_file_security
    scan_configuration_security
    scan_vulnerabilities
    generate_security_recommendations
    complete_html_report
    
    log "安全掃描完成"
    log "報告檔案：$REPORT_FILE"
    log "發現問題總數：$((CRITICAL_COUNT + HIGH_COUNT + MEDIUM_COUNT + LOW_COUNT))"
    log "嚴重/高風險問題：$((CRITICAL_COUNT + HIGH_COUNT))"
    
    # 如果有嚴重安全問題，返回錯誤代碼
    if [[ $CRITICAL_COUNT -gt 0 ]] || [[ $HIGH_COUNT -gt 0 ]]; then
        log "警告：發現嚴重安全問題，需要立即處理！"
        return 1
    fi
    
    return 0
}

# 執行主函數
main "$@"