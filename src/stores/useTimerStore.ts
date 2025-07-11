import { create } from 'zustand';
import { immer } from 'zustand/middleware/immer';
import { TimerState, UUID } from '@/types';

interface TimerStoreState extends TimerState {
  settings: {
    workDuration: number;      // minutes
    shortBreakDuration: number; // minutes
    longBreakDuration: number;  // minutes
    sessionsUntilLongBreak: number;
    autoStartBreaks: boolean;
    autoStartPomodoros: boolean;
    soundEnabled: boolean;
    desktopNotifications: boolean;
  };
  statistics: {
    todayCompletedSessions: number;
    todayFocusMinutes: number;
    weeklyFocusMinutes: number;
    totalSessions: number;
  };
  isInitialized: boolean;
}

interface TimerActions {
  // Timer controls
  start: (taskId?: UUID) => void;
  pause: () => void;
  resume: () => void;
  stop: () => void;
  reset: () => void;
  skip: () => void;
  
  // Mode switching
  switchToWork: (taskId?: UUID) => void;
  switchToShortBreak: () => void;
  switchToLongBreak: () => void;
  
  // Settings management
  updateSettings: (settings: Partial<TimerStoreState['settings']>) => void;
  
  // Statistics
  recordCompletedSession: () => void;
  updateTodayStats: (focusMinutes: number) => void;
  
  // Internal timer management
  tick: () => void;
  complete: () => void;
  
  // Initialization
  initialize: () => void;
}

type TimerStore = TimerStoreState & TimerActions;

const initialState: TimerStoreState = {
  isRunning: false,
  isPaused: false,
  mode: 'work',
  timeRemaining: 25 * 60, // 25 minutes in seconds
  totalDuration: 25 * 60,
  sessionCount: 0,
  currentTaskId: undefined,
  startedAt: undefined,
  settings: {
    workDuration: 25,
    shortBreakDuration: 5,
    longBreakDuration: 15,
    sessionsUntilLongBreak: 4,
    autoStartBreaks: false,
    autoStartPomodoros: false,
    soundEnabled: true,
    desktopNotifications: true,
  },
  statistics: {
    todayCompletedSessions: 0,
    todayFocusMinutes: 0,
    weeklyFocusMinutes: 0,
    totalSessions: 0,
  },
  isInitialized: false,
};

let timerInterval: ReturnType<typeof setInterval> | null = null;

