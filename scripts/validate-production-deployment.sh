#!/bin/bash

# ADHD 生產力系統 - 生產環境部署驗證腳本
# 功能：全面驗證生產環境配置和部署狀態

set -euo pipefail

# ===========================================
# 配置變數
# ===========================================
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOG_FILE="/logs/deployment-validation.log"
VALIDATION_REPORT="/reports/deployment-validation-$(date +%Y%m%d_%H%M%S).txt"

# 驗證項目計數器
TOTAL_CHECKS=0
PASSED_CHECKS=0
FAILED_CHECKS=0
WARNING_CHECKS=0

# ===========================================
# 日誌函數
# ===========================================
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

check_result() {
    local check_name="$1"
    local result="$2"
    local details="$3"
    
    ((TOTAL_CHECKS++))
    
    case $result in
        "PASS")
            ((PASSED_CHECKS++))
            log "✅ PASS: $check_name - $details"
            echo "✅ PASS: $check_name - $details" >> "$VALIDATION_REPORT"
            ;;
        "FAIL")
            ((FAILED_CHECKS++))
            log "❌ FAIL: $check_name - $details"
            echo "❌ FAIL: $check_name - $details" >> "$VALIDATION_REPORT"
            ;;
        "WARN")
            ((WARNING_CHECKS++))
            log "⚠️  WARN: $check_name - $details"
            echo "⚠️  WARN: $check_name - $details" >> "$VALIDATION_REPORT"
            ;;
    esac
}

# ===========================================
# 初始化驗證報告
# ===========================================
init_validation_report() {
    cat > "$VALIDATION_REPORT" << EOF
ADHD 生產力系統 - 生產環境部署驗證報告
=============================================

驗證時間: $(date)
驗證版本: 1.0.0
執行者: $(whoami)

===== 驗證結果摘要 =====

EOF
}

