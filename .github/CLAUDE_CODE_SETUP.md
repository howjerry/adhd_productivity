# Claude Code GitHub Actions 設定指南

⚠️ **注意：Claude Max/Pro 訂閱戶請參考 [CLAUDE_MAX_SETUP.md](./CLAUDE_MAX_SETUP.md)**

## 🚀 快速開始

### 1. 設定 API 金鑰

在您的 GitHub 儲存庫中設定以下 Secrets：

#### 選項 A：使用 Anthropic API（僅適用於 API 用戶）
1. 前往 [Anthropic Console](https://console.anthropic.com/)
2. 建立 API 金鑰
3. 在 GitHub 設定：
   - 進入 Settings → Secrets and variables → Actions
   - 新增 Secret：`ANTHROPIC_API_KEY`

#### 選項 B：使用 AWS Bedrock
1. 設定 AWS IAM 角色
2. 新增 Secret：`AWS_ROLE_TO_ASSUME`

#### 選項 C：使用 Google Vertex AI
1. 設定 GCP 服務帳戶
2. 新增 Secrets：
   - `GCP_WORKLOAD_IDENTITY_PROVIDER`
   - `GCP_SERVICE_ACCOUNT`

### 2. 安裝 Claude GitHub App（可選）

執行以下命令：
```bash
claude code /install-github-app
```

或手動安裝：
1. 前往 GitHub Apps 頁面
2. 安裝 Claude Code App
3. 授權存取您的儲存庫

## 📋 功能使用說明

### 1. 將 Issue 轉換為 PR

**方法一：使用標籤**
- 在 issue 加上 `claude-implement` 標籤
- Claude 會自動分析 issue 並建立 PR

**方法二：使用評論**
- 在 issue 中評論：`@claude 請實作這個功能`

### 2. 自動程式碼審查

**自動審查所有新 PR：**
- 預設會自動審查所有新的 PR
- 若要跳過審查，加上 `skip-claude-review` 標籤

**手動觸發審查：**
- 在 PR 加上 `claude-review` 標籤
- 或評論：`@claude 請審查這個 PR`

### 3. 快速修復錯誤

在 issue 或 PR 評論中：
```
@claude fix 這個錯誤
[貼上錯誤訊息或描述]
```

### 4. 實作協助

在任何地方評論：
```
@claude help 如何實作這個功能？
[描述您的需求]
```

## 🎯 使用範例

### 範例 1：從 Issue 建立功能
```markdown
Title: 新增使用者通知功能

@claude 請根據以下需求實作：
- 支援 Email 和推播通知
- 可以設定通知偏好
- 符合 ADHD 使用者需求（不要太頻繁）
```

### 範例 2：程式碼審查
```markdown
@claude 請特別注意：
- 效能影響
- 安全性問題
- ADHD 使用者體驗
```

### 範例 3：修復 Bug
```markdown
@claude fix
錯誤訊息：TypeError: Cannot read property 'id' of undefined
位置：src/components/TaskList.tsx:45
```

## ⚙️ 進階設定

### 自訂 Claude 行為

在專案根目錄的 `CLAUDE.md` 已包含專案特定指示。您可以加入更多指導原則。

### 設定不同的模型

在 workflow 中修改：
```yaml
model: claude-3-5-sonnet-20241022  # 最新版本
# 或
model: claude-3-opus-20240229      # 更強大但較慢
```

### 調整回應參數

```yaml
max-tokens: 4096      # 最大回應長度
temperature: 0.2      # 創意程度 (0-1)
```

## 🛡️ 安全性考量

1. **API 金鑰安全**
   - 永遠使用 GitHub Secrets
   - 定期輪換金鑰
   - 監控使用量

2. **程式碼審查**
   - Claude 的建議應該被審查
   - 不要自動合併 PR
   - 確保符合安全標準

3. **存取控制**
   - 限制誰可以觸發 Claude
   - 使用 CODEOWNERS 檔案
   - 設定分支保護規則

## 💰 成本控制

1. **監控使用量**
   - 在 Anthropic Console 查看使用情況
   - 設定使用量警報

2. **優化提示**
   - 保持提示簡潔明確
   - 使用 `CLAUDE.md` 避免重複說明

3. **限制觸發條件**
   - 只在需要時使用 @claude
   - 考慮使用特定標籤觸發

## 🐛 疑難排解

### 常見問題

1. **Claude 沒有回應**
   - 檢查 API 金鑰是否正確
   - 確認 workflow 語法正確
   - 查看 Actions 日誌

2. **回應不如預期**
   - 更新 `CLAUDE.md` 提供更多上下文
   - 在提示中更具體

3. **成本過高**
   - 減少自動觸發
   - 使用更小的模型
   - 優化提示長度

## 📚 相關資源

- [Claude Code 文件](https://docs.anthropic.com/en/docs/claude-code)
- [GitHub Actions 文件](https://docs.github.com/en/actions)
- [專案 CLAUDE.md](./CLAUDE.md)