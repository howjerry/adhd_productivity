# ADHD 生產力系統 - 開發環境設定指南

## 📋 目錄
1. [概述](#概述)
2. [開發環境需求](#開發環境需求)
3. [快速開始](#快速開始)
4. [詳細設定步驟](#詳細設定步驟)
5. [IDE 配置](#ide-配置)
6. [除錯和測試](#除錯和測試)
7. [程式碼品質工具](#程式碼品質工具)
8. [常見問題解決](#常見問題解決)
9. [貢獻指南](#貢獻指南)

## 🎯 概述

本指南將幫助開發者設定 ADHD 生產力系統的完整開發環境，包括前端 React 應用程式、ASP.NET Core API 後端、資料庫和相關開發工具。

### 開發架構

```
開發環境架構:
┌─────────────────────────────────────────────────┐
│                開發者工作站                        │
├─────────────────┬───────────────────────────────┤
│  前端開發伺服器   │  後端開發伺服器                  │
│  React + Vite   │  ASP.NET Core                │
│  Port: 5173     │  Port: 5000                  │
├─────────────────┼───────────────────────────────┤
│           Docker 開發服務群組                    │
│  ┌──────────────┬──────────────┬──────────────┐  │
│  │ PostgreSQL   │ Redis        │ pgAdmin      │  │
│  │ Port: 5432   │ Port: 6379   │ Port: 5050   │  │
│  └──────────────┴──────────────┴──────────────┘  │
└─────────────────────────────────────────────────┘
```

## 💻 開發環境需求

### 基本開發工具

| 工具 | 版本 | 用途 | 安裝連結 |
|------|------|------|----------|
| **Git** | 2.30+ | 版本控制 | [下載](https://git-scm.com/) |
| **Docker Desktop** | 4.0+ | 容器化開發 | [下載](https://www.docker.com/products/docker-desktop) |
| **Node.js** | 18.0+ | 前端開發 | [下載](https://nodejs.org/) |
| **.NET SDK** | 8.0+ | 後端開發 | [下載](https://dotnet.microsoft.com/download) |
| **Visual Studio Code** | 最新版 | 主要編輯器 | [下載](https://code.visualstudio.com/) |

### 可選開發工具

| 工具 | 用途 | 平台 |
|------|------|------|
| **Visual Studio 2022** | 完整 .NET IDE | Windows/Mac |
| **JetBrains Rider** | 跨平台 .NET IDE | All |
| **DataGrip** | 資料庫管理 | All |
| **Postman** | API 測試 | All |

### 瀏覽器擴充套件

- **React Developer Tools** - React 除錯
- **Redux DevTools** - 狀態管理除錯  
- **Apollo Client Devtools** - GraphQL 除錯（未來使用）

## 🚀 快速開始

### 一鍵開發環境設定

```bash
# 1. 克隆專案
git clone https://github.com/your-org/adhd-productivity.git
cd adhd-productivity

# 2. 執行設定腳本
./scripts/setup-dev.sh

# 3. 啟動開發服務
npm run dev
```

### 手動設定 (詳細步驟)

如果自動設定失敗，請按照以下詳細步驟進行：

## 📝 詳細設定步驟

### 步驟 1: 系統檢查

```bash
# 檢查必要工具
git --version          # 應該 >= 2.30
node --version         # 應該 >= 18.0
npm --version          # 應該 >= 8.0
dotnet --version       # 應該 >= 8.0
docker --version       # 應該 >= 20.10
docker-compose --version # 應該 >= 2.0
```

### 步驟 2: 專案克隆和初始化

```bash
# 克隆專案
git clone https://github.com/your-org/adhd-productivity.git
cd adhd-productivity

# 檢查分支
git branch -a
git checkout main

# 檢查專案結構
ls -la
```

### 步驟 3: 開發環境配置

```bash
# 複製開發環境配置
cp .env.example .env.development

# 編輯開發配置
nano .env.development
```

**開發環境配置 (.env.development):**

```bash
# 開發環境配置
NODE_ENV=development
ASPNETCORE_ENVIRONMENT=Development

# 資料庫配置 (Docker 服務)
POSTGRES_DB=adhd_productivity_dev
POSTGRES_USER=dev_user
POSTGRES_PASSWORD=dev_password_123
POSTGRES_HOST=localhost
POSTGRES_PORT=5432

# Redis 配置
REDIS_HOST=localhost
REDIS_PORT=6379
REDIS_PASSWORD=

# JWT 配置 (開發用)
JWT_SECRET_KEY=development_jwt_secret_key_for_local_dev_only
JWT_ISSUER=ADHDProductivitySystem
JWT_AUDIENCE=ADHDUsers
JWT_EXPIRY_MINUTES=1440

# 開發伺服器 URL
VITE_API_BASE_URL=http://localhost:5000/api
VITE_SIGNALR_HUB_URL=http://localhost:5000/hubs

# CORS 設定 (開發寬鬆設定)
ALLOWED_ORIGINS=http://localhost:3000,http://localhost:5173,http://localhost:8080

# 日誌設定
LOG_LEVEL=Debug
LOG_PATH=./logs/

# 開發功能
ENABLE_SWAGGER=true
ENABLE_DETAILED_ERRORS=true
ENABLE_PERFORMANCE_LOGGING=true
```

### 步驟 4: 啟動基礎服務

```bash
# 啟動 Docker 服務 (資料庫、Redis、pgAdmin)
docker-compose -f docker-compose.dev.yml up -d

# 檢查服務狀態
docker-compose -f docker-compose.dev.yml ps

# 查看服務日誌
docker-compose -f docker-compose.dev.yml logs
```

### 步驟 5: 後端開發環境設定

```bash
# 進入後端目錄
cd backend

# 還原 NuGet 套件
dotnet restore

# 建置專案
dotnet build

# 執行資料庫遷移
dotnet ef database update --project src/AdhdProductivitySystem.Infrastructure --startup-project src/AdhdProductivitySystem.Api

# 種子資料 (可選)
dotnet run --project src/AdhdProductivitySystem.Api -- --seed-data

# 啟動開發伺服器
dotnet watch run --project src/AdhdProductivitySystem.Api
```

### 步驟 6: 前端開發環境設定

```bash
# 開啟新終端，進入前端目錄
cd frontend  # 或專案根目錄 (如果前端在根目錄)

# 安裝 npm 套件
npm install

# 啟動開發伺服器
npm run dev
```

### 步驟 7: 驗證開發環境

開啟瀏覽器訪問以下 URL：

- **前端應用**: http://localhost:5173
- **後端 API**: http://localhost:5000
- **Swagger 文檔**: http://localhost:5000/swagger
- **pgAdmin**: http://localhost:5050

## 🛠️ IDE 配置

### Visual Studio Code

#### 必要擴充套件

```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "ms-dotnettools.vscode-dotnet-runtime",
    "bradlc.vscode-tailwindcss",
    "esbenp.prettier-vscode",
    "ms-vscode.vscode-typescript-next",
    "ms-vscode.vscode-json",
    "formulahendry.auto-rename-tag",
    "christian-kohler.path-intellisense",
    "ms-vscode.vscode-eslint",
    "dbaeumer.vscode-eslint"
  ]
}
```

#### 工作區設定

```json
{
  "editor.defaultFormatter": "esbenp.prettier-vscode",
  "editor.formatOnSave": true,
  "editor.codeActionsOnSave": {
    "source.fixAll.eslint": true
  },
  "typescript.preferences.importModuleSpecifier": "relative",
  "dotnet.completion.showCompletionItemsFromUnimportedNamespaces": true,
  "omnisharp.enableEditorConfigSupport": true,
  "files.exclude": {
    "**/node_modules": true,
    "**/bin": true,
    "**/obj": true
  }
}
```

#### 除錯配置 (.vscode/launch.json)

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (API)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/backend/src/AdhdProductivitySystem.Api/bin/Debug/net8.0/AdhdProductivitySystem.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/backend/src/AdhdProductivitySystem.Api",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "sourceFileMap": {
        "/Views": "${workspaceFolder}/Views"
      }
    },
    {
      "name": "Chrome Debug",
      "type": "chrome",
      "request": "launch",
      "url": "http://localhost:5173",
      "webRoot": "${workspaceFolder}/src"
    }
  ]
}
```

#### 任務配置 (.vscode/tasks.json)

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": [
        "build",
        "${workspaceFolder}/backend/AdhdProductivitySystem.sln",
        "/property:GenerateFullPaths=true",
        "/consoleloggerparameters:NoSummary"
      ],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "watch",
      "command": "dotnet",
      "type": "process",
      "args": [
        "watch",
        "run",
        "--project",
        "${workspaceFolder}/backend/src/AdhdProductivitySystem.Api"
      ],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "frontend-dev",
      "type": "shell",
      "command": "npm run dev",
      "group": "build",
      "presentation": {
        "echo": true,
        "reveal": "always",
        "focus": false,
        "panel": "new"
      }
    }
  ]
}
```

## 🧪 除錯和測試

### 後端測試

```bash
# 執行所有測試
dotnet test

# 執行特定測試專案
dotnet test backend/tests/AdhdProductivitySystem.Tests

# 執行測試並生成覆蓋率報告
dotnet test --collect:"XPlat Code Coverage"

# 執行整合測試
dotnet test backend/tests/Application.IntegrationTests
```

### 前端測試

```bash
# 執行單元測試
npm test

# 執行測試並監控變更
npm run test:watch

# 執行測試覆蓋率
npm run test:coverage

# 執行 E2E 測試
npm run test:e2e
```

### 除錯技巧

#### 後端除錯

```csharp
// 在控制器中設定斷點
[HttpGet]
public async Task<ActionResult<List<TaskDto>>> GetTasks()
{
    // 設定斷點在這裡
    var tasks = await _mediator.Send(new GetTasksQuery());
    return Ok(tasks);
}
```

#### 前端除錯

```typescript
// 在 React 組件中使用 debugger
const TaskList: React.FC = () => {
  const [tasks, setTasks] = useState<Task[]>([]);
  
  useEffect(() => {
    debugger; // 設定斷點
    fetchTasks();
  }, []);
  
  return <div>{/* 組件內容 */}</div>;
};
```

## 📊 程式碼品質工具

### ESLint 配置 (.eslintrc.cjs)

```javascript
module.exports = {
  root: true,
  env: { browser: true, es2020: true },
  extends: [
    'eslint:recommended',
    '@typescript-eslint/recommended',
    'plugin:react-hooks/recommended',
  ],
  ignorePatterns: ['dist', '.eslintrc.cjs'],
  parser: '@typescript-eslint/parser',
  plugins: ['react-refresh'],
  rules: {
    'react-refresh/only-export-components': [
      'warn',
      { allowConstantExport: true },
    ],
    '@typescript-eslint/no-unused-vars': 'error',
    '@typescript-eslint/explicit-function-return-type': 'warn',
  },
}
```

### Prettier 配置 (.prettierrc)

```json
{
  "semi": true,
  "trailingComma": "es5",
  "singleQuote": true,
  "printWidth": 80,
  "tabWidth": 2,
  "useTabs": false
}
```

### EditorConfig (.editorconfig)

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true

[*.{js,ts,tsx,jsx}]
indent_style = space
indent_size = 2

[*.{cs,csproj,sln}]
indent_style = space
indent_size = 4

[*.md]
trim_trailing_whitespace = false
```

### 程式碼格式化腳本

```bash
#!/bin/bash
# format-code.sh

echo "格式化前端程式碼..."
npm run lint:fix
npm run format

echo "格式化後端程式碼..."
dotnet format backend/AdhdProductivitySystem.sln

echo "程式碼格式化完成"
```

## 🔧 常見問題解決

### 1. Docker 服務啟動失敗

```bash
# 檢查 Docker 狀態
docker info

# 清理 Docker 資源
docker system prune -f

# 重新建構並啟動服務
docker-compose -f docker-compose.dev.yml down
docker-compose -f docker-compose.dev.yml up -d --build
```

### 2. 資料庫遷移問題

```bash
# 重置資料庫
dotnet ef database drop --project src/AdhdProductivitySystem.Infrastructure --startup-project src/AdhdProductivitySystem.Api --force

# 重新建立遷移
dotnet ef migrations add InitialCreate --project src/AdhdProductivitySystem.Infrastructure --startup-project src/AdhdProductivitySystem.Api

# 應用遷移
dotnet ef database update --project src/AdhdProductivitySystem.Infrastructure --startup-project src/AdhdProductivitySystem.Api
```

### 3. 前端套件衝突

```bash
# 清理 node_modules
rm -rf node_modules package-lock.json

# 重新安裝
npm install

# 或使用 npm ci 進行乾淨安裝
npm ci
```

### 4. 端口衝突

```bash
# 查看端口使用情況 (macOS/Linux)
lsof -i :5000
lsof -i :5173

# 查看端口使用情況 (Windows)
netstat -ano | findstr :5000

# 終止佔用端口的程序
kill -9 <PID>
```

### 5. CORS 錯誤

檢查 `.env.development` 中的 CORS 設定：

```bash
ALLOWED_ORIGINS=http://localhost:3000,http://localhost:5173,http://localhost:8080
```

## 🤝 貢獻指南

### Git 工作流程

```bash
# 1. 建立功能分支
git checkout -b feature/new-feature

# 2. 進行開發
# ... 編寫程式碼 ...

# 3. 提交變更
git add .
git commit -m "feat: 新增任務拖放功能"

# 4. 推送分支
git push origin feature/new-feature

# 5. 建立 Pull Request
```

### 提交訊息規範

使用 [Conventional Commits](https://www.conventionalcommits.org/) 格式：

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

範例：
```
feat(tasks): 新增任務拖放排序功能

實作了任務列表的拖放功能，使用者可以透過拖拉來重新排序任務。

Closes #123
```

### 程式碼審查清單

- [ ] 程式碼符合專案風格指南
- [ ] 包含適當的測試
- [ ] 文檔已更新
- [ ] 沒有引入安全漏洞
- [ ] 效能考量已評估
- [ ] 可訪問性要求已滿足

## 📚 開發工作流程範例

### 典型開發日工作流程

```bash
# 1. 開始新的一天
git pull origin main

# 2. 啟動開發環境
docker-compose -f docker-compose.dev.yml up -d

# 3. 啟動後端 (終端 1)
cd backend
dotnet watch run --project src/AdhdProductivitySystem.Api

# 4. 啟動前端 (終端 2)
npm run dev

# 5. 開始開發
code .

# 6. 結束開發時
docker-compose -f docker-compose.dev.yml down
```

### 功能開發工作流程

```bash
# 1. 建立功能分支
git checkout -b feature/task-priority-matrix

# 2. 後端開發
cd backend
# 建立實體、服務、控制器等

# 3. 執行後端測試
dotnet test

# 4. 前端開發
cd ../
# 建立組件、頁面、狀態管理等

# 5. 執行前端測試
npm test

# 6. 整合測試
# 測試前後端整合

# 7. 提交程式碼
git add .
git commit -m "feat: 實作任務優先順序矩陣"
git push origin feature/task-priority-matrix
```

## 🚀 生產力提升工具

### 自動化腳本

建立 `scripts/` 目錄並添加常用腳本：

```bash
# scripts/start-dev.sh
#!/bin/bash
echo "啟動 ADHD 生產力系統開發環境..."

# 啟動 Docker 服務
docker-compose -f docker-compose.dev.yml up -d

# 等待服務啟動
sleep 10

# 在新終端視窗啟動後端
osascript -e 'tell app "Terminal" to do script "cd '$(pwd)'/backend && dotnet watch run --project src/AdhdProductivitySystem.Api"'

# 在新終端視窗啟動前端
osascript -e 'tell app "Terminal" to do script "cd '$(pwd)' && npm run dev"'

echo "開發環境啟動完成！"
echo "前端: http://localhost:5173"
echo "後端: http://localhost:5000"
echo "Swagger: http://localhost:5000/swagger"
```

### VS Code 程式碼片段

建立 `.vscode/snippets.json`：

```json
{
  "React Function Component": {
    "prefix": "rfc",
    "body": [
      "import React from 'react';",
      "",
      "interface ${1:ComponentName}Props {",
      "  ${2:// props}",
      "}",
      "",
      "const ${1:ComponentName}: React.FC<${1:ComponentName}Props> = ({${3:props}}) => {",
      "  return (",
      "    <div>",
      "      ${4:// content}",
      "    </div>",
      "  );",
      "};",
      "",
      "export default ${1:ComponentName};"
    ],
    "description": "建立 React 功能組件"
  },
  "ASP.NET Controller": {
    "prefix": "controller",
    "body": [
      "[ApiController]",
      "[Route(\"api/[controller]\")]",
      "public class ${1:Name}Controller : ControllerBase",
      "{",
      "    private readonly ILogger<${1:Name}Controller> _logger;",
      "",
      "    public ${1:Name}Controller(ILogger<${1:Name}Controller> logger)",
      "    {",
      "        _logger = logger;",
      "    }",
      "",
      "    [HttpGet]",
      "    public async Task<ActionResult> Get()",
      "    {",
      "        ${2:// implementation}",
      "    }",
      "}"
    ],
    "description": "建立 ASP.NET Core 控制器"
  }
}
```

---

**版本**: 1.0.0  
**最後更新**: 2024年12月22日  
**維護者**: ADHD 生產力系統開發團隊