export const useTimerStore = create<TimerStore>()(
  immer((set, get) => ({
    ...initialState,

    start: (taskId?: UUID) => {
      const state = get();
      
      set((draft) => {
        draft.isRunning = true;
        draft.isPaused = false;
        draft.startedAt = new Date().toISOString();
        if (taskId) {
          draft.currentTaskId = taskId;
        }
      });

      // Clear existing interval
      if (timerInterval) {
        clearInterval(timerInterval);
      }

      // Start new interval
      timerInterval = setInterval(() => {
        get().tick();
      }, 1000);

      // Show notification
      if (state.settings.desktopNotifications && 'Notification' in window) {
        new Notification(`${state.mode === 'work' ? 'Focus' : 'Break'} session started!`, {
          icon: '/icon-192.png',
          tag: 'timer-start'
        });
      }
    },

    pause: () => {
      set((draft) => {
        draft.isPaused = true;
        draft.isRunning = false;
      });

      if (timerInterval) {
        clearInterval(timerInterval);
        timerInterval = null;
      }
    },

    resume: () => {
      set((draft) => {
        draft.isPaused = false;
        draft.isRunning = true;
      });

      // Start interval again
      timerInterval = setInterval(() => {
        get().tick();
      }, 1000);
    },

    stop: () => {
      set((draft) => {
        draft.isRunning = false;
        draft.isPaused = false;
        draft.startedAt = undefined;
      });

      if (timerInterval) {
        clearInterval(timerInterval);
        timerInterval = null;
      }
    },

    reset: () => {
      const state = get();
      
      get().stop();
      
      set((draft) => {
        draft.timeRemaining = state.settings.workDuration * 60;
        draft.totalDuration = state.settings.workDuration * 60;
        draft.mode = 'work';
        draft.currentTaskId = undefined;
      });
    },

    skip: () => {
      const state = get();
      
      get().stop();
      
      // Move to next phase
      if (state.mode === 'work') {
        const shouldTakeLongBreak = (state.sessionCount + 1) % state.settings.sessionsUntilLongBreak === 0;
        
        set((draft) => {
          draft.sessionCount += 1;
        });

        if (shouldTakeLongBreak) {
          get().switchToLongBreak();
        } else {
          get().switchToShortBreak();
        }
      } else {
        get().switchToWork();
      }

      // Auto-start if enabled
      const newState = get();
      if (
        (newState.mode !== 'work' && newState.settings.autoStartBreaks) ||
        (newState.mode === 'work' && newState.settings.autoStartPomodoros)
      ) {
        setTimeout(() => get().start(), 1000);
      }
    },

    switchToWork: (taskId?: UUID) => {
      const state = get();
      
      get().stop();
      
      set((draft) => {
        draft.mode = 'work';
        draft.timeRemaining = state.settings.workDuration * 60;
        draft.totalDuration = state.settings.workDuration * 60;
        if (taskId) {
          draft.currentTaskId = taskId;
        }
      });
    },

    switchToShortBreak: () => {
      const state = get();
      
      get().stop();
      
      set((draft) => {
        draft.mode = 'short_break';
        draft.timeRemaining = state.settings.shortBreakDuration * 60;
        draft.totalDuration = state.settings.shortBreakDuration * 60;
        draft.currentTaskId = undefined;
      });
    },

    switchToLongBreak: () => {
      const state = get();
      
      get().stop();
      
      set((draft) => {
        draft.mode = 'long_break';
        draft.timeRemaining = state.settings.longBreakDuration * 60;
        draft.totalDuration = state.settings.longBreakDuration * 60;
        draft.currentTaskId = undefined;
      });
    },

    updateSettings: (newSettings: Partial<TimerStoreState['settings']>) => {
      set((draft) => {
        draft.settings = { ...draft.settings, ...newSettings };
      });

      // Update current timer if not running
      const state = get();
      if (!state.isRunning && !state.isPaused) {
        const duration = state.mode === 'work' 
          ? state.settings.workDuration * 60
          : state.mode === 'short_break'
          ? state.settings.shortBreakDuration * 60
          : state.settings.longBreakDuration * 60;

        set((draft) => {
          draft.timeRemaining = duration;
          draft.totalDuration = duration;
        });
      }
    },

    recordCompletedSession: () => {
      set((draft) => {
        draft.statistics.todayCompletedSessions += 1;
        draft.statistics.totalSessions += 1;
      });
    },

    updateTodayStats: (focusMinutes: number) => {
      set((draft) => {
        draft.statistics.todayFocusMinutes += focusMinutes;
        draft.statistics.weeklyFocusMinutes += focusMinutes;
      });
    },

    tick: () => {
      const state = get();
      
      if (!state.isRunning || state.isPaused) {
        return;
      }

      if (state.timeRemaining <= 0) {
        get().complete();
        return;
      }

      set((draft) => {
        draft.timeRemaining -= 1;
      });
    },

    complete: () => {
      const state = get();
      
      get().stop();

      // Record completed session
      if (state.mode === 'work') {
        get().recordCompletedSession();
        get().updateTodayStats(Math.round(state.settings.workDuration));
        
        set((draft) => {
          draft.sessionCount += 1;
        });
      }

      // Play sound if enabled
      if (state.settings.soundEnabled) {
        // TODO: Play completion sound
        console.log('Timer completed - play sound');
      }

      // Show notification
      if (state.settings.desktopNotifications && 'Notification' in window) {
        const message = state.mode === 'work' 
          ? 'Focus session completed! Time for a break.'
          : 'Break time is over! Ready to focus?';
          
        new Notification(message, {
          icon: '/icon-192.png',
          tag: 'timer-complete'
        });
      }

      // Determine next phase
      const newState = get();
      if (state.mode === 'work') {
        const shouldTakeLongBreak = newState.sessionCount % state.settings.sessionsUntilLongBreak === 0;
        
        if (shouldTakeLongBreak) {
          get().switchToLongBreak();
        } else {
          get().switchToShortBreak();
        }
      } else {
        get().switchToWork();
      }

      // Auto-start next session if enabled
      const finalState = get();
      if (
        (finalState.mode !== 'work' && finalState.settings.autoStartBreaks) ||
        (finalState.mode === 'work' && finalState.settings.autoStartPomodoros)
      ) {
        setTimeout(() => get().start(), 3000); // 3 second delay
      }
    },

    initialize: () => {
      // Load saved statistics from localStorage or API
      const savedStats = localStorage.getItem('timer-statistics');
      if (savedStats) {
        try {
          const stats = JSON.parse(savedStats);
          set((draft) => {
            draft.statistics = { ...draft.statistics, ...stats };
          });
        } catch (error) {
          console.warn('Failed to load timer statistics:', error);
        }
      }

      // Request notification permission
      if ('Notification' in window && Notification.permission === 'default') {
        Notification.requestPermission();
      }

      set((draft) => {
        draft.isInitialized = true;
      });
    },
  }))
);

// Persist statistics to localStorage when they change
useTimerStore.subscribe((state) => {
  localStorage.setItem('timer-statistics', JSON.stringify(state.statistics));
});

// Clean up interval on store destruction
if (typeof window !== 'undefined') {
  window.addEventListener('beforeunload', () => {
    if (timerInterval) {
      clearInterval(timerInterval);
    }
  });
}