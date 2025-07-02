#!/bin/bash

# ADHD 生產力系統一鍵安裝腳本
# 完全容器化部署，不依賴任何外部資源
# 
# 使用方法:
# curl -fsSL https://raw.githubusercontent.com/your-repo/ADHD/main/install.sh | bash
# 或者
# wget -qO- https://raw.githubusercontent.com/your-repo/ADHD/main/install.sh | bash

set -e

# 顏色定義
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 配置變數
PROJECT_NAME="ADHD"
REPO_URL="https://github.com/your-username/ADHD.git"  # 請修改為實際的 repo URL
INSTALL_DIR="$HOME/adhd-productivity-system"
COMPOSE_FILE="docker-compose.self-contained.yml"

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

# 檢查系統需求
check_requirements() {
    log_info "檢查系統需求..."
    
    # 檢查作業系統
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        OS="Linux"
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        OS="macOS"
    elif [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "cygwin" ]]; then
        OS="Windows"
    else
        log_error "不支援的作業系統: $OSTYPE"
        exit 1
    fi
    log_success "作業系統: $OS"
    
    # 檢查 Docker
    if ! command -v docker &> /dev/null; then
        log_error "Docker 未安裝，請先安裝 Docker"
        log_info "安裝指南: https://docs.docker.com/get-docker/"
        exit 1
    fi
    
    # 檢查 Docker 是否運行
    if ! docker info &> /dev/null; then
        log_error "Docker 未運行，請啟動 Docker"
        exit 1
    fi
    log_success "Docker 已安裝並運行"
    
    # 檢查 Docker Compose
    if ! command -v docker-compose &> /dev/null; then
        log_error "Docker Compose 未安裝，請先安裝 Docker Compose"
        log_info "安裝指南: https://docs.docker.com/compose/install/"
        exit 1
    fi
    log_success "Docker Compose 已安裝"
    
    # 檢查 Git
    if ! command -v git &> /dev/null; then
        log_error "Git 未安裝，請先安裝 Git"
        exit 1
    fi
    log_success "Git 已安裝"
}

# 清理現有安裝
cleanup_existing() {
    log_info "檢查並清理現有安裝..."
    
    if [ -d "$INSTALL_DIR" ]; then
        log_warning "發現現有安裝目錄: $INSTALL_DIR"
        read -p "是否要刪除現有安裝並重新安裝? (y/N): " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            log_info "停止現有容器..."
            cd "$INSTALL_DIR" && docker-compose -f "$COMPOSE_FILE" down --volumes --remove-orphans || true
            
            log_info "刪除現有目錄..."
            rm -rf "$INSTALL_DIR"
            log_success "現有安裝已清理"
        else
            log_info "保留現有安裝，退出安裝程序"
            exit 0
        fi
    fi
    
    # 清理可能存在的容器和網路
    log_info "清理殘留的 Docker 資源..."
    docker stop adhd-postgres adhd-redis adhd-backend adhd-frontend adhd-nginx adhd-pgadmin 2>/dev/null || true
    docker rm adhd-postgres adhd-redis adhd-backend adhd-frontend adhd-nginx adhd-pgadmin 2>/dev/null || true
    docker network rm adhd-productivity-network 2>/dev/null || true
    log_success "Docker 資源清理完成"
}

# 下載專案
download_project() {
    log_info "下載 ADHD 生產力系統..."
    
    # 建立安裝目錄
    mkdir -p "$(dirname "$INSTALL_DIR")"
    
    # Clone 專案
    git clone "$REPO_URL" "$INSTALL_DIR"
    cd "$INSTALL_DIR"
    
    log_success "專案下載完成"
}

# 檢查必要文件
check_files() {
    log_info "檢查必要文件..."
    
    required_files=(
        "$COMPOSE_FILE"
        "backend/Dockerfile"
        "frontend/Dockerfile"
        "nginx/nginx.conf"
        "database/init"
    )
    
    for file in "${required_files[@]}"; do
        if [ ! -e "$file" ]; then
            log_error "缺少必要文件: $file"
            exit 1
        fi
    done
    
    log_success "所有必要文件檢查完成"
}

# 建立環境配置
setup_environment() {
    log_info "設置環境配置..."
    
    # 建立 .env 文件（如果不存在）
    if [ ! -f ".env" ]; then
        cat > .env << EOF
# ADHD 生產力系統環境配置
POSTGRES_PASSWORD=adhd_secure_pass_2024
REDIS_PASSWORD=redis_secure_pass_2024
JWT_SECRET=ADHD_Production_JWT_Secret_Key_2024_Very_Secure_And_Long_String_For_Container_Deployment
PGADMIN_EMAIL=admin@adhd.local
PGADMIN_PASSWORD=admin_secure_pass_2024

# 資料庫配置
POSTGRES_DB=adhd_productivity
POSTGRES_USER=adhd_user

# 應用配置
ASPNETCORE_ENVIRONMENT=Production
NODE_ENV=production
EOF
        log_success "環境配置文件已建立"
    else
        log_info "使用現有環境配置文件"
    fi
}

