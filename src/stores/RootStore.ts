import { create } from 'zustand';
import { immer } from 'zustand/middleware/immer';
import { persist } from 'zustand/middleware';
import { apiClient } from '@/services/ApiClient';
import { useAuthStore } from './useAuthStore';
import { useTaskStore } from './useTaskStore';
import { useTimerStore } from './useTimerStore';
import { useRealtimeSyncStore } from './RealtimeSync';
import { AppState } from '@/types';

// 全域應用狀態定義
interface GlobalState {
  // 應用核心狀態
  isInitialized: boolean;
  isLoading: boolean;
  globalError: string | null;
  theme: 'light' | 'dark' | 'auto';
  
  // UI 狀態
  sidebarOpen: boolean;
  currentView: AppState['currentView'];
  modalStack: string[];
  
  // 網路狀態
  isOnline: boolean;
  lastOnlineAt: string | null;
  
  // 性能監控
  performanceMetrics: {
    initialLoadTime: number;
    storeInitTime: number;
    apiResponseTimes: Record<string, number>;
  };
  
  // 功能開關
  featureFlags: {
    enableRealtimeSync: boolean;
    enableOfflineMode: boolean;
    enableDebugMode: boolean;
    enableAnalytics: boolean;
  };
}

// Store 協調動作
interface RootStoreActions {
  // 初始化
  initialize: () => Promise<void>;
  reset: () => void;
  
  // 主題管理
  setTheme: (theme: GlobalState['theme']) => void;
  toggleTheme: () => void;
  
  // UI 狀態管理
  setSidebarOpen: (open: boolean) => void;
  toggleSidebar: () => void;
  setCurrentView: (view: AppState['currentView']) => void;
  pushModal: (modalId: string) => void;
  popModal: () => string | null;
  clearModalStack: () => void;
  
  // 網路狀態管理
  setOnlineStatus: (online: boolean) => void;
  
  // 錯誤處理
  setGlobalError: (error: string | null) => void;
  clearGlobalError: () => void;
  
  // 性能監控
  recordApiResponseTime: (endpoint: string, time: number) => void;
  recordStoreInitTime: (time: number) => void;
  
  // 功能開關
  toggleFeatureFlag: (flag: keyof GlobalState['featureFlags']) => void;
  setFeatureFlag: (flag: keyof GlobalState['featureFlags'], value: boolean) => void;
  
  // Store 協調
  syncStores: () => Promise<void>;
  getStoreStates: () => StoreStates;
  subscribeToStoreChanges: () => void;
  
  // 資料管理
  clearAllData: () => Promise<void>;
  exportAllData: () => Promise<object>;
  importAllData: (data: object) => Promise<void>;
}

// 組合的 Store 狀態
interface StoreStates {
  auth: ReturnType<typeof useAuthStore.getState>;
  task: ReturnType<typeof useTaskStore.getState>;
  timer: ReturnType<typeof useTimerStore.getState>;
  realtimeSync: ReturnType<typeof useRealtimeSyncStore.getState>;
}

type RootStore = GlobalState & RootStoreActions;

// 預設狀態
const initialState: GlobalState = {
  isInitialized: false,
  isLoading: false,
  globalError: null,
  theme: 'auto',
  
  sidebarOpen: true,
  currentView: 'dashboard',
  modalStack: [],
  
  isOnline: navigator.onLine,
  lastOnlineAt: navigator.onLine ? new Date().toISOString() : null,
  
  performanceMetrics: {
    initialLoadTime: 0,
    storeInitTime: 0,
    apiResponseTimes: {}
  },
  
  featureFlags: {
    enableRealtimeSync: true,
    enableOfflineMode: true,
    enableDebugMode: process.env.NODE_ENV === 'development',
    enableAnalytics: process.env.NODE_ENV === 'production'
  }
};

// 輔助函數
const logDebug = (message: string, data?: unknown) => {
  if (process.env.NODE_ENV === 'development') {
    console.log(`[RootStore] ${message}`, data);
  }
};

const measureTime = <T>(fn: () => T): [T, number] => {
  const start = performance.now();
  const result = fn();
  const end = performance.now();
  return [result, end - start];
};

