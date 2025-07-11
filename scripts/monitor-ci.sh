#!/bin/bash

# CI/CD 監控腳本
echo "🔍 開始監控 CI/CD 執行狀態..."

# 取得最新的 workflow run ID
RUN_ID=$(gh run list --limit 1 --json databaseId -q '.[0].databaseId')

if [ -z "$RUN_ID" ]; then
    echo "❌ 無法找到執行中的 workflow"
    exit 1
fi

echo "📊 監控 Workflow Run ID: $RUN_ID"

# 持續監控直到完成
while true; do
    STATUS=$(gh run view $RUN_ID --json status,conclusion -q '.status')
    CONCLUSION=$(gh run view $RUN_ID --json status,conclusion -q '.conclusion')
    
    if [ "$STATUS" = "completed" ]; then
        echo ""
        if [ "$CONCLUSION" = "success" ]; then
            echo "✅ CI/CD 執行成功！"
            gh run view $RUN_ID
            exit 0
        else
            echo "❌ CI/CD 執行失敗 (結果: $CONCLUSION)"
            echo ""
            echo "🔍 失敗詳情："
            gh run view $RUN_ID --log-failed | head -100
            exit 1
        fi
    else
        echo -ne "\r⏳ 狀態: $STATUS... (按 Ctrl+C 停止監控)"
        sleep 5
    fi
done