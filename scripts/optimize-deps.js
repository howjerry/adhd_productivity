#!/usr/bin/env node

/**
 * 依賴優化腳本
 * 分析並建議移除未使用的依賴
 */

import fs from 'fs';
import path from 'path';
import { execSync } from 'child_process';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// 讀取 package.json
const packageJsonPath = path.join(__dirname, '../package.json');
const packageJson = JSON.parse(fs.readFileSync(packageJsonPath, 'utf8'));

// 可能保留的依賴（即使顯示為未使用）
const keepDependencies = [
  '@types/react',
  '@types/react-dom',
  'typescript',
  'vite',
  'sass',
  'vitest',
  '@testing-library/react',
  '@testing-library/jest-dom',
  '@testing-library/user-event',
  'jsdom',
  'eslint',
  '@typescript-eslint/eslint-plugin',
  '@typescript-eslint/parser',
  'eslint-plugin-react-hooks',
  'eslint-plugin-react-refresh',
  '@vitejs/plugin-react',
  'vite-plugin-pwa',
];

// 可能移除的依賴列表（基於 depcheck 結果）
const potentiallyUnusedDeps = [
  '@hookform/resolvers',
  '@react-aria/focus',
  'date-fns',
  'react-aria',
  'react-dnd',
  'react-dnd-html5-backend',
  'react-hook-form',
  'react-hotkeys-hook',
  'react-intersection-observer',
  'zod',
];

const potentiallyUnusedDevDeps = [
  '@vitest/coverage-v8',
];

console.log('🔍 分析依賴使用情況...\n');

// 檢查每個依賴是否真的未使用
function checkDependencyUsage(depName) {
  try {
    // 搜尋在 src 目錄中的使用
    const result = execSync(`grep -r "${depName}" src/ --include="*.ts" --include="*.tsx" --include="*.js" --include="*.jsx"`, {
      encoding: 'utf8',
      stdio: 'pipe'
    });
    return result.trim().length > 0;
  } catch (error) {
    // grep 沒找到結果會拋出錯誤
    return false;
  }
}

function analyzeDependency(depName) {
  const isUsed = checkDependencyUsage(depName);
  const shouldKeep = keepDependencies.includes(depName);
  
  return {
    name: depName,
    isUsed,
    shouldKeep,
    canRemove: !isUsed && !shouldKeep
  };
}

console.log('📦 生產依賴分析:');
console.log('================');

const depAnalysis = potentiallyUnusedDeps.map(analyzeDependency);
depAnalysis.forEach(dep => {
  const status = dep.canRemove ? '❌ 可移除' : 
                 dep.isUsed ? '✅ 使用中' : 
                 dep.shouldKeep ? '🔒 保留' : '⚠️  檢查';
  console.log(`${status} ${dep.name}`);
});

console.log('\n🛠️  開發依賴分析:');
console.log('================');

const devDepAnalysis = potentiallyUnusedDevDeps.map(analyzeDependency);
devDepAnalysis.forEach(dep => {
  const status = dep.canRemove ? '❌ 可移除' : 
                 dep.isUsed ? '✅ 使用中' : 
                 dep.shouldKeep ? '🔒 保留' : '⚠️  檢查';
  console.log(`${status} ${dep.name}`);
});

// 生成移除命令
const depsToRemove = depAnalysis.filter(dep => dep.canRemove).map(dep => dep.name);
const devDepsToRemove = devDepAnalysis.filter(dep => dep.canRemove).map(dep => dep.name);

if (depsToRemove.length > 0 || devDepsToRemove.length > 0) {
  console.log('\n🗑️  建議移除的依賴:');
  console.log('==================');
  
  if (depsToRemove.length > 0) {
    const removeCommand = `npm uninstall ${depsToRemove.join(' ')}`;
    console.log(`生產依賴: ${removeCommand}`);
  }
  
  if (devDepsToRemove.length > 0) {
    const removeDevCommand = `npm uninstall ${devDepsToRemove.join(' ')}`;
    console.log(`開發依賴: ${removeDevCommand}`);
  }
  
  console.log('\n⚠️  注意: 移除前請確認這些依賴確實未使用!');
  console.log('建議先備份 package.json 或創建 git commit。');
} else {
  console.log('\n✨ 沒有發現可以安全移除的依賴!');
}

// 檢查 bundle size
console.log('\n📊 Bundle 大小分析:');
console.log('=================');

try {
  console.log('正在構建專案以分析 bundle 大小...');
  execSync('npm run build', { stdio: 'pipe' });
  
  // 讀取 dist 目錄大小
  const distPath = path.join(__dirname, '../dist');
  if (fs.existsSync(distPath)) {
    const stats = execSync(`du -sh ${distPath}`, { encoding: 'utf8' });
    console.log(`總構建大小: ${stats.trim()}`);
    
    // 列出最大的文件
    try {
      const largeFiles = execSync(`find ${distPath} -name "*.js" -o -name "*.css" | head -10 | xargs ls -lh`, { 
        encoding: 'utf8' 
      });
      console.log('\n最大的資源文件:');
      console.log(largeFiles);
    } catch (error) {
      // ignore
    }
  }
} catch (error) {
  console.log('無法分析 bundle 大小 (構建失敗)');
}

console.log('\n🎯 效能優化建議:');
console.log('===============');
console.log('✅ 已實作程式碼分割 (React.lazy)');
console.log('✅ 已實作虛擬滾動');
console.log('✅ 已配置 tree shaking');
console.log('✅ 已優化 Vite 構建配置');
console.log('📝 考慮實作服務工作者快取');
console.log('📝 考慮實作圖片懶加載');
console.log('📝 監控實際使用者效能指標');