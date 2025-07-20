#!/bin/bash

# ADHD 生產力系統 - 容器化部署腳本
# Agent 5 - Docker 配置優化完成

set -e

echo "🚀 啟動 ADHD 生產力系統容器化部署..."

# 檢查必要文件
echo "📋 檢查配置文件..."
if [ ! -f ".env" ]; then
    echo "❌ 錯誤：.env 文件不存在，請先創建環境變數配置"
    exit 1
fi

if [ ! -f "docker-compose.yml" ]; then
    echo "❌ 錯誤：docker-compose.yml 文件不存在"
    exit 1
fi

# 停止現有容器（如果有）
echo "🛑 停止現有容器..."
docker-compose down || true

# 清理孤立的網路和卷
echo "🧹 清理舊資源..."
docker network prune -f || true

# 構建並啟動所有服務
echo "🔨 構建並啟動容器..."
docker-compose up --build -d

# 等待服務啟動
echo "⏳ 等待服務啟動完成..."
sleep 30

# 檢查容器狀態
echo "📊 檢查容器狀態..."
docker-compose ps

# 健康檢查
echo "🔍 執行健康檢查..."
echo "前端檢查："
if curl -f -s http://localhost:8080 > /dev/null; then
    echo "✅ 前端服務正常 (http://localhost:8080)"
else
    echo "❌ 前端服務異常"
fi

echo "後端檢查："
if curl -f -s http://localhost:8080/api/health > /dev/null; then
    echo "✅ 後端 API 正常 (http://localhost:8080/api)"
else
    echo "❌ 後端 API 異常"
fi

echo "數據庫檢查："
if docker exec adhd-postgres pg_isready -U adhd_user -d adhd_productivity > /dev/null 2>&1; then
    echo "✅ PostgreSQL 資料庫正常"
else
    echo "❌ PostgreSQL 資料庫異常"
fi

echo "快取檢查："
if docker exec adhd-redis redis-cli ping > /dev/null 2>&1; then
    echo "✅ Redis 快取正常"
else
    echo "❌ Redis 快取異常"
fi

echo ""
echo "🎉 部署完成！"
echo "📱 前端應用: http://localhost:8080"
echo "🔧 後端 API: http://localhost:8080/api"
echo "💾 直接後端: http://localhost:5000"
echo "🐘 PostgreSQL: localhost:5432 (容器內部)"
echo "🔄 Redis: localhost:6379 (容器內部)"
echo ""
echo "📋 常用命令："
echo "  docker-compose logs -f [service]  # 查看日誌"
echo "  docker-compose stop              # 停止服務"
echo "  docker-compose down              # 停止並刪除容器"
echo "  docker-compose ps                # 查看狀態"