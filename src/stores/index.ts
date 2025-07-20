// ADHD 生產力系統 - Store 統一匯出
// 統一的狀態管理系統，包含所有 store 和相關工具

// 導入 stores 以供內部使用
import { useAuthStore } from './useAuthStore';
import { useTaskStore } from './useTaskStore'; 
import { useTimerStore } from './useTimerStore';
import { useRealtimeSyncStore } from './RealtimeSync';
import { 
  useRootStore,
  useAppInitialized,
  useAppLoading,
  useGlobalError
} from './RootStore';
import { apiClient } from '@/services/ApiClient';
import { signalRService } from '@/services/signalRService';

// === 核心 Stores ===
export { useAuthStore } from './useAuthStore';
export { useTaskStore } from './useTaskStore'; 
export { useTimerStore } from './useTimerStore';
export { useRealtimeSyncStore } from './RealtimeSync';

// === 中央管理 Store ===
export { 
  useRootStore,
  useAppInitialized,
  useAppLoading,
  useGlobalError,
  useTheme,
  useNetworkStatus,
  useFeatureFlags
} from './RootStore';

// === 服務和工具 ===
export { apiClient } from '@/services/ApiClient';
export { signalRService } from '@/services/signalRService';

// === Store 類型定義 ===
export type { 
  SyncStatus,
  SyncEvent,
  ConflictResolution,
  SyncConfig 
} from './RealtimeSync';

// === 便利 Hooks ===

/**
 * 統一的狀態管理 Hook
 * 提供所有 store 的訪問入口
 */
export const useAppStores = () => {
  const rootStore = useRootStore();
  const authStore = useAuthStore();
  const taskStore = useTaskStore();
  const timerStore = useTimerStore();
  const syncStore = useRealtimeSyncStore();

  return {
    root: rootStore,
    auth: authStore,
    task: taskStore,
    timer: timerStore,
    sync: syncStore
  };
};

/**
 * 應用初始化狀態 Hook
 * 檢查應用是否已完全初始化
 */
export const useAppReady = () => {
  const isInitialized = useAppInitialized();
  const isLoading = useAppLoading();
  const globalError = useGlobalError();

  return {
    isReady: isInitialized && !isLoading,
    isLoading,
    hasError: !!globalError,
    error: globalError
  };
};

/**
 * 認證狀態 Hook
 * 提供簡化的認證狀態訪問
 */
export const useAuth = () => {
  const store = useAuthStore();

  return {
    user: store.user,
    isAuthenticated: store.isAuthenticated,
    isLoading: store.isLoading,
    error: store.error,
    login: store.login,
    register: store.register,
    logout: store.logout
  };
};

/**
 * 任務管理 Hook
 * 提供簡化的任務操作接口
 */
export const useTasks = () => {
  const store = useTaskStore();

  return {
    tasks: store.tasks,
    filteredTasks: store.filteredTasks,
    selectedTask: store.selectedTask,
    filters: store.filters,
    isLoading: store.isLoading,
    error: store.error,
    actions: {
      create: store.createTask,
      update: store.updateTask,
      delete: store.deleteTask,
      complete: store.completeTask,
      fetch: store.fetchTasks,
      setFilters: store.setFilters,
      select: store.selectTask
    }
  };
};

/**
 * 計時器 Hook
 * 提供簡化的計時器操作接口
 */
export const useTimer = () => {
  const store = useTimerStore();

  return {
    state: {
      isRunning: store.isRunning,
      isPaused: store.isPaused,
      mode: store.mode,
      timeRemaining: store.timeRemaining,
      totalDuration: store.totalDuration,
      sessionCount: store.sessionCount,
      currentTaskId: store.currentTaskId
    },
    settings: store.settings,
    statistics: store.statistics,
    actions: {
      start: store.start,
      pause: store.pause,
      resume: store.resume,
      stop: store.stop,
      reset: store.reset,
      skip: store.skip,
      switchToWork: store.switchToWork,
      switchToShortBreak: store.switchToShortBreak,
      switchToLongBreak: store.switchToLongBreak,
      updateSettings: store.updateSettings
    }
  };
};

/**
 * 實時同步 Hook
 * 提供簡化的同步狀態訪問
 */
export const useSync = () => {
  const store = useRealtimeSyncStore();

  return {
    status: store.status,
    isSyncing: store.isSyncing,
    lastSyncAt: store.lastSyncAt,
    pendingEventsCount: store.pendingEvents.length,
    conflictsCount: store.conflictingEvents.length,
    stats: store.stats,
    actions: {
      connect: store.connect,
      disconnect: store.disconnect,
      sendTimerSync: store.sendTimerSync,
      sendTaskUpdate: store.sendTaskUpdate,
      sendProgressUpdate: store.sendProgressUpdate
    }
  };
};

