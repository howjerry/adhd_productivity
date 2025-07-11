# Claude Code OAuth Token 設定指南

## 🎯 重要發現

根據 [這個成功案例](https://github.com/susumutomita/zenn-article/issues/165)，可以使用 OAuth token 來整合 Claude Code Actions，這可能是 Max 訂閱戶的解決方案！

## 🔐 取得 OAuth Token

### 方法一：從 Claude Code CLI 取得（推薦）

1. **在本機安裝 Claude Code**
   ```bash
   npm install -g @anthropic-ai/claude-code
   ```

2. **登入您的 Claude 帳戶**
   ```bash
   claude code /login
   ```

3. **取得 OAuth token**
   ```bash
   claude code /auth status --show-token
   ```
   
   或查看設定檔：
   - Windows: `%APPDATA%\claude-code\config.json`
   - macOS/Linux: `~/.config/claude-code/config.json`

### 方法二：從瀏覽器開發者工具取得

1. 登入 [Claude.ai](https://claude.ai)
2. 開啟開發者工具 (F12)
3. 前往 Network 標籤
4. 重新整理頁面
5. 尋找包含 `Authorization` header 的請求
6. 複製 Bearer token

## 📝 設定步驟

### 1. 新增 GitHub Secret

1. 前往：https://github.com/howjerry/adhd_productivity/settings/secrets/actions
2. 點擊 **"New repository secret"**
3. 名稱：`CLAUDE_CODE_OAUTH_TOKEN`
4. 值：貼上您的 OAuth token
5. 點擊 **"Add secret"**

### 2. 使用 Workflow

已建立 `claude-oauth.yml`，支援：
- Issue 評論觸發：`@claude`
- Issue 標籤觸發：`claude`
- PR 審查觸發

## ⚠️ 注意事項

1. **Token 有效期**
   - OAuth token 可能會過期
   - 需要定期更新（約每 30-90 天）
   
2. **安全性**
   - 永遠不要將 token 提交到程式碼中
   - 只透過 GitHub Secrets 使用

3. **限制**
   - 這是非官方的解決方案
   - 可能隨時失效
   - 使用風險自負

## 🔄 Token 更新流程

當 workflow 失敗並顯示認證錯誤時：

1. 重新執行取得 token 的步驟
2. 更新 GitHub Secret
3. 重新執行 workflow

## 📚 參考資料

- [susumutomita 的實作](https://github.com/susumutomita/zenn-article/blob/main/.github/workflows/claude.yml)
- [官方 Claude Code Action](https://github.com/anthropics/claude-code-action)

---

**免責聲明**：這是社群發現的非官方方法，Anthropic 並未正式支援此認證方式。