# 建立必要目錄
create_directories() {
    log_info "建立必要目錄..."
    
    directories=(
        "database/init"
        "database/schemas"
        "nginx/conf.d"
        "logs"
    )
    
    for dir in "${directories[@]}"; do
        mkdir -p "$dir"
    done
    
    log_success "目錄結構建立完成"
}

# 建構和啟動服務
build_and_start() {
    log_info "建構 Docker 映像..."
    
    # 建構映像
    docker-compose -f "$COMPOSE_FILE" build --no-cache
    log_success "Docker 映像建構完成"
    
    log_info "啟動服務..."
    
    # 啟動服務
    docker-compose -f "$COMPOSE_FILE" up -d
    
    log_info "等待服務啟動..."
    sleep 30
    
    # 檢查服務狀態
    log_info "檢查服務狀態..."
    docker-compose -f "$COMPOSE_FILE" ps
}

# 等待服務就緒
wait_for_services() {
    log_info "等待服務完全啟動..."
    
    # 等待資料庫準備就緒
    log_info "等待資料庫服務..."
    timeout=120
    while [ $timeout -gt 0 ]; do
        if docker exec adhd-postgres pg_isready -U adhd_user -d adhd_productivity > /dev/null 2>&1; then
            log_success "資料庫服務已就緒"
            break
        fi
        sleep 2
        timeout=$((timeout-2))
    done
    
    if [ $timeout -le 0 ]; then
        log_error "資料庫服務啟動超時"
        exit 1
    fi
    
    # 等待 API 服務
    log_info "等待 API 服務..."
    timeout=120
    while [ $timeout -gt 0 ]; do
        if curl -f http://localhost:5000/health > /dev/null 2>&1; then
            log_success "API 服務已就緒"
            break
        fi
        sleep 2
        timeout=$((timeout-2))
    done
    
    if [ $timeout -le 0 ]; then
        log_warning "API 服務健康檢查超時，但可能仍在啟動中"
    fi
    
    # 等待前端服務
    log_info "等待前端服務..."
    timeout=60
    while [ $timeout -gt 0 ]; do
        if curl -f http://localhost:80 > /dev/null 2>&1; then
            log_success "前端服務已就緒"
            break
        fi
        sleep 2
        timeout=$((timeout-2))
    done
    
    if [ $timeout -le 0 ]; then
        log_warning "前端服務健康檢查超時，但可能仍在啟動中"
    fi
}

# 顯示安裝結果
show_result() {
    echo
    echo "========================================"
    log_success "ADHD 生產力系統安裝完成！"
    echo "========================================"
    echo
    echo "系統資訊："
    echo "• 安裝目錄: $INSTALL_DIR"
    echo "• 主要服務: http://localhost"
    echo "• API 服務: http://localhost:5000"
    echo "• 資料庫管理: http://localhost:5050 (需要啟動 admin profile)"
    echo
    echo "管理指令："
    echo "• 查看狀態: cd $INSTALL_DIR && docker-compose -f $COMPOSE_FILE ps"
    echo "• 停止服務: cd $INSTALL_DIR && docker-compose -f $COMPOSE_FILE down"
    echo "• 啟動服務: cd $INSTALL_DIR && docker-compose -f $COMPOSE_FILE up -d"
    echo "• 查看日誌: cd $INSTALL_DIR && docker-compose -f $COMPOSE_FILE logs -f"
    echo "• 啟動管理界面: cd $INSTALL_DIR && docker-compose -f $COMPOSE_FILE --profile admin up -d"
    echo
    echo "資料持久化："
    echo "• 資料庫資料將保存在 Docker volume 中"
    echo "• 即使重新啟動容器，資料也會保持"
    echo
    log_info "享受您的 ADHD 生產力系統！"
}

# 錯誤處理
handle_error() {
    log_error "安裝過程中發生錯誤"
    log_info "您可以查看錯誤日誌並嘗試以下操作："
    echo "1. 檢查 Docker 是否正常運行"
    echo "2. 確保端口 80, 5000, 5432, 6379 未被佔用"
    echo "3. 重新運行安裝腳本"
    echo "4. 查看詳細日誌: cd $INSTALL_DIR && docker-compose -f $COMPOSE_FILE logs"
    exit 1
}

# 主要安裝流程
main() {
    echo "========================================"
    echo "     ADHD 生產力系統一鍵安裝"
    echo "========================================"
    echo
    
    # 設置錯誤處理
    trap 'handle_error' ERR
    
    # 執行安裝步驟
    check_requirements
    cleanup_existing
    download_project
    check_files
    setup_environment
    create_directories
    build_and_start
    wait_for_services
    show_result
}

# 執行主程序
main "$@"