/**
 * 全域載入狀態 Hook
 * 統一處理所有 store 的載入狀態
 */
export const useGlobalLoading = () => {
  const appLoading = useAppLoading();
  const authLoading = useAuthStore((state) => state.isLoading);
  const taskLoading = useTaskStore((state) => state.isLoading);
  const syncLoading = useRealtimeSyncStore((state) => state.isSyncing);

  return {
    isLoading: appLoading || authLoading || taskLoading || syncLoading,
    breakdown: {
      app: appLoading,
      auth: authLoading,
      tasks: taskLoading,
      sync: syncLoading
    }
  };
};

/**
 * 全域錯誤狀態 Hook
 * 統一處理所有 store 的錯誤狀態
 */
export const useGlobalErrors = () => {
  const globalError = useGlobalError();
  const authError = useAuthStore((state) => state.error);
  const taskError = useTaskStore((state) => state.error);
  const syncErrors = useRealtimeSyncStore((state) => state.syncErrors);

  const errors = [
    globalError && { source: 'global', message: globalError },
    authError && { source: 'auth', message: authError },
    taskError && { source: 'tasks', message: taskError },
    ...syncErrors.map((error) => ({ source: 'sync', message: error }))
  ].filter(Boolean);

  return {
    hasErrors: errors.length > 0,
    errors,
    firstError: errors[0] || null
  };
};

/**
 * 開發工具 Hook
 * 提供開發和調試相關的工具
 */
export const useDevTools = () => {
  const rootStore = useRootStore();
  const syncStore = useRealtimeSyncStore();

  return {
    // 獲取所有 store 狀態
    getAllStates: () => rootStore.getStoreStates(),
    
    // 獲取調試資訊
    getDebugInfo: () => ({
      root: {
        initialized: rootStore.isInitialized,
        theme: rootStore.theme,
        online: rootStore.isOnline,
        features: rootStore.featureFlags
      },
      sync: syncStore.getDebugInfo(),
      performance: rootStore.performanceMetrics
    }),
    
    // 匯出同步日誌
    exportSyncLog: () => syncStore.exportSyncLog(),
    
    // 重置所有狀態
    resetAll: () => rootStore.reset(),
    
    // 清除所有資料
    clearAllData: () => rootStore.clearAllData(),
    
    // 匯出/匯入資料
    exportData: () => rootStore.exportAllData(),
    importData: (data: object) => rootStore.importAllData(data)
  };
};

// === 常數定義 ===

/**
 * Store 事件類型
 */
export const STORE_EVENTS = {
  AUTH_LOGIN: 'auth:login',
  AUTH_LOGOUT: 'auth:logout',
  TASK_CREATED: 'task:created',
  TASK_UPDATED: 'task:updated',
  TASK_DELETED: 'task:deleted',
  TIMER_STARTED: 'timer:started',
  TIMER_STOPPED: 'timer:stopped',
  SYNC_CONNECTED: 'sync:connected',
  SYNC_DISCONNECTED: 'sync:disconnected'
} as const;

/**
 * Store 狀態
 */
export const STORE_STATUS = {
  IDLE: 'idle',
  LOADING: 'loading',
  SUCCESS: 'success',
  ERROR: 'error'
} as const;

// === 工具函數 ===

/**
 * 檢查所有 store 是否已準備就緒
 */
export const areStoresReady = (): boolean => {
  const rootState = useRootStore.getState();
  return rootState.isInitialized && !rootState.isLoading;
};

/**
 * 獲取當前所有錯誤
 */
export const getAllErrors = (): string[] => {
  const rootState = useRootStore.getState();
  const authState = useAuthStore.getState();
  const taskState = useTaskStore.getState();
  const syncState = useRealtimeSyncStore.getState();

  return [
    rootState.globalError,
    authState.error,
    taskState.error,
    ...syncState.syncErrors
  ].filter(Boolean) as string[];
};

/**
 * 清除所有錯誤
 */
export const clearAllErrors = (): void => {
  useRootStore.getState().clearGlobalError();
  useAuthStore.getState().clearError();
  useTaskStore.getState().clearError();
  useRealtimeSyncStore.getState().clearSyncErrors();
};

export default {
  // Stores
  useRootStore,
  useAuthStore,
  useTaskStore,
  useTimerStore,
  useRealtimeSyncStore,
  
  // Hooks
  useAppStores,
  useAppReady,
  useAuth,
  useTasks,
  useTimer,
  useSync,
  useGlobalLoading,
  useGlobalErrors,
  useDevTools,
  
  // Services
  apiClient,
  signalRService,
  
  // Utils
  areStoresReady,
  getAllErrors,
  clearAllErrors
};