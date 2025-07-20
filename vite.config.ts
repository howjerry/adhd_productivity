import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'
import path from 'path'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      workbox: {
        clientsClaim: true,
        skipWaiting: true
      },
      manifest: {
        name: 'ADHD Productivity System',
        short_name: 'ADHD Focus',
        description: 'A productivity system designed specifically for ADHD minds',
        theme_color: '#6366F1',
        background_color: '#F8F9FA',
        display: 'standalone',
        icons: [
          {
            src: '/icon-192.png',
            sizes: '192x192',
            type: 'image/png'
          },
          {
            src: '/icon-512.png',
            sizes: '512x512',
            type: 'image/png'
          }
        ]
      }
    })
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@/components': path.resolve(__dirname, './src/components'),
      '@/stores': path.resolve(__dirname, './src/stores'),
      '@/hooks': path.resolve(__dirname, './src/hooks'),
      '@/utils': path.resolve(__dirname, './src/utils'),
      '@/types': path.resolve(__dirname, './src/types'),
      '@/services': path.resolve(__dirname, './src/services'),
      '@/styles': path.resolve(__dirname, './src/styles')
    }
  },
  server: {
    port: 3000,
    open: true
  },
  build: {
    target: 'esnext',
    outDir: 'dist',
    assetsDir: 'assets',
    sourcemap: process.env.NODE_ENV === 'development',
    minify: 'terser',
    terserOptions: {
      compress: {
        drop_console: process.env.NODE_ENV === 'production',
        drop_debugger: true,
      },
    },
    rollupOptions: {
      output: {
        manualChunks: (id) => {
          // React 核心
          if (id.includes('react') || id.includes('react-dom')) {
            return 'react-vendor';
          }
          
          // 路由相關
          if (id.includes('react-router')) {
            return 'router';
          }
          
          // UI 庫
          if (id.includes('framer-motion') || id.includes('lucide-react')) {
            return 'ui-vendor';
          }
          
          // 虛擬滾動
          if (id.includes('react-window')) {
            return 'virtualization';
          }
          
          // SignalR
          if (id.includes('@microsoft/signalr')) {
            return 'signalr';
          }
          
          // Zustand 狀態管理
          if (id.includes('zustand') || id.includes('immer')) {
            return 'state-management';
          }
          
          // 工具庫
          if (id.includes('clsx') || id.includes('date-fns')) {
            return 'utils';
          }
          
          // 第三方大型庫
          if (id.includes('node_modules')) {
            return 'vendor';
          }
        },
        chunkFileNames: (chunkInfo) => {
          const facadeModuleId = chunkInfo.facadeModuleId
            ? chunkInfo.facadeModuleId.split('/').pop().replace(/\.[^/.]+$/, "")
            : 'unknown';
          return `assets/js/[name]-[hash].js`;
        },
        assetFileNames: (assetInfo) => {
          const info = assetInfo.name.split('.');
          const extType = info[info.length - 1];
          if (/\.(png|jpe?g|svg|gif|tiff|bmp|ico)$/i.test(assetInfo.name)) {
            return `assets/images/[name]-[hash].${extType}`;
          }
          if (/\.(css)$/i.test(assetInfo.name)) {
            return `assets/css/[name]-[hash].${extType}`;
          }
          return `assets/[ext]/[name]-[hash].${extType}`;
        },
      },
      treeshake: {
        moduleSideEffects: false,
        propertyReadSideEffects: false,
        unknownGlobalSideEffects: false,
      },
      external: (id) => {
        // 排除不需要打包的外部依賴
        return false;
      },
    },
    // 資源最佳化
    assetsInlineLimit: 4096, // 小於 4KB 的資源內聯
    reportCompressedSize: false, // 減少構建時間
    chunkSizeWarningLimit: 1000, // 增加 chunk 大小警告閾值
  }
})