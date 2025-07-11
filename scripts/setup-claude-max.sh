#!/bin/bash

# Claude Max 訂閱戶 GitHub 整合設定腳本
# 使用方式: ./scripts/setup-claude-max.sh

set -e

echo "🤖 Claude Max GitHub 整合設定精靈"
echo "===================================="
echo ""
echo "📌 重要：此設定適用於 Claude Max/Pro 訂閱戶"
echo "   無需使用 API key！"
echo ""

# 檢查是否在 git 儲存庫中
if ! git rev-parse --git-dir > /dev/null 2>&1; then
    echo "❌ 錯誤：請在 git 儲存庫根目錄執行此腳本"
    exit 1
fi

# 取得儲存庫資訊
REPO_URL=$(git config --get remote.origin.url)
REPO_NAME=$(basename -s .git "$REPO_URL")
REPO_OWNER=$(dirname "$REPO_URL" | xargs basename)

echo "📁 儲存庫: $REPO_OWNER/$REPO_NAME"
echo ""

echo "步驟 1: 安裝 Claude GitHub App"
echo "==============================="
echo ""
echo "請選擇安裝方式："
echo "1) 使用 Claude Code CLI (推薦)"
echo "2) 手動安裝"
read -p "請輸入選項 (1-2): " INSTALL_CHOICE

case $INSTALL_CHOICE in
    1)
        echo ""
        echo "請在終端機執行："
        echo "claude code /install-github-app"
        echo ""
        read -p "完成後按 Enter 繼續..."
        ;;
    2)
        echo ""
        echo "請前往以下網址安裝："
        echo "https://github.com/apps/claude"
        echo ""
        echo "1. 點擊 'Install'"
        echo "2. 選擇 '$REPO_NAME' 儲存庫"
        echo "3. 完成授權"
        echo ""
        read -p "完成後按 Enter 繼續..."
        ;;
esac

echo ""
echo "步驟 2: 設定 GitHub App 認證"
echo "============================="
echo ""
echo "您需要："
echo "1. 前往 GitHub Settings → Developer settings → GitHub Apps"
echo "2. 找到您的 Claude App"
echo "3. 記下 App ID"
echo "4. 產生 Private Key (.pem 檔案)"
echo ""
echo "然後在以下頁面新增 Secrets："
echo "https://github.com/$REPO_OWNER/$REPO_NAME/settings/secrets/actions"
echo ""
echo "需要新增的 Secrets："
echo "- CLAUDE_APP_ID: 您的 App ID"
echo "- CLAUDE_APP_PRIVATE_KEY: Private Key 內容（包含 BEGIN/END）"
echo ""
read -p "完成後按 Enter 繼續..."

echo ""
echo "✅ 設定完成！"
echo ""
echo "📋 可用功能："
echo ""
echo "1️⃣ Issue → 分支/PR："
echo "   在 issue 加上 'claude' 標籤"
echo ""
echo "2️⃣ 程式碼審查："
echo "   在 PR 加上 'claude-review' 標籤"
echo ""
echo "3️⃣ 互動協助："
echo "   評論中提及 '@claude'"
echo ""
echo "📝 使用流程："
echo "1. 在 GitHub 觸發動作（標籤或評論）"
echo "2. 前往 Claude.ai 貼上相關內容"
echo "3. 取得 Claude 的建議或程式碼"
echo "4. 更新到對應的分支/PR"
echo ""
echo "詳細說明請參考 .github/CLAUDE_MAX_SETUP.md"
echo ""
echo "提交變更："
echo "git add .github/ scripts/"
echo "git commit -m '🤖 設定 Claude Max GitHub 整合'"
echo "git push"