#!/bin/bash

# ADHD 生產力系統 - SSL 憑證管理器
# 統一管理開發和生產環境的 SSL 配置

set -e

# 顏色定義
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# 配置
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
CERT_DIR="${PROJECT_ROOT}/security/certs"
NGINX_CONF_DIR="${PROJECT_ROOT}/nginx/conf.d"

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

log_debug() {
    echo -e "${PURPLE}[DEBUG]${NC} $1"
}

# 顯示橫幅
show_banner() {
    echo -e "${CYAN}"
    echo "╔══════════════════════════════════════════════════════════╗"
    echo "║              ADHD 生產力系統 SSL 管理器                 ║"
    echo "║                                                          ║"
    echo "║  🔐 統一管理開發和生產環境的 SSL 憑證配置               ║"
    echo "╚══════════════════════════════════════════════════════════╝"
    echo -e "${NC}"
}

# 檢查依賴
check_dependencies() {
    local deps=("openssl" "docker" "docker-compose")
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

# 創建開發環境自簽名證書
setup_dev_ssl() {
    log_info "設置開發環境 SSL 證書..."
    
    mkdir -p "$CERT_DIR"
    
    if [ -f "$CERT_DIR/adhd-dev.crt" ] && [ -f "$CERT_DIR/adhd-dev.key" ]; then
        log_warning "開發證書已存在"
        read -p "是否要重新生成？(y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            log_info "跳過證書生成"
            setup_nginx_dev
            return
        fi
    fi
    
    # 使用 Docker 容器生成證書
    log_info "使用 Docker 容器生成自簽名證書..."
    docker run --rm \
        -v "$CERT_DIR:/certs" \
        -v "$SCRIPT_DIR:/scripts:ro" \
        alpine:latest \
        sh -c "
            apk add --no-cache openssl && \
            chmod +x /scripts/generate-dev-certs.sh && \
            /scripts/generate-dev-certs.sh
        "
    
    if [ $? -eq 0 ]; then
        log_success "開發環境 SSL 證書生成完成"
        setup_nginx_dev
    else
        log_error "SSL 證書生成失敗"
        exit 1
    fi
}

# 設置 Nginx 開發配置
setup_nginx_dev() {
    log_info "配置 Nginx 開發環境..."
    
    # 啟用開發配置，禁用生產配置
    if [ -f "$NGINX_CONF_DIR/ssl-production.conf" ]; then
        mv "$NGINX_CONF_DIR/ssl-production.conf" "$NGINX_CONF_DIR/ssl-production.conf.disabled" 2>/dev/null || true
    fi
    
    if [ -f "$NGINX_CONF_DIR/ssl-development.conf.disabled" ]; then
        mv "$NGINX_CONF_DIR/ssl-development.conf.disabled" "$NGINX_CONF_DIR/ssl-development.conf"
    fi
    
    log_success "開發環境配置完成"
    echo ""
    echo "🌐 開發環境訪問地址:"
    echo "   HTTP:  http://localhost"
    echo "   HTTPS: https://localhost (會顯示安全警告)"
    echo ""
    echo "⚠️  注意: 請將 $CERT_DIR/adhd-dev.crt 添加到瀏覽器的受信任證書中"
}

# 設置生產環境 SSL
setup_prod_ssl() {
    log_info "設置生產環境 SSL 證書..."
    
    # 檢查環境變數
    if [ -z "$DOMAIN" ] || [ -z "$CERTBOT_EMAIL" ]; then
        log_error "請設置必要的環境變數:"
        echo "  export DOMAIN=your-domain.com"
        echo "  export CERTBOT_EMAIL=admin@your-domain.com"
        exit 1
    fi
    
    log_info "域名: $DOMAIN"
    log_info "郵箱: $CERTBOT_EMAIL"
    
    # 檢查域名 DNS 解析
    log_info "檢查域名 DNS 解析..."
    if ! dig +short "$DOMAIN" | grep -E '^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$' > /dev/null; then
        log_warning "無法解析域名 $DOMAIN，請確保 DNS 配置正確"
        read -p "是否繼續？(y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            exit 1
        fi
    fi
    
    # 啟動 Certbot 容器
    log_info "啟動 Let's Encrypt 證書申請..."
    
    cd "$PROJECT_ROOT"
    
    # 先啟動基本服務
    docker-compose up -d adhd-nginx
    
    # 等待 Nginx 就緒
    sleep 10
    
    # 啟動 Certbot
    docker-compose --profile ssl up -d adhd-certbot
    
    # 監控證書申請過程
    log_info "監控證書申請過程..."
    docker-compose logs -f adhd-certbot &
    LOGS_PID=$!
    
    # 等待證書申請完成
    local retry_count=0
    local max_retries=30
    
    while [ $retry_count -lt $max_retries ]; do
        if docker-compose exec -T adhd-certbot test -f "/etc/letsencrypt/live/$DOMAIN/fullchain.pem"; then
            log_success "SSL 證書獲取成功"
            kill $LOGS_PID 2>/dev/null || true
            break
        fi
        
        sleep 10
        retry_count=$((retry_count + 1))
        log_debug "等待證書申請完成... ($retry_count/$max_retries)"
    done
    
    if [ $retry_count -eq $max_retries ]; then
        log_error "SSL 證書申請超時"
        kill $LOGS_PID 2>/dev/null || true
        exit 1
    fi
    
    setup_nginx_prod
}

# 設置 Nginx 生產配置
setup_nginx_prod() {
    log_info "配置 Nginx 生產環境..."
    
    # 啟用生產配置，禁用開發配置
    if [ -f "$NGINX_CONF_DIR/ssl-development.conf" ]; then
        mv "$NGINX_CONF_DIR/ssl-development.conf" "$NGINX_CONF_DIR/ssl-development.conf.disabled" 2>/dev/null || true
    fi
    
    if [ -f "$NGINX_CONF_DIR/ssl-production.conf.disabled" ]; then
        mv "$NGINX_CONF_DIR/ssl-production.conf.disabled" "$NGINX_CONF_DIR/ssl-production.conf"
    fi
    
    # 重載 Nginx 配置
    log_info "重載 Nginx 配置..."
    docker-compose exec adhd-nginx nginx -t && docker-compose exec adhd-nginx nginx -s reload
    
    log_success "生產環境配置完成"
    echo ""
    echo "🌐 生產環境訪問地址:"
    echo "   HTTP:  http://$DOMAIN (會重導向到 HTTPS)"
    echo "   HTTPS: https://$DOMAIN"
    echo ""
    echo "🔒 SSL 證書將自動更新"
}

# 檢查證書狀態
check_ssl_status() {
    log_info "檢查 SSL 證書狀態..."
    
    echo ""
    echo "📊 證書狀態報告"
    echo "=================="
    
    # 檢查開發證書
    if [ -f "$CERT_DIR/adhd-dev.crt" ]; then
        echo ""
        echo "🔧 開發環境證書:"
        echo "   路徑: $CERT_DIR/adhd-dev.crt"
        
        local dev_expiry
        dev_expiry=$(openssl x509 -enddate -noout -in "$CERT_DIR/adhd-dev.crt" 2>/dev/null | cut -d= -f2)
        if [ -n "$dev_expiry" ]; then
            echo "   過期時間: $dev_expiry"
            
            local dev_days
            dev_days=$(( ($(date -d "$dev_expiry" +%s) - $(date +%s)) / 86400 ))
            echo "   剩餘天數: $dev_days 天"
            
            if [ $dev_days -lt 30 ]; then
                echo "   ⚠️  狀態: 即將過期"
            else
                echo "   ✅ 狀態: 正常"
            fi
        fi
    else
        echo ""
        echo "🔧 開發環境證書: ❌ 未找到"
    fi
    
    # 檢查生產證書
    if docker-compose ps adhd-certbot | grep -q "Up"; then
        echo ""
        echo "🌐 生產環境證書:"
        
        local domain_from_env
        domain_from_env=$(docker-compose exec -T adhd-certbot printenv DOMAIN 2>/dev/null || echo "unknown")
        echo "   域名: $domain_from_env"
        
        if docker-compose exec -T adhd-certbot test -f "/etc/letsencrypt/live/$domain_from_env/fullchain.pem" 2>/dev/null; then
            local prod_expiry
            prod_expiry=$(docker-compose exec -T adhd-certbot openssl x509 -enddate -noout -in "/etc/letsencrypt/live/$domain_from_env/fullchain.pem" 2>/dev/null | cut -d= -f2)
            
            if [ -n "$prod_expiry" ]; then
                echo "   過期時間: $prod_expiry"
                
                local prod_days
                prod_days=$(( ($(date -d "$prod_expiry" +%s) - $(date +%s)) / 86400 ))
                echo "   剩餘天數: $prod_days 天"
                
                if [ $prod_days -lt 30 ]; then
                    echo "   ⚠️  狀態: 即將過期"
                else
                    echo "   ✅ 狀態: 正常"
                fi
            fi
        else
            echo "   ❌ 狀態: 證書文件不存在"
        fi
    else
        echo ""
        echo "🌐 生產環境證書: ❌ Certbot 容器未運行"
    fi
    
    echo ""
}

# 更新證書
renew_certificates() {
    log_info "更新 SSL 證書..."
    
    # 更新開發證書
    if [ -f "$CERT_DIR/adhd-dev.crt" ]; then
        local dev_days
        dev_days=$(( ($(date -d "$(openssl x509 -enddate -noout -in "$CERT_DIR/adhd-dev.crt" | cut -d= -f2)" +%s) - $(date +%s)) / 86400 ))
        
        if [ $dev_days -lt 30 ]; then
            log_info "更新開發環境證書..."
            setup_dev_ssl
        else
            log_info "開發環境證書尚未到期 (剩餘 $dev_days 天)"
        fi
    fi
    
    # 更新生產證書
    if docker-compose ps adhd-certbot | grep -q "Up"; then
        log_info "更新生產環境證書..."
        docker-compose exec adhd-certbot certbot renew
        
        if [ $? -eq 0 ]; then
            log_success "生產環境證書更新完成"
            # 重載 Nginx
            docker-compose exec adhd-nginx nginx -s reload
        else
            log_warning "生產環境證書無需更新或更新失敗"
        fi
    else
        log_warning "Certbot 容器未運行，跳過生產環境證書更新"
    fi
}

# 清理證書
cleanup_certificates() {
    log_warning "清理 SSL 證書..."
    
    read -p "⚠️  確定要清理所有 SSL 證書嗎？這將刪除所有證書文件。(y/N): " -n 1 -r
    echo
    
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        # 清理開發證書
        if [ -d "$CERT_DIR" ]; then
            log_info "清理開發環境證書..."
            rm -rf "$CERT_DIR"/*
            log_success "開發證書清理完成"
        fi
        
        # 清理生產證書
        if docker-compose ps adhd-certbot | grep -q "Up"; then
            log_info "清理生產環境證書..."
            docker-compose down adhd-certbot
            docker volume rm adhd_certbot_data 2>/dev/null || true
            log_success "生產證書清理完成"
        fi
        
        log_success "所有證書已清理"
    else
        log_info "已取消清理操作"
    fi
}

# 顯示幫助
show_help() {
    echo "ADHD 生產力系統 SSL 憑證管理器"
    echo ""
    echo "使用方法:"
    echo "  $0 [命令] [選項]"
    echo ""
    echo "命令:"
    echo "  dev         設置開發環境 SSL (自簽名證書)"
    echo "  prod        設置生產環境 SSL (Let's Encrypt)"
    echo "  status      檢查證書狀態"
    echo "  renew       更新證書"
    echo "  cleanup     清理所有證書"
    echo "  help        顯示此幫助信息"
    echo ""
    echo "範例:"
    echo "  # 設置開發環境"
    echo "  $0 dev"
    echo ""
    echo "  # 設置生產環境"
    echo "  export DOMAIN=adhd-productivity.com"
    echo "  export CERTBOT_EMAIL=admin@adhd-productivity.com"
    echo "  $0 prod"
    echo ""
    echo "  # 檢查證書狀態"
    echo "  $0 status"
    echo ""
    echo "環境變數 (生產環境):"
    echo "  DOMAIN           要申請證書的域名"
    echo "  CERTBOT_EMAIL    Let's Encrypt 帳戶郵箱"
    echo ""
}

# 主函數
main() {
    show_banner
    check_dependencies
    
    case "${1:-help}" in
        "dev"|"development")
            setup_dev_ssl
            ;;
        "prod"|"production")
            setup_prod_ssl
            ;;
        "status")
            check_ssl_status
            ;;
        "renew"|"update")
            renew_certificates
            ;;
        "cleanup"|"clean")
            cleanup_certificates
            ;;
        "help"|"--help"|"-h"|*)
            show_help
            ;;
    esac
}

main "$@"