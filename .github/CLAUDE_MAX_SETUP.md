# Claude Max 訂閱戶 GitHub 整合設定指南

## 🎯 概述

作為 Claude Max 訂閱戶，您可以透過 GitHub App 整合來連接 Claude 與您的 GitHub 儲存庫，無需使用 API key。

## 🔧 設定步驟

### 1. 安裝 Claude GitHub App

#### 選項 A：使用 Claude Code CLI（推薦）
```bash
claude code /install-github-app
```

#### 選項 B：手動安裝
1. 前往 [GitHub Apps - Claude](https://github.com/apps/claude)
2. 點擊 "Install" 
3. 選擇您要授權的儲存庫
4. 完成安裝流程

### 2. 設定 GitHub App 認證

安裝完成後，您需要在儲存庫中設定以下 Secrets：

1. **取得 App 資訊**
   - 在 GitHub Settings → Developer settings → GitHub Apps 找到您的 Claude App
   - 記下 App ID
   - 產生並下載 Private Key (.pem 檔案)

2. **新增 GitHub Secrets**
   
   前往：`https://github.com/[您的帳號]/adhd_productivity/settings/secrets/actions`
   
   新增以下 Secrets：
   - `CLAUDE_APP_ID`: 您的 GitHub App ID
   - `CLAUDE_APP_PRIVATE_KEY`: Private Key 的完整內容（包含 BEGIN/END 行）

### 3. 使用 Workflow

已建立的 workflow (`claude-github-app.yml`) 提供以下功能：

## 📋 功能使用說明

### 1. Issue 自動建立分支

**觸發方式：**
- 在 issue 加上 `claude` 標籤

**自動執行：**
- 建立新分支 `claude/issue-[編號]`
- 建立 TODO 檔案
- 開啟 PR 草稿
- 提供 Claude.ai 使用指引

### 2. PR 程式碼審查

**觸發方式：**
- 在 PR 加上 `claude-review` 標籤

**自動執行：**
- 產生審查檢查清單
- 提供 Claude.ai 審查步驟
- 建立審查重點指引

### 3. 互動式協助

**觸發方式：**
- 在 issue 或 PR 評論中提及 `@claude`

**自動執行：**
- 確認收到請求
- 提供 Claude.ai 使用建議

## 🌟 最佳實踐

### 工作流程範例

1. **建立新功能**
   ```markdown
   # Issue 標題：新增任務提醒功能
   
   標籤：enhancement, claude
   
   內容：
   請實作一個任務提醒功能，需要：
   - 支援多種提醒時間
   - 考慮 ADHD 使用者需求
   - 整合現有通知系統
   ```

2. **在 Claude.ai 中使用**
   ```
   我有一個 GitHub issue #123 需要實作：
   [貼上 issue 內容]
   
   請幫我產生完整的實作程式碼，包含：
   - 前端 React 元件
   - 後端 API 端點
   - 資料庫 schema
   ```

3. **更新 PR**
   - 將 Claude 產生的程式碼複製到本地
   - 提交到對應的分支
   - 推送更新

### 安全性考量

1. **Secrets 保護**
   - Private Key 必須保密
   - 定期輪換 App credentials
   - 使用 GitHub 的 secret scanning

2. **權限控制**
   - 限制 App 權限到必要的儲存庫
   - 使用 branch protection rules
   - 審查所有 Claude 建議的變更

## 🚀 進階使用

### 自訂 Workflow

您可以修改 `claude-github-app.yml` 來：

1. **新增自訂標籤**
   ```yaml
   contains(github.event.issue.labels.*.name, 'your-custom-label')
   ```

2. **客製化回應訊息**
   ```bash
   gh issue comment $ISSUE_NUMBER --body "您的自訂訊息"
   ```

3. **整合其他工具**
   - 結合 CI/CD pipeline
   - 觸發其他 GitHub Actions
   - 整合專案管理工具

## 🐛 疑難排解

### 常見問題

1. **Workflow 沒有觸發**
   - 確認標籤名稱正確
   - 檢查 workflow 權限設定
   - 查看 Actions 日誌

2. **認證失敗**
   - 確認 Secrets 設定正確
   - 檢查 App 安裝狀態
   - 確認 Private Key 格式

3. **Claude.ai 整合**
   - 確保在 Claude.ai 中提供完整上下文
   - 參考專案的 CLAUDE.md 檔案
   - 使用明確的指令

## 📚 相關資源

- [GitHub Apps 文件](https://docs.github.com/en/apps)
- [GitHub Actions Secrets](https://docs.github.com/en/actions/security-guides/using-secrets-in-github-actions)
- [Claude.ai](https://claude.ai)
- [專案 CLAUDE.md](../CLAUDE.md)