// Store 實作
export const useRootStore = create<RootStore>()(
  persist(
    immer((set, get) => ({
      ...initialState,

      // === 初始化 ===
      initialize: async () => {
        const startTime = performance.now();
        
        set((draft) => {
          draft.isLoading = true;
          draft.globalError = null;
        });

        try {
          logDebug('開始初始化 RootStore');

          // 設置 API 客戶端認證提供者
          apiClient.setAuthTokenProvider(() => {
            return useAuthStore.getState().token;
          });

          // 記錄各個 store 的初始化時間
          const [, authInitTime] = measureTime(() => {
            // Auth store 已經通過 persist 中間件自動初始化
          });

          const [, timerInitTime] = measureTime(() => {
            useTimerStore.getState().initialize();
          });

          const [, syncInitTime] = measureTime(() => {
            if (get().featureFlags.enableRealtimeSync) {
              useRealtimeSyncStore.getState().connect();
            }
          });

          // 設置 store 間的協調機制
          get().subscribeToStoreChanges();

          // 檢查使用者登入狀態
          const authState = useAuthStore.getState();
          if (authState.isAuthenticated && authState.token) {
            try {
              // 驗證 token 有效性並獲取使用者資料
              const userResponse = await apiClient.getCurrentUser();
              if (userResponse.data) {
                useAuthStore.getState().updateUser(userResponse.data);
                logDebug('使用者驗證成功');
              }
            } catch (error) {
              logDebug('Token 驗證失敗，登出使用者', error);
              useAuthStore.getState().logout();
            }
          }

          const totalInitTime = performance.now() - startTime;

          set((draft) => {
            draft.isInitialized = true;
            draft.isLoading = false;
            draft.performanceMetrics.initialLoadTime = totalInitTime;
            draft.performanceMetrics.storeInitTime = authInitTime + timerInitTime + syncInitTime;
          });

          logDebug(`RootStore 初始化完成，耗時 ${totalInitTime.toFixed(2)}ms`);

        } catch (error) {
          const errorMessage = error instanceof Error ? error.message : '初始化失敗';
          
          set((draft) => {
            draft.isLoading = false;
            draft.globalError = errorMessage;
          });

          logDebug('RootStore 初始化失敗', error);
        }
      },

      reset: () => {
        // 重置所有 store
        useAuthStore.getState().logout();
        useTaskStore.getState().clearError();
        useTimerStore.getState().reset();
        useRealtimeSyncStore.getState().disconnect();

        // 清除 API 快取
        apiClient.clearCache();

        set(() => ({
          ...initialState,
          isOnline: navigator.onLine,
          lastOnlineAt: navigator.onLine ? new Date().toISOString() : null
        }));

        logDebug('RootStore 已重置');
      },

      // === 主題管理 ===
      setTheme: (theme: GlobalState['theme']) => {
        set((draft) => {
          draft.theme = theme;
        });

        // 應用主題到 document
        const root = document.documentElement;
        if (theme === 'auto') {
          const isDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
          root.setAttribute('data-theme', isDark ? 'dark' : 'light');
        } else {
          root.setAttribute('data-theme', theme);
        }

        logDebug(`主題已切換到: ${theme}`);
      },

      toggleTheme: () => {
        const currentTheme = get().theme;
        const newTheme = currentTheme === 'light' ? 'dark' : 'light';
        get().setTheme(newTheme);
      },

      // === UI 狀態管理 ===
      setSidebarOpen: (open: boolean) => {
        set((draft) => {
          draft.sidebarOpen = open;
        });
      },

      toggleSidebar: () => {
        set((draft) => {
          draft.sidebarOpen = !draft.sidebarOpen;
        });
      },

      setCurrentView: (view: AppState['currentView']) => {
        set((draft) => {
          draft.currentView = view;
        });
      },

      pushModal: (modalId: string) => {
        set((draft) => {
          draft.modalStack.push(modalId);
        });
      },

      popModal: () => {
        const state = get();
        if (state.modalStack.length === 0) return null;

        const modalId = state.modalStack[state.modalStack.length - 1];
        
        set((draft) => {
          draft.modalStack.pop();
        });

        return modalId;
      },

      clearModalStack: () => {
        set((draft) => {
          draft.modalStack = [];
        });
      },

      // === 網路狀態管理 ===
      setOnlineStatus: (online: boolean) => {
        const wasOnline = get().isOnline;
        
        set((draft) => {
          draft.isOnline = online;
          if (online) {
            draft.lastOnlineAt = new Date().toISOString();
          }
        });

        // 處理網路狀態變化
        if (!wasOnline && online) {
          logDebug('網路已恢復連線');
          // 重新連線實時同步
          if (get().featureFlags.enableRealtimeSync) {
            useRealtimeSyncStore.getState().connect();
          }
          // 同步待處理的變更
          get().syncStores();
        } else if (wasOnline && !online) {
          logDebug('網路已斷線');
          useRealtimeSyncStore.getState().disconnect();
        }
      },

      // === 錯誤處理 ===
      setGlobalError: (error: string | null) => {
        set((draft) => {
          draft.globalError = error;
        });

        if (error) {
          logDebug('設置全域錯誤', error);
        }
      },

      clearGlobalError: () => {
        set((draft) => {
          draft.globalError = null;
        });
      },

      // === 性能監控 ===
      recordApiResponseTime: (endpoint: string, time: number) => {
        set((draft) => {
          draft.performanceMetrics.apiResponseTimes[endpoint] = time;
        });
      },

      recordStoreInitTime: (time: number) => {
        set((draft) => {
          draft.performanceMetrics.storeInitTime = time;
        });
      },

      // === 功能開關 ===
      toggleFeatureFlag: (flag: keyof GlobalState['featureFlags']) => {
        set((draft) => {
          draft.featureFlags[flag] = !draft.featureFlags[flag];
        });

        logDebug(`功能開關 ${flag} 已切換到: ${get().featureFlags[flag]}`);

        // 處理特定功能開關的副作用
        if (flag === 'enableRealtimeSync') {
          const syncStore = useRealtimeSyncStore.getState();
          if (get().featureFlags.enableRealtimeSync) {
            syncStore.connect();
          } else {
            syncStore.disconnect();
          }
        }
      },

      setFeatureFlag: (flag: keyof GlobalState['featureFlags'], value: boolean) => {
        set((draft) => {
          draft.featureFlags[flag] = value;
        });
      },

      // === Store 協調 ===
      syncStores: async () => {
        logDebug('開始同步 stores');

        try {
          // 如果使用者已登入且有網路連線，同步資料
          const authState = useAuthStore.getState();
          if (authState.isAuthenticated && get().isOnline) {
            // 重新獲取任務資料
            await useTaskStore.getState().fetchTasks();
            
            // 處理實時同步的待處理事件
            const syncState = useRealtimeSyncStore.getState();
            if (syncState.status === 'connected') {
              await syncState.processPendingEvents();
            }
          }

          logDebug('Store 同步完成');
        } catch (error) {
          logDebug('Store 同步失敗', error);
          get().setGlobalError('資料同步失敗');
        }
      },

      getStoreStates: (): StoreStates => {
        return {
          auth: useAuthStore.getState(),
          task: useTaskStore.getState(),
          timer: useTimerStore.getState(),
          realtimeSync: useRealtimeSyncStore.getState()
        };
      },

      subscribeToStoreChanges: () => {
        // 訂閱認證狀態變化
        useAuthStore.subscribe((state, prevState) => {
          if (state.isAuthenticated !== prevState.isAuthenticated) {
            if (state.isAuthenticated) {
              logDebug('使用者已登入，開始同步資料');
              get().syncStores();
              
              // 連線實時同步
              if (get().featureFlags.enableRealtimeSync) {
                useRealtimeSyncStore.getState().connect();
              }
            } else {
              logDebug('使用者已登出，清理資料');
              // 清除敏感資料
              useTaskStore.getState().clearError();
              useRealtimeSyncStore.getState().disconnect();
              apiClient.clearCache();
            }
          }
        });

        // 訂閱任務 store 錯誤
        useTaskStore.subscribe((state) => {
          if (state.error && !get().globalError) {
            get().setGlobalError(`任務操作失敗: ${state.error}`);
          }
        });

        // 訂閱實時同步錯誤
        useRealtimeSyncStore.subscribe((state) => {
          if (state.syncErrors.length > 0 && !get().globalError) {
            const latestError = state.syncErrors[state.syncErrors.length - 1];
            get().setGlobalError(`同步失敗: ${latestError}`);
          }
        });

        logDebug('Store 變化訂閱已設置');
      },

      // === 資料管理 ===
      clearAllData: async () => {
        logDebug('清除所有資料');

        try {
          // 如果使用者已登入，嘗試在伺服器端清除資料
          const authState = useAuthStore.getState();
          if (authState.isAuthenticated) {
            // 這裡可以呼叫 API 來清除伺服器端資料
            // await apiClient.clearUserData();
          }

          // 重置所有 store
          get().reset();

          // 清除 localStorage
          localStorage.clear();

          logDebug('所有資料已清除');
        } catch (error) {
          logDebug('清除資料失敗', error);
          throw error;
        }
      },

      exportAllData: async () => {
        logDebug('匯出所有資料');

        const stores = get().getStoreStates();
        const exportData = {
          timestamp: new Date().toISOString(),
          version: '1.0.0',
          stores: {
            auth: {
              user: stores.auth.user,
              preferences: stores.auth.user?.preferences
            },
            timer: {
              settings: stores.timer.settings,
              statistics: stores.timer.statistics
            },
            // 注意：不匯出敏感資料如 token
          },
          metadata: {
            exportedAt: new Date().toISOString(),
            platform: navigator.platform,
            userAgent: navigator.userAgent
          }
        };

        return exportData;
      },

      importAllData: async (data: object) => {
        logDebug('匯入資料', data);

        try {
          // 驗證資料格式
          // 這裡應該加入適當的資料驗證邏輯

          // 匯入各 store 的資料
          // 注意：需要小心處理資料的完整性和安全性

          logDebug('資料匯入完成');
        } catch (error) {
          logDebug('資料匯入失敗', error);
          throw error;
        }
      }
    })),
    {
      name: 'root-store',
      partialize: (state) => ({
        theme: state.theme,
        sidebarOpen: state.sidebarOpen,
        featureFlags: state.featureFlags
      })
    }
  )
);

