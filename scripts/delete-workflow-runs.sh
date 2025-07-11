#!/bin/bash

# 刪除 GitHub Actions Workflow 執行歷程腳本
# 使用方法: ./delete-workflow-runs.sh [選項]

echo "🗑️ GitHub Actions Workflow 執行歷程刪除工具"
echo "=========================================="

# 顯示選項
echo "請選擇刪除方式："
echo "1. 刪除所有失敗的執行記錄"
echo "2. 刪除所有執行記錄"
echo "3. 刪除特定 workflow 的所有記錄"
echo "4. 刪除超過 N 天的記錄"
echo "5. 退出"

read -p "請輸入選項 (1-5): " choice

case $choice in
    1)
        echo "正在刪除所有失敗的執行記錄..."
        gh run list --status failure --limit 100 --json databaseId -q '.[].databaseId' | while read id; do
            echo "刪除執行 ID: $id"
            gh run delete $id
        done
        ;;
    2)
        read -p "⚠️ 確定要刪除所有執行記錄嗎？(y/N): " confirm
        if [[ $confirm == "y" || $confirm == "Y" ]]; then
            echo "正在刪除所有執行記錄..."
            gh run list --limit 100 --json databaseId -q '.[].databaseId' | while read id; do
                echo "刪除執行 ID: $id"
                gh run delete $id
            done
        fi
        ;;
    3)
        echo "可用的 workflows:"
        gh workflow list
        read -p "請輸入 workflow 名稱: " workflow_name
        echo "正在刪除 '$workflow_name' 的所有執行記錄..."
        gh run list --workflow "$workflow_name" --limit 100 --json databaseId -q '.[].databaseId' | while read id; do
            echo "刪除執行 ID: $id"
            gh run delete $id
        done
        ;;
    4)
        read -p "請輸入天數: " days
        echo "正在刪除超過 $days 天的執行記錄..."
        # 計算日期
        if [[ "$OSTYPE" == "darwin"* ]]; then
            # macOS
            cutoff_date=$(date -v-${days}d -u +"%Y-%m-%dT%H:%M:%SZ")
        else
            # Linux
            cutoff_date=$(date -d "$days days ago" -u +"%Y-%m-%dT%H:%M:%SZ")
        fi
        
        gh run list --limit 100 --json databaseId,createdAt | \
        python3 -c "
import sys, json
from datetime import datetime
data = json.load(sys.stdin)
cutoff = datetime.fromisoformat('$cutoff_date'.replace('Z', '+00:00'))
for run in data:
    created = datetime.fromisoformat(run['createdAt'].replace('Z', '+00:00'))
    if created < cutoff:
        print(run['databaseId'])
" | while read id; do
            echo "刪除執行 ID: $id"
            gh run delete $id
        done
        ;;
    5)
        echo "退出"
        exit 0
        ;;
    *)
        echo "無效的選項"
        exit 1
        ;;
esac

echo "✅ 完成！"