# ===========================================
# 環境變數驗證
# ===========================================
validate_environment_variables() {
    log "開始驗證環境變數..."
    
    echo "" >> "$VALIDATION_REPORT"
    echo "=== 環境變數驗證 ===" >> "$VALIDATION_REPORT"
    
    # 檢查生產環境配置檔案
    if [[ -f ".env.production" ]]; then
        check_result "生產環境配置檔案" "PASS" ".env.production 檔案存在"
        
        # 檢查必要變數
        local required_vars=(
            "ASPNETCORE_ENVIRONMENT"
            "POSTGRES_DB"
            "POSTGRES_USER"
            "POSTGRES_PASSWORD"
            "JWT_SECRET_KEY"
            "REDIS_PASSWORD"
        )
        
        for var in "${required_vars[@]}"; do
            if grep -q "^${var}=" .env.production; then
                local value=$(grep "^${var}=" .env.production | cut -d'=' -f2)
                if [[ -n "$value" ]]; then
                    check_result "環境變數 $var" "PASS" "已設定且非空"
                else
                    check_result "環境變數 $var" "FAIL" "已設定但為空值"
                fi
            else
                check_result "環境變數 $var" "FAIL" "未設定"
            fi
        done
        
        # 檢查環境設定
        if grep -q "ASPNETCORE_ENVIRONMENT=Production" .env.production; then
            check_result "生產環境設定" "PASS" "ASPNETCORE_ENVIRONMENT=Production"
        else
            check_result "生產環境設定" "FAIL" "ASPNETCORE_ENVIRONMENT 不是 Production"
        fi
        
        # 檢查密鑰強度
        local jwt_key=$(grep "JWT_SECRET_KEY=" .env.production | cut -d'=' -f2)
        if [[ ${#jwt_key} -ge 32 ]]; then
            check_result "JWT 密鑰強度" "PASS" "長度 ${#jwt_key} 字符 (≥32)"
        else
            check_result "JWT 密鑰強度" "FAIL" "長度 ${#jwt_key} 字符 (<32)"
        fi
        
    else
        check_result "生產環境配置檔案" "FAIL" ".env.production 檔案不存在"
    fi
}

# ===========================================
# Docker 配置驗證
# ===========================================
validate_docker_configuration() {
    log "開始驗證 Docker 配置..."
    
    echo "" >> "$VALIDATION_REPORT"
    echo "=== Docker 配置驗證 ===" >> "$VALIDATION_REPORT"
    
    # 檢查 Docker 是否運行
    if docker version > /dev/null 2>&1; then
        check_result "Docker 服務" "PASS" "Docker 守護程序運行正常"
    else
        check_result "Docker 服務" "FAIL" "Docker 守護程序未運行"
        return
    fi
    
    # 檢查 Docker Compose
    if docker-compose version > /dev/null 2>&1; then
        local compose_version=$(docker-compose version --short)
        check_result "Docker Compose" "PASS" "版本 $compose_version"
    else
        check_result "Docker Compose" "FAIL" "Docker Compose 未安裝或無法執行"
    fi
    
    # 檢查生產配置檔案
    if [[ -f "docker-compose.production.yml" ]]; then
        check_result "生產 Compose 檔案" "PASS" "docker-compose.production.yml 存在"
        
        # 驗證配置語法
        if docker-compose -f docker-compose.production.yml config > /dev/null 2>&1; then
            check_result "Compose 檔案語法" "PASS" "配置語法正確"
        else
            check_result "Compose 檔案語法" "FAIL" "配置語法錯誤"
        fi
    else
        check_result "生產 Compose 檔案" "FAIL" "docker-compose.production.yml 不存在"
    fi
    
    # 檢查映像是否存在
    local images=("postgres:16-alpine" "redis:7-alpine" "nginx:alpine")
    for image in "${images[@]}"; do
        if docker image inspect "$image" > /dev/null 2>&1; then
            check_result "Docker 映像 $image" "PASS" "映像已下載"
        else
            check_result "Docker 映像 $image" "WARN" "映像未下載，首次啟動時會自動下載"
        fi
    done
}

# ===========================================
# 網路配置驗證
# ===========================================
validate_network_configuration() {
    log "開始驗證網路配置..."
    
    echo "" >> "$VALIDATION_REPORT"
    echo "=== 網路配置驗證 ===" >> "$VALIDATION_REPORT"
    
    # 檢查 Nginx 配置檔案
    if [[ -f "nginx/nginx.production.conf" ]]; then
        check_result "Nginx 生產配置" "PASS" "nginx.production.conf 存在"
        
        # 檢查配置語法 (如果 nginx 可用)
        if command -v nginx > /dev/null 2>&1; then
            if nginx -t -c "$(pwd)/nginx/nginx.production.conf" > /dev/null 2>&1; then
                check_result "Nginx 配置語法" "PASS" "配置語法正確"
            else
                check_result "Nginx 配置語法" "FAIL" "配置語法錯誤"
            fi
        else
            check_result "Nginx 配置語法" "WARN" "無法驗證，nginx 未安裝"
        fi
        
        # 檢查安全標頭
        local security_headers=("X-Frame-Options" "X-Content-Type-Options" "Strict-Transport-Security")
        for header in "${security_headers[@]}"; do
            if grep -q "$header" nginx/nginx.production.conf; then
                check_result "安全標頭 $header" "PASS" "已配置"
            else
                check_result "安全標頭 $header" "WARN" "未配置"
            fi
        done
    else
        check_result "Nginx 生產配置" "FAIL" "nginx.production.conf 不存在"
    fi
    
    # 檢查端口可用性
    local ports=(80 443 5432 6379 9090 3000)
    for port in "${ports[@]}"; do
        if ss -tuln | grep -q ":$port "; then
            check_result "端口 $port 可用性" "WARN" "端口已被佔用"
        else
            check_result "端口 $port 可用性" "PASS" "端口可用"
        fi
    done
}

# ===========================================
# SSL 憑證驗證
# ===========================================
validate_ssl_certificates() {
    log "開始驗證 SSL 憑證..."
    
    echo "" >> "$VALIDATION_REPORT"
    echo "=== SSL 憑證驗證 ===" >> "$VALIDATION_REPORT"
    
    # 檢查憑證目錄
    if [[ -d "certs" ]]; then
        check_result "憑證目錄" "PASS" "certs 目錄存在"
        
        # 檢查憑證檔案
        if [[ -f "certs/adhd-productivity.crt" ]]; then
            check_result "SSL 憑證檔案" "PASS" "憑證檔案存在"
            
            # 檢查憑證有效期
            if command -v openssl > /dev/null 2>&1; then
                local expiry=$(openssl x509 -in certs/adhd-productivity.crt -noout -enddate 2>/dev/null | cut -d= -f2)
                if [[ -n "$expiry" ]]; then
                    local days_until_expiry=$(( ($(date -d "$expiry" +%s) - $(date +%s)) / 86400 )) 2>/dev/null || echo "N/A"
                    
                    if [[ "$days_until_expiry" =~ ^[0-9]+$ ]]; then
                        if [[ $days_until_expiry -gt 30 ]]; then
                            check_result "SSL 憑證有效期" "PASS" "還有 $days_until_expiry 天到期"
                        elif [[ $days_until_expiry -gt 7 ]]; then
                            check_result "SSL 憑證有效期" "WARN" "還有 $days_until_expiry 天到期"
                        else
                            check_result "SSL 憑證有效期" "FAIL" "還有 $days_until_expiry 天到期"
                        fi
                    else
                        check_result "SSL 憑證有效期" "WARN" "無法解析到期日期"
                    fi
                else
                    check_result "SSL 憑證有效期" "FAIL" "無法讀取憑證"
                fi
            else
                check_result "SSL 憑證有效期" "WARN" "無法驗證，openssl 未安裝"
            fi
        else
            check_result "SSL 憑證檔案" "FAIL" "憑證檔案不存在"
        fi
        
        # 檢查私鑰檔案
        if [[ -f "certs/adhd-productivity.key" ]]; then
            check_result "SSL 私鑰檔案" "PASS" "私鑰檔案存在"
            
            # 檢查私鑰權限
            local key_perms=$(stat -c "%a" certs/adhd-productivity.key 2>/dev/null || echo "000")
            if [[ "$key_perms" == "600" ]] || [[ "$key_perms" == "400" ]]; then
                check_result "SSL 私鑰權限" "PASS" "權限 $key_perms (安全)"
            else
                check_result "SSL 私鑰權限" "FAIL" "權限 $key_perms (不安全)"
            fi
        else
            check_result "SSL 私鑰檔案" "FAIL" "私鑰檔案不存在"
        fi
    else
        check_result "憑證目錄" "WARN" "certs 目錄不存在，如使用自動憑證可忽略"
    fi
}

# ===========================================
# 備份配置驗證
# ===========================================
validate_backup_configuration() {
    log "開始驗證備份配置..."
    
    echo "" >> "$VALIDATION_REPORT"
    echo "=== 備份配置驗證 ===" >> "$VALIDATION_REPORT"
    
    # 檢查備份腳本
    local backup_scripts=("backup-database.sh" "backup-redis.sh" "restore-database.sh")
    for script in "${backup_scripts[@]}"; do
        if [[ -f "scripts/backup/$script" ]]; then
            check_result "備份腳本 $script" "PASS" "腳本檔案存在"
            
            # 檢查執行權限
            if [[ -x "scripts/backup/$script" ]]; then
                check_result "備份腳本權限 $script" "PASS" "具有執行權限"
            else
                check_result "備份腳本權限 $script" "FAIL" "缺少執行權限"
            fi
        else
            check_result "備份腳本 $script" "FAIL" "腳本檔案不存在"
        fi
    done
    
    # 檢查備份目錄
    if [[ -d "backups" ]]; then
        check_result "備份目錄" "PASS" "backups 目錄存在"
        
        # 檢查目錄權限
        local backup_perms=$(stat -c "%a" backups 2>/dev/null || echo "000")
        if [[ "$backup_perms" =~ ^[7][0-7][0-7]$ ]]; then
            check_result "備份目錄權限" "PASS" "權限 $backup_perms (所有者可寫)"
        else
            check_result "備份目錄權限" "WARN" "權限 $backup_perms (檢查是否適當)"
        fi
    else
        check_result "備份目錄" "WARN" "backups 目錄不存在，將在首次備份時創建"
    fi
    
    # 檢查 crontab 配置
    if [[ -f "scripts/backup/crontab" ]]; then
        check_result "備份排程配置" "PASS" "crontab 配置檔案存在"
    else
        check_result "備份排程配置" "WARN" "crontab 配置檔案不存在"
    fi
}

# ===========================================
# 監控配置驗證
# ===========================================
validate_monitoring_configuration() {
    log "開始驗證監控配置..."
    
    echo "" >> "$VALIDATION_REPORT"
    echo "=== 監控配置驗證 ===" >> "$VALIDATION_REPORT"
    
    # 檢查 Prometheus 配置
    if [[ -f "monitoring/prometheus/prometheus.production.yml" ]]; then
        check_result "Prometheus 配置" "PASS" "配置檔案存在"
        
        # 檢查配置語法（基本檢查）
        if grep -q "global:" monitoring/prometheus/prometheus.production.yml; then
            check_result "Prometheus 配置語法" "PASS" "包含必要的 global 配置"
        else
            check_result "Prometheus 配置語法" "FAIL" "配置格式可能錯誤"
        fi
    else
        check_result "Prometheus 配置" "FAIL" "prometheus.production.yml 不存在"
    fi
    
    # 檢查警報規則
    if [[ -f "monitoring/prometheus/rules/adhd-system-alerts.yml" ]]; then
        check_result "Prometheus 警報規則" "PASS" "警報規則檔案存在"
    else
        check_result "Prometheus 警報規則" "WARN" "警報規則檔案不存在"
    fi
    
    # 檢查 Grafana 配置
    if [[ -f "monitoring/grafana/datasources/prometheus.yml" ]]; then
        check_result "Grafana 資料源配置" "PASS" "配置檔案存在"
    else
        check_result "Grafana 資料源配置" "WARN" "資料源配置檔案不存在"
    fi
    
    # 檢查 Loki 配置
    if [[ -f "monitoring/loki/loki.yml" ]]; then
        check_result "Loki 日誌配置" "PASS" "配置檔案存在"
    else
        check_result "Loki 日誌配置" "WARN" "日誌配置檔案不存在"
    fi
}

# ===========================================
# 安全配置驗證
# ===========================================
validate_security_configuration() {
    log "開始驗證安全配置..."
    
    echo "" >> "$VALIDATION_REPORT"
    echo "=== 安全配置驗證 ===" >> "$VALIDATION_REPORT"
    
    # 檢查安全掃描腳本
    if [[ -f "scripts/security/security-scanner.sh" ]]; then
        check_result "安全掃描腳本" "PASS" "腳本檔案存在"
        
        if [[ -x "scripts/security/security-scanner.sh" ]]; then
            check_result "安全掃描腳本權限" "PASS" "具有執行權限"
        else
            check_result "安全掃描腳本權限" "FAIL" "缺少執行權限"
        fi
    else
        check_result "安全掃描腳本" "WARN" "安全掃描腳本不存在"
    fi
    
    # 檢查敏感檔案權限
    local sensitive_files=(".env.production")
    for file in "${sensitive_files[@]}"; do
        if [[ -f "$file" ]]; then
            local file_perms=$(stat -c "%a" "$file" 2>/dev/null || echo "000")
            if [[ "$file_perms" == "600" ]] || [[ "$file_perms" == "400" ]]; then
                check_result "敏感檔案權限 $file" "PASS" "權限 $file_perms (安全)"
            else
                check_result "敏感檔案權限 $file" "WARN" "權限 $file_perms (建議設為 600)"
            fi
        fi
    done
    
    # 檢查是否有不應該存在的敏感檔案
    local forbidden_files=(".env" "id_rsa" "*.key" "*.pem")
    for pattern in "${forbidden_files[@]}"; do
        if ls $pattern 2>/dev/null | grep -v "certs/" | grep -q .; then
            check_result "敏感檔案檢查 $pattern" "WARN" "發現可能的敏感檔案"
        else
            check_result "敏感檔案檢查 $pattern" "PASS" "未發現意外的敏感檔案"
        fi
    done
}

# ===========================================
# 系統資源檢查
# ===========================================
validate_system_resources() {
    log "開始檢查系統資源..."
    
    echo "" >> "$VALIDATION_REPORT"
    echo "=== 系統資源檢查 ===" >> "$VALIDATION_REPORT"
    
    # 檢查磁碟空間
    local disk_usage=$(df / | tail -1 | awk '{print $5}' | sed 's/%//')
    if [[ $disk_usage -lt 70 ]]; then
        check_result "磁碟空間" "PASS" "使用率 $disk_usage% (良好)"
    elif [[ $disk_usage -lt 85 ]]; then
        check_result "磁碟空間" "WARN" "使用率 $disk_usage% (注意)"
    else
        check_result "磁碟空間" "FAIL" "使用率 $disk_usage% (不足)"
    fi
    
    # 檢查記憶體
    local memory_info=$(free | grep Mem)
    local total_mem=$(echo $memory_info | awk '{print $2}')
    local available_mem=$(echo $memory_info | awk '{print $7}')
    local memory_usage=$(echo "scale=1; ($total_mem - $available_mem) * 100 / $total_mem" | bc)
    
    if (( $(echo "$memory_usage < 70" | bc -l) )); then
        check_result "記憶體使用率" "PASS" "使用率 $memory_usage% (良好)"
    elif (( $(echo "$memory_usage < 85" | bc -l) )); then
        check_result "記憶體使用率" "WARN" "使用率 $memory_usage% (注意)"
    else
        check_result "記憶體使用率" "FAIL" "使用率 $memory_usage% (過高)"
    fi
    
    # 檢查 CPU 核心數
    local cpu_cores=$(nproc)
    if [[ $cpu_cores -ge 2 ]]; then
        check_result "CPU 核心數" "PASS" "$cpu_cores 核心 (足夠)"
    else
        check_result "CPU 核心數" "WARN" "$cpu_cores 核心 (建議至少 2 核心)"
    fi
}

# ===========================================
# CI/CD 配置驗證
# ===========================================
validate_cicd_configuration() {
    log "開始驗證 CI/CD 配置..."
    
    echo "" >> "$VALIDATION_REPORT"
    echo "=== CI/CD 配置驗證 ===" >> "$VALIDATION_REPORT"
    
    # 檢查 GitHub Actions 配置
    if [[ -f ".github/workflows/ci-cd-production.yml" ]]; then
        check_result "CI/CD 工作流程" "PASS" "GitHub Actions 配置存在"
        
        # 檢查基本語法
        if grep -q "name:" .github/workflows/ci-cd-production.yml; then
            check_result "CI/CD 配置語法" "PASS" "基本語法正確"
        else
            check_result "CI/CD 配置語法" "FAIL" "配置格式可能錯誤"
        fi
    else
        check_result "CI/CD 工作流程" "WARN" "GitHub Actions 配置不存在"
    fi
    
    # 檢查自定義 Actions
    local custom_actions=("blue-green-deploy" "deploy" "backup")
    for action in "${custom_actions[@]}"; do
        if [[ -f ".github/actions/$action/action.yml" ]]; then
            check_result "自定義 Action $action" "PASS" "配置檔案存在"
        else
            check_result "自定義 Action $action" "WARN" "配置檔案不存在"
        fi
    done
}

# ===========================================
# 生成最終報告
# ===========================================
generate_final_report() {
    log "生成最終驗證報告..."
    
    local success_rate=$(echo "scale=1; $PASSED_CHECKS * 100 / $TOTAL_CHECKS" | bc)
    
    cat >> "$VALIDATION_REPORT" << EOF

===== 驗證結果統計 =====

總檢查項目: $TOTAL_CHECKS
通過項目: $PASSED_CHECKS
失敗項目: $FAILED_CHECKS
警告項目: $WARNING_CHECKS

成功率: $success_rate%

===== 部署建議 =====

EOF
    
    if [[ $FAILED_CHECKS -eq 0 ]]; then
        cat >> "$VALIDATION_REPORT" << EOF
🎉 恭喜！所有關鍵檢查項目都已通過。
   系統已準備好進行生產環境部署。

建議操作：
1. 在進行實際部署前，請再次確認所有警告項目
2. 確保生產環境的外部依賴（域名、SSL憑證等）已準備就緒
3. 通知相關團隊成員準備進行部署

EOF
    elif [[ $FAILED_CHECKS -le 2 ]]; then
        cat >> "$VALIDATION_REPORT" << EOF
⚠️  發現少量問題，建議修復後再進行部署。

必須修復的問題：
$(grep "❌ FAIL" "$VALIDATION_REPORT")

修復完成後，請重新執行此驗證腳本。

EOF
    else
        cat >> "$VALIDATION_REPORT" << EOF
🚫 發現多個嚴重問題，不建議進行部署。

必須修復的問題：
$(grep "❌ FAIL" "$VALIDATION_REPORT")

請修復所有失敗項目後，重新執行驗證。

EOF
    fi
    
    cat >> "$VALIDATION_REPORT" << EOF
===== 快速修復指南 =====

常見問題修復方法：

1. 環境變數問題：
   - 複製 .env.example 到 .env.production
   - 修改其中的值為生產環境適用的配置

2. 檔案權限問題：
   - chmod 600 .env.production
   - chmod 600 certs/*.key
   - chmod +x scripts/*/*.sh

3. 缺失目錄問題：
   - mkdir -p backups logs certs
   - mkdir -p monitoring/{prometheus,grafana,loki}

4. Docker 相關問題：
   - 確保 Docker 和 Docker Compose 已正確安裝
   - 檢查 docker-compose.production.yml 語法

===== 聯絡資訊 =====

如需協助，請聯絡：
- 技術負責人：[聯絡資訊]
- 系統管理員：[聯絡資訊]
- DevOps 團隊：[聯絡資訊]

報告生成時間: $(date)
驗證腳本版本: 1.0.0

EOF
}

# ===========================================
# 主執行流程
# ===========================================
main() {
    log "開始生產環境部署驗證..."
    
    init_validation_report
    
    validate_environment_variables
    validate_docker_configuration
    validate_network_configuration
    validate_ssl_certificates
    validate_backup_configuration
    validate_monitoring_configuration
    validate_security_configuration
    validate_system_resources
    validate_cicd_configuration
    
    generate_final_report
    
    log "驗證完成！"
    log "總檢查項目: $TOTAL_CHECKS"
    log "通過: $PASSED_CHECKS, 失敗: $FAILED_CHECKS, 警告: $WARNING_CHECKS"
    log "詳細報告: $VALIDATION_REPORT"
    
    # 輸出摘要到控制台
    echo ""
    echo "============================================"
    echo "ADHD 生產力系統 - 部署驗證結果"
    echo "============================================"
    echo "總檢查項目: $TOTAL_CHECKS"
    echo "✅ 通過: $PASSED_CHECKS"
    echo "❌ 失敗: $FAILED_CHECKS"
    echo "⚠️  警告: $WARNING_CHECKS"
    echo ""
    echo "詳細報告: $VALIDATION_REPORT"
    echo "============================================"
    
    # 根據結果返回適當的退出代碼
    if [[ $FAILED_CHECKS -eq 0 ]]; then
        echo "🎉 驗證通過！系統已準備好進行生產部署。"
        return 0
    elif [[ $FAILED_CHECKS -le 2 ]]; then
        echo "⚠️  發現少量問題，建議修復後再部署。"
        return 1
    else
        echo "🚫 發現嚴重問題，不建議進行部署。"
        return 2
    fi
}

# 執行主函數
main "$@"