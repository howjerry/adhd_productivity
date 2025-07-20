#!/bin/bash

# ADHD 生產力系統 - Let's Encrypt SSL 證書管理腳本
# 此腳本用於生產環境的 SSL 證書自動化管理

set -e

# 顏色定義
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 配置變數
DOMAIN="${DOMAIN:-your-domain.com}"
EMAIL="${EMAIL:-admin@your-domain.com}"
CERT_DIR="/etc/letsencrypt/live/$DOMAIN"
NGINX_CERT_DIR="/etc/nginx/certs"
WEBROOT="/var/www/certbot"

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

# 檢查必要的環境變數
check_environment() {
    log_info "檢查環境配置..."
    
    if [ "$DOMAIN" = "your-domain.com" ]; then
        log_error "請設置 DOMAIN 環境變數"
        echo "例如: export DOMAIN=adhd-productivity.com"
        exit 1
    fi
    
    if [ "$EMAIL" = "admin@your-domain.com" ]; then
        log_error "請設置 EMAIL 環境變數"
        echo "例如: export EMAIL=admin@adhd-productivity.com"
        exit 1
    fi
    
    log_success "環境配置檢查完成"
    echo "  域名: $DOMAIN"
    echo "  郵箱: $EMAIL"
}

# 安裝 Certbot
install_certbot() {
    log_info "安裝 Certbot..."
    
    if command -v certbot &> /dev/null; then
        log_success "Certbot 已安裝"
        return
    fi
    
    # 根據系統類型安裝 Certbot
    if [ -f /etc/debian_version ]; then
        apt-get update
        apt-get install -y certbot python3-certbot-nginx
    elif [ -f /etc/redhat-release ]; then
        yum install -y certbot python3-certbot-nginx
    else
        log_error "不支持的系統類型，請手動安裝 Certbot"
        exit 1
    fi
    
    log_success "Certbot 安裝完成"
}

# 創建必要目錄
create_directories() {
    log_info "創建必要目錄..."
    
    mkdir -p "$WEBROOT"
    mkdir -p "$NGINX_CERT_DIR"
    mkdir -p "/var/log/letsencrypt"
    
    log_success "目錄創建完成"
}

# 獲取 SSL 證書
obtain_certificate() {
    log_info "獲取 SSL 證書..."
    
    # 檢查是否已存在證書
    if [ -f "$CERT_DIR/fullchain.pem" ]; then
        log_warning "證書已存在，將進行更新..."
        renew_certificate
        return
    fi
    
    # 首次獲取證書
    certbot certonly \
        --webroot \
        --webroot-path="$WEBROOT" \
        --email "$EMAIL" \
        --agree-tos \
        --no-eff-email \
        --force-renewal \
        -d "$DOMAIN" \
        -d "www.$DOMAIN"
    
    if [ $? -eq 0 ]; then
        log_success "SSL 證書獲取成功"
        setup_nginx_certificates
    else
        log_error "SSL 證書獲取失敗"
        exit 1
    fi
}

# 更新 SSL 證書
renew_certificate() {
    log_info "更新 SSL 證書..."
    
    certbot renew --quiet
    
    if [ $? -eq 0 ]; then
        log_success "SSL 證書更新成功"
        setup_nginx_certificates
        reload_nginx
    else
        log_warning "SSL 證書更新失敗或無需更新"
    fi
}