// 自動初始化
if (typeof window !== 'undefined') {
  // 監聽網路狀態變化
  window.addEventListener('online', () => {
    useRootStore.getState().setOnlineStatus(true);
  });

  window.addEventListener('offline', () => {
    useRootStore.getState().setOnlineStatus(false);
  });

  // 監聽主題變化
  const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
  mediaQuery.addEventListener('change', () => {
    const rootStore = useRootStore.getState();
    if (rootStore.theme === 'auto') {
      rootStore.setTheme('auto'); // 重新應用主題
    }
  });

  // 頁面載入完成後初始化
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
      useRootStore.getState().initialize();
    });
  } else {
    // 文檔已經載入完成
    setTimeout(() => {
      useRootStore.getState().initialize();
    }, 0);
  }
}

// 匯出便利 hook
export const useAppInitialized = () => {
  return useRootStore((state) => state.isInitialized);
};

export const useAppLoading = () => {
  return useRootStore((state) => state.isLoading);
};

export const useGlobalError = () => {
  return useRootStore((state) => state.globalError);
};

export const useTheme = () => {
  return useRootStore((state) => ({
    theme: state.theme,
    setTheme: state.setTheme,
    toggleTheme: state.toggleTheme
  }));
};

export const useNetworkStatus = () => {
  return useRootStore((state) => ({
    isOnline: state.isOnline,
    lastOnlineAt: state.lastOnlineAt
  }));
};

export const useFeatureFlags = () => {
  return useRootStore((state) => ({
    flags: state.featureFlags,
    toggleFlag: state.toggleFeatureFlag,
    setFlag: state.setFeatureFlag
  }));
};

export default useRootStore;