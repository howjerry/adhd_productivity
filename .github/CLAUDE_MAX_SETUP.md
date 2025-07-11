# Claude Max 訂閱戶 GitHub 整合設定指南

## ⚠️ 重要更新（2025年1月）

根據 [Anthropic 官方回應](https://github.com/anthropics/claude-code-action/issues/4)：
> "Currently we don't support Claude Max in the GitHub action. You'll need to create an API key via console.anthropic.com in order to use the action."

**經過實際測試確認：**
- Claude Code Action 不支援 Max/Pro 訂閱戶
- OAuth token (`sk-ant-oat-xxx`) 無法用於 API 調用
- 必須使用付費 API key (`sk-ant-api-xxx`)

## 🎯 替代方案

### 方案一：使用 Claude Helper Workflow（✅ 已實作）
我們已經為您建立了 `claude-helper.yml`，它會：
- 自動格式化 GitHub issue 內容
- 產生適合貼到 Claude.ai 的提示
- 提供清楚的使用指引

### 方案二：申請 API Key（需額外付費）
前往 [console.anthropic.com](https://console.anthropic.com) 申請 API key

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

### 2. 使用 Claude Helper Workflow

GitHub App 安裝後，Helper workflow 會自動運作：
- **無需設定 API keys 或 Secrets**
- **完全免費使用**
- **支援 Max/Pro 訂閱戶**

## 📋 使用說明

Helper workflow (`claude-helper.yml`) 提供以下功能：


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
- 在 PR 評論中提及 `@claude`

**自動執行：**
- 產生程式碼審查提示
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

您可以修改 `claude-helper.yml` 來：

1. **新增自訂標籤**
   ```yaml
   contains(github.event.issue.labels.*.name, 'your-custom-label')
   ```

2. **客製化回應訊息**
   ```javascript
   const customMessage = `您的自訂訊息`;
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

2. **Helper 訊息未出現**
   - 確認使用 `@claude` 觸發
   - 檢查 GitHub App 安裝狀態
   - 查看 Actions 日誌

3. **Claude.ai 整合**
   - 確保在 Claude.ai 中提供完整上下文
   - 參考專案的 CLAUDE.md 檔案
   - 使用明確的指令

## 📚 相關資源

- [GitHub Apps 文件](https://docs.github.com/en/apps)
- [GitHub Actions Secrets](https://docs.github.com/en/actions/security-guides/using-secrets-in-github-actions)
- [Claude.ai](https://claude.ai)
- [專案 CLAUDE.md](../CLAUDE.md)