# 設置 Nginx 證書軟鏈接
setup_nginx_certificates() {
    log_info "設置 Nginx 證書軟鏈接..."
    
    # 創建軟鏈接到 Nginx 證書目錄
    ln -sf "$CERT_DIR/fullchain.pem" "$NGINX_CERT_DIR/adhd-prod.crt"
    ln -sf "$CERT_DIR/privkey.pem" "$NGINX_CERT_DIR/adhd-prod.key"
    
    # 生成 DH 參數（如果不存在）
    if [ ! -f "$NGINX_CERT_DIR/dhparam.pem" ]; then
        log_info "生成 DH 參數..."
        openssl dhparam -out "$NGINX_CERT_DIR/dhparam.pem" 2048
    fi
    
    # 設置適當權限
    chown -R root:root "$NGINX_CERT_DIR"
    chmod 644 "$NGINX_CERT_DIR"/*.crt
    chmod 600 "$NGINX_CERT_DIR"/*.key
    chmod 644 "$NGINX_CERT_DIR"/dhparam.pem
    
    log_success "Nginx 證書設置完成"
}

# 重載 Nginx
reload_nginx() {
    log_info "重載 Nginx 配置..."
    
    if command -v nginx &> /dev/null; then
        nginx -t && nginx -s reload
        log_success "Nginx 重載完成"
    elif command -v docker &> /dev/null; then
        docker exec adhd-nginx nginx -t && docker exec adhd-nginx nginx -s reload
        log_success "Nginx 容器重載完成"
    else
        log_warning "無法重載 Nginx，請手動執行"
    fi
}

# 設置自動更新
setup_auto_renewal() {
    log_info "設置自動證書更新..."
    
    # 創建更新腳本
    cat > /usr/local/bin/adhd-cert-renewal.sh << 'EOF'
#!/bin/bash
# ADHD 生產力系統證書自動更新腳本

export PATH=/usr/local/sbin:/usr/local/bin:/sbin:/bin:/usr/sbin:/usr/bin

# 記錄日誌
exec > /var/log/letsencrypt/renewal.log 2>&1

echo "$(date): 開始證書更新檢查"

# 更新證書
certbot renew --quiet

# 檢查更新結果
if [ $? -eq 0 ]; then
    echo "$(date): 證書更新檢查完成"
    
    # 重載 Nginx（如果是 Docker 環境）
    if docker ps | grep -q adhd-nginx; then
        docker exec adhd-nginx nginx -s reload
        echo "$(date): Nginx 重載完成"
    fi
else
    echo "$(date): 證書更新失敗"
fi
EOF

    chmod +x /usr/local/bin/adhd-cert-renewal.sh
    
    # 設置 crontab
    (crontab -l 2>/dev/null; echo "0 3 * * * /usr/local/bin/adhd-cert-renewal.sh") | crontab -
    
    log_success "自動更新設置完成（每日 3:00 AM 檢查）"
}

# 檢查證書狀態
check_certificate_status() {
    log_info "檢查證書狀態..."
    
    if [ -f "$CERT_DIR/fullchain.pem" ]; then
        local expiry_date
        expiry_date=$(openssl x509 -enddate -noout -in "$CERT_DIR/fullchain.pem" | cut -d= -f2)
        local expiry_timestamp
        expiry_timestamp=$(date -d "$expiry_date" +%s)
        local current_timestamp
        current_timestamp=$(date +%s)
        local days_until_expiry
        days_until_expiry=$(( (expiry_timestamp - current_timestamp) / 86400 ))
        
        echo "  證書路徑: $CERT_DIR/fullchain.pem"
        echo "  過期時間: $expiry_date"
        echo "  剩餘天數: $days_until_expiry 天"
        
        if [ $days_until_expiry -lt 30 ]; then
            log_warning "證書將在 30 天內過期，建議更新"
        else
            log_success "證書狀態正常"
        fi
    else
        log_error "未找到 SSL 證書"
    fi
}

# 備份證書
backup_certificates() {
    local backup_dir="/backup/ssl-certs/$(date +%Y%m%d)"
    
    log_info "備份 SSL 證書到 $backup_dir..."
    
    mkdir -p "$backup_dir"
    
    if [ -d "$CERT_DIR" ]; then
        cp -r "$CERT_DIR" "$backup_dir/"
        tar -czf "$backup_dir/ssl-backup-$(date +%Y%m%d-%H%M%S).tar.gz" -C "/etc/letsencrypt" .
        
        log_success "證書備份完成"
    else
        log_warning "未找到證書目錄，跳過備份"
    fi
}

# 顯示幫助信息
show_help() {
    echo "ADHD 生產力系統 SSL 證書管理腳本"
    echo ""
    echo "使用方法:"
    echo "  $0 [選項]"
    echo ""
    echo "選項:"
    echo "  install     安裝 Certbot 並獲取證書"
    echo "  renew       更新現有證書"
    echo "  status      檢查證書狀態"
    echo "  backup      備份證書"
    echo "  help        顯示此幫助信息"
    echo ""
    echo "環境變數:"
    echo "  DOMAIN      要申請證書的域名"
    echo "  EMAIL       Let's Encrypt 帳戶郵箱"
    echo ""
    echo "範例:"
    echo "  export DOMAIN=adhd-productivity.com"
    echo "  export EMAIL=admin@adhd-productivity.com"
    echo "  $0 install"
}

# 主程序
main() {
    case "${1:-help}" in
        "install")
            check_environment
            install_certbot
            create_directories
            obtain_certificate
            setup_auto_renewal
            check_certificate_status
            ;;
        "renew")
            check_environment
            renew_certificate
            check_certificate_status
            ;;
        "status")
            check_certificate_status
            ;;
        "backup")
            backup_certificates
            ;;
        "help"|*)
            show_help
            ;;
    esac
}

# 檢查是否為 root 用戶
if [ "$EUID" -ne 0 ]; then
    log_error "請以 root 用戶執行此腳本"
    exit 1
fi

main "$@"