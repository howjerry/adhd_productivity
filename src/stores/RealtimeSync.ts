import { create } from 'zustand';
import { immer } from 'zustand/middleware/immer';
import { signalRService } from '@/services/signalRService';
import { TimerSyncEvent, TaskUpdateEvent, ProgressUpdateEvent, UUID } from '@/types';

// 同步狀態定義
export enum SyncStatus {
  DISCONNECTED = 'disconnected',
  CONNECTING = 'connecting',
  CONNECTED = 'connected',
  RECONNECTING = 'reconnecting',
  ERROR = 'error'
}

// 同步事件類型
export interface SyncEvent {
  id: string;
  type: 'timer' | 'task' | 'progress' | 'system';
  timestamp: string;
  data: unknown;
  source: 'local' | 'remote';
}

// 衝突解決策略
export enum ConflictResolution {
  LOCAL_WINS = 'local_wins',
  REMOTE_WINS = 'remote_wins',
  LAST_UPDATED_WINS = 'last_updated_wins',
  MANUAL_MERGE = 'manual_merge'
}

// 同步配置
export interface SyncConfig {
  autoReconnect: boolean;
  maxReconnectAttempts: number;
  conflictResolution: ConflictResolution;
  batchSyncInterval: number; // ms
  enableDebugLogging: boolean;
}

// Store 狀態定義
interface RealtimeSyncState {
  // 連線狀態
  status: SyncStatus;
  lastConnectedAt: string | null;
  lastDisconnectedAt: string | null;
  reconnectAttempts: number;
  
  // 同步狀態
  isSyncing: boolean;
  lastSyncAt: string | null;
  syncErrors: string[];
  
  // 事件佇列
  pendingEvents: SyncEvent[];
  recentEvents: SyncEvent[];
  conflictingEvents: SyncEvent[];
  
  // 配置
  config: SyncConfig;
  
  // 訂閱管理
  subscriptions: Map<string, () => void>;
  
  // 性能統計
  stats: {
    totalEventsReceived: number;
    totalEventsSent: number;
    totalConflicts: number;
    averageLatency: number;
    syncSuccessRate: number;
  };
}

// Store 動作定義
interface RealtimeSyncActions {
  // 連線管理
  connect: () => Promise<void>;
  disconnect: () => Promise<void>;
  reconnect: () => Promise<void>;
  
  // 事件處理
  handleTimerSync: (event: TimerSyncEvent) => void;
  handleTaskUpdate: (event: TaskUpdateEvent) => void;
  handleProgressUpdate: (event: ProgressUpdateEvent) => void;
  
  // 事件發送
  sendTimerSync: (timerState: Omit<TimerSyncEvent, 'timestamp'>) => Promise<void>;
  sendTaskUpdate: (taskId: UUID, changes: Record<string, unknown>) => Promise<void>;
  sendProgressUpdate: (progressData: Omit<ProgressUpdateEvent, 'timestamp'>) => Promise<void>;
  
  // 衝突解決
  resolveConflict: (eventId: string, resolution: ConflictResolution) => void;
  addConflictingEvent: (event: SyncEvent) => void;
  
  // 佇列管理
  addPendingEvent: (event: SyncEvent) => void;
  processPendingEvents: () => Promise<void>;
  clearPendingEvents: () => void;
  
  // 配置管理
  updateConfig: (config: Partial<SyncConfig>) => void;
  resetConfig: () => void;
  
  // 統計管理
  updateStats: (updates: Partial<RealtimeSyncState['stats']>) => void;
  resetStats: () => void;
  
  // 訂閱管理
  subscribe: (storeId: string, callback: () => void) => () => void;
  unsubscribe: (storeId: string) => void;
  
  // 錯誤處理
  addSyncError: (error: string) => void;
  clearSyncErrors: () => void;
  
  // 調試和監控
  getDebugInfo: () => object;
  exportSyncLog: () => SyncEvent[];
}

type RealtimeSyncStore = RealtimeSyncState & RealtimeSyncActions;

// 預設配置
const DEFAULT_CONFIG: SyncConfig = {
  autoReconnect: true,
  maxReconnectAttempts: 5,
  conflictResolution: ConflictResolution.LAST_UPDATED_WINS,
  batchSyncInterval: 1000,
  enableDebugLogging: process.env.NODE_ENV === 'development'
};

// 初始狀態
const initialState: RealtimeSyncState = {
  status: SyncStatus.DISCONNECTED,
  lastConnectedAt: null,
  lastDisconnectedAt: null,
  reconnectAttempts: 0,
  
  isSyncing: false,
  lastSyncAt: null,
  syncErrors: [],
  
  pendingEvents: [],
  recentEvents: [],
  conflictingEvents: [],
  
  config: DEFAULT_CONFIG,
  
  subscriptions: new Map(),
  
  stats: {
    totalEventsReceived: 0,
    totalEventsSent: 0,
    totalConflicts: 0,
    averageLatency: 0,
    syncSuccessRate: 0
  }
};

// 輔助函數
const createEvent = (
  type: SyncEvent['type'],
  data: unknown,
  source: SyncEvent['source'] = 'local'
): SyncEvent => ({
  id: `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
  type,
  timestamp: new Date().toISOString(),
  data,
  source
});

const logDebug = (message: string, data?: unknown) => {
  if (process.env.NODE_ENV === 'development') {
    console.log(`[RealtimeSync] ${message}`, data);
  }
};

// Store 實作
export const useRealtimeSyncStore = create<RealtimeSyncStore>()(
  immer((set, get) => ({
    ...initialState,

    // === 連線管理 ===
    connect: async () => {
      const state = get();
      
      if (state.status === SyncStatus.CONNECTED || state.status === SyncStatus.CONNECTING) {
        logDebug('已經連線或正在連線中');
        return;
      }

      set((draft) => {
        draft.status = SyncStatus.CONNECTING;
        draft.reconnectAttempts = 0;
      });

      try {
        // 設置 SignalR 事件監聽器
        const timerUnsubscribe = signalRService.onTimerSync((event) => {
          get().handleTimerSync(event);
        });

        const taskUnsubscribe = signalRService.onTaskUpdate((event) => {
          get().handleTaskUpdate(event);
        });

        const progressUnsubscribe = signalRService.onProgressUpdate((event) => {
          get().handleProgressUpdate(event);
        });

        // 檢查 SignalR 連線狀態
        if (signalRService.isConnected()) {
          set((draft) => {
            draft.status = SyncStatus.CONNECTED;
            draft.lastConnectedAt = new Date().toISOString();
            draft.reconnectAttempts = 0;
          });

          // 處理待處理的事件
          await get().processPendingEvents();

          logDebug('實時同步連線成功');
        } else {
          throw new Error('SignalR 未連線');
        }

        // 儲存取消訂閱函數
        set((draft) => {
          draft.subscriptions.set('timer', timerUnsubscribe);
          draft.subscriptions.set('task', taskUnsubscribe);
          draft.subscriptions.set('progress', progressUnsubscribe);
        });

      } catch (error) {
        set((draft) => {
          draft.status = SyncStatus.ERROR;
          draft.syncErrors.push(`連線失敗: ${error instanceof Error ? error.message : '未知錯誤'}`);
        });

        logDebug('實時同步連線失敗', error);

        // 自動重連
        if (state.config.autoReconnect && state.reconnectAttempts < state.config.maxReconnectAttempts) {
          setTimeout(() => {
            set((draft) => {
              draft.reconnectAttempts += 1;
            });
            get().reconnect();
          }, Math.min(1000 * Math.pow(2, state.reconnectAttempts), 30000));
        }
      }
    },

    disconnect: async () => {
      const state = get();
      
      // 取消所有訂閱
      state.subscriptions.forEach((unsubscribe) => {
        unsubscribe();
      });

      set((draft) => {
        draft.status = SyncStatus.DISCONNECTED;
        draft.lastDisconnectedAt = new Date().toISOString();
        draft.subscriptions.clear();
      });

      logDebug('實時同步已斷線');
    },

    reconnect: async () => {
      await get().disconnect();
      
      set((draft) => {
        draft.status = SyncStatus.RECONNECTING;
      });

      await get().connect();
    },

    // === 事件處理 ===
    handleTimerSync: (event: TimerSyncEvent) => {
      const syncEvent = createEvent('timer', event, 'remote');
      
      set((draft) => {
        draft.recentEvents.unshift(syncEvent);
        draft.recentEvents = draft.recentEvents.slice(0, 100); // 保留最近 100 個事件
        draft.stats.totalEventsReceived += 1;
        draft.lastSyncAt = new Date().toISOString();
      });

      // 通知訂閱的 store
      const state = get();
      const callback = state.subscriptions.get('timer');
      if (callback) {
        try {
          callback();
        } catch (error) {
          logDebug('訂閱回調執行失敗: timer', error);
        }
      }
      
      logDebug('收到計時器同步事件', event);
    },

    handleTaskUpdate: (event: TaskUpdateEvent) => {
      const syncEvent = createEvent('task', event, 'remote');
      
      set((draft) => {
        draft.recentEvents.unshift(syncEvent);
        draft.recentEvents = draft.recentEvents.slice(0, 100);
        draft.stats.totalEventsReceived += 1;
        draft.lastSyncAt = new Date().toISOString();
      });

      // 檢查衝突
      const state = get();
      const hasConflict = checkForConflicts(syncEvent, state);
      if (hasConflict) {
        get().addConflictingEvent(syncEvent);
        return;
      }

      // 通知訂閱的 store
      const callback = state.subscriptions.get('task');
      if (callback) {
        try {
          callback();
        } catch (error) {
          logDebug('訂閱回調執行失敗: task', error);
        }
      }
      
      logDebug('收到任務更新事件', event);
    },

    handleProgressUpdate: (event: ProgressUpdateEvent) => {
      const syncEvent = createEvent('progress', event, 'remote');
      
      set((draft) => {
        draft.recentEvents.unshift(syncEvent);
        draft.recentEvents = draft.recentEvents.slice(0, 100);
        draft.stats.totalEventsReceived += 1;
        draft.lastSyncAt = new Date().toISOString();
      });

      // 通知訂閱的 store
      const state = get();
      const callback = state.subscriptions.get('progress');
      if (callback) {
        try {
          callback();
        } catch (error) {
          logDebug('訂閱回調執行失敗: progress', error);
        }
      }
      
      logDebug('收到進度更新事件', event);
    },

    // === 事件發送 ===
    sendTimerSync: async (timerState: Omit<TimerSyncEvent, 'timestamp'>) => {
      const state = get();
      
      if (state.status !== SyncStatus.CONNECTED) {
        // 如果未連線，加入待處理佇列
        const event = createEvent('timer', timerState);
        get().addPendingEvent(event);
        return;
      }

      try {
        await signalRService.syncTimer(timerState);
        
        set((draft) => {
          draft.stats.totalEventsSent += 1;
        });

        logDebug('計時器同步事件已發送', timerState);
      } catch (error) {
        get().addSyncError(`發送計時器同步失敗: ${error instanceof Error ? error.message : '未知錯誤'}`);
      }
    },

    sendTaskUpdate: async (taskId: UUID, changes: Record<string, unknown>) => {
      const state = get();
      
      if (state.status !== SyncStatus.CONNECTED) {
        const event = createEvent('task', { taskId, changes });
        get().addPendingEvent(event);
        return;
      }

      try {
        await signalRService.notifyTaskUpdate(taskId, changes);
        
        set((draft) => {
          draft.stats.totalEventsSent += 1;
        });

        logDebug('任務更新事件已發送', { taskId, changes });
      } catch (error) {
        get().addSyncError(`發送任務更新失敗: ${error instanceof Error ? error.message : '未知錯誤'}`);
      }
    },

    sendProgressUpdate: async (progressData: Omit<ProgressUpdateEvent, 'timestamp'>) => {
      const state = get();
      
      if (state.status !== SyncStatus.CONNECTED) {
        const event = createEvent('progress', progressData);
        get().addPendingEvent(event);
        return;
      }

      try {
        await signalRService.notifyProgressUpdate(progressData);
        
        set((draft) => {
          draft.stats.totalEventsSent += 1;
        });

        logDebug('進度更新事件已發送', progressData);
      } catch (error) {
        get().addSyncError(`發送進度更新失敗: ${error instanceof Error ? error.message : '未知錯誤'}`);
      }
    },

    // === 衝突解決 ===
    resolveConflict: (eventId: string, resolution: ConflictResolution) => {
      set((draft) => {
        const eventIndex = draft.conflictingEvents.findIndex(e => e.id === eventId);
        if (eventIndex === -1) return;

        // const event = draft.conflictingEvents[eventIndex]; // 暫時不使用
        
        switch (resolution) {
          case ConflictResolution.LOCAL_WINS:
            // 忽略遠端更新
            break;
          case ConflictResolution.REMOTE_WINS:
            // 應用遠端更新
            // 這裡需要根據事件類型進行處理
            break;
          case ConflictResolution.LAST_UPDATED_WINS:
            // 根據時間戳決定
            // 實作邏輯取決於具體的資料結構
            break;
          case ConflictResolution.MANUAL_MERGE:
            // 手動合併，需要額外的 UI 支援
            break;
        }

        draft.conflictingEvents.splice(eventIndex, 1);
        draft.stats.totalConflicts += 1;
      });

      logDebug(`衝突已解決: ${eventId}, 策略: ${resolution}`);
    },

    addConflictingEvent: (event: SyncEvent) => {
      set((draft) => {
        draft.conflictingEvents.push(event);
      });

      logDebug('發現衝突事件', event);
    },

    // === 佇列管理 ===
    addPendingEvent: (event: SyncEvent) => {
      set((draft) => {
        draft.pendingEvents.push(event);
      });

      logDebug('事件已加入待處理佇列', event);
    },

    processPendingEvents: async () => {
      const state = get();
      
      if (state.pendingEvents.length === 0) return;

      set((draft) => {
        draft.isSyncing = true;
      });

      try {
        const events = [...state.pendingEvents];
        
        for (const event of events) {
          switch (event.type) {
            case 'timer':
              await signalRService.syncTimer(event.data as Omit<TimerSyncEvent, 'timestamp'>);
              break;
            case 'task':
              const taskData = event.data as { taskId: UUID; changes: Record<string, unknown> };
              await signalRService.notifyTaskUpdate(taskData.taskId, taskData.changes);
              break;
            case 'progress':
              await signalRService.notifyProgressUpdate(event.data as Omit<ProgressUpdateEvent, 'timestamp'>);
              break;
          }
        }

        set((draft) => {
          draft.pendingEvents = [];
          draft.stats.totalEventsSent += events.length;
        });

        logDebug(`已處理 ${events.length} 個待處理事件`);
      } catch (error) {
        get().addSyncError(`處理待處理事件失敗: ${error instanceof Error ? error.message : '未知錯誤'}`);
      } finally {
        set((draft) => {
          draft.isSyncing = false;
        });
      }
    },

    clearPendingEvents: () => {
      set((draft) => {
        draft.pendingEvents = [];
      });
    },

    // === 配置管理 ===
    updateConfig: (config: Partial<SyncConfig>) => {
      set((draft) => {
        draft.config = { ...draft.config, ...config };
      });
    },

    resetConfig: () => {
      set((draft) => {
        draft.config = DEFAULT_CONFIG;
      });
    },

    // === 統計管理 ===
    updateStats: (updates: Partial<RealtimeSyncState['stats']>) => {
      set((draft) => {
        draft.stats = { ...draft.stats, ...updates };
      });
    },

    resetStats: () => {
      set((draft) => {
        draft.stats = {
          totalEventsReceived: 0,
          totalEventsSent: 0,
          totalConflicts: 0,
          averageLatency: 0,
          syncSuccessRate: 0
        };
      });
    },

    // === 訂閱管理 ===
    subscribe: (storeId: string, callback: () => void) => {
      set((draft) => {
        draft.subscriptions.set(storeId, callback);
      });

      return () => {
        get().unsubscribe(storeId);
      };
    },

    unsubscribe: (storeId: string) => {
      set((draft) => {
        draft.subscriptions.delete(storeId);
      });
    },

    // === 錯誤處理 ===
    addSyncError: (error: string) => {
      set((draft) => {
        draft.syncErrors.push(`${new Date().toISOString()}: ${error}`);
        // 只保留最近 50 個錯誤
        if (draft.syncErrors.length > 50) {
          draft.syncErrors = draft.syncErrors.slice(-50);
        }
      });
    },

    clearSyncErrors: () => {
      set((draft) => {
        draft.syncErrors = [];
      });
    },

    // === 調試和監控 ===
    getDebugInfo: () => {
      const state = get();
      return {
        status: state.status,
        lastConnectedAt: state.lastConnectedAt,
        lastSyncAt: state.lastSyncAt,
        pendingEventsCount: state.pendingEvents.length,
        conflictingEventsCount: state.conflictingEvents.length,
        stats: state.stats,
        config: state.config,
        recentErrors: state.syncErrors.slice(-5)
      };
    },

    exportSyncLog: () => {
      const state = get();
      return [...state.recentEvents].reverse(); // 按時間順序排列
    },


  }))
);

// 自動初始化同步服務
if (typeof window !== 'undefined') {
  // 在瀏覽器環境中自動連線
  setTimeout(() => {
    useRealtimeSyncStore.getState().connect();
  }, 1000);

  // 監聽網路狀態變化
  window.addEventListener('online', () => {
    const state = useRealtimeSyncStore.getState();
    if (state.status === SyncStatus.DISCONNECTED || state.status === SyncStatus.ERROR) {
      state.connect();
    }
  });

  window.addEventListener('offline', () => {
    useRealtimeSyncStore.getState().disconnect();
  });

  // 頁面卸載時清理
  window.addEventListener('beforeunload', () => {
    useRealtimeSyncStore.getState().disconnect();
  });
}

// 輔助函數
const checkForConflicts = (event: SyncEvent, state: RealtimeSyncState): boolean => {
  // 簡單的衝突檢測邏輯
  // 在實際應用中，這需要更複雜的邏輯來檢測資料衝突
  if (event.type === 'task') {
    const taskEvent = event.data as TaskUpdateEvent;
    // 檢查是否有本地未同步的變更
    const hasPendingTaskUpdates = state.pendingEvents.some(
      e => e.type === 'task' && 
      (e.data as { taskId: UUID }).taskId === taskEvent.taskId
    );
    
    return hasPendingTaskUpdates;
  }

  return false;
};

export default useRealtimeSyncStore;