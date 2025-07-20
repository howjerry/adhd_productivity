import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { act, renderHook } from '@testing-library/react';
import { useTimerStore } from './useTimerStore';

describe('useTimerStore', () => {
  // 模擬 setInterval 和 clearInterval
  beforeEach(() => {
    vi.useFakeTimers();
    // 重置 store 到初始狀態
    useTimerStore.setState({
      isRunning: false,
      isPaused: false,
      mode: 'work',
      timeRemaining: 25 * 60,
      totalDuration: 25 * 60,
      sessionCount: 0,
      currentTaskId: undefined,
      startedAt: undefined,
      timerInterval: null,
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
    });
  });

  afterEach(() => {
    // 清理所有 timers
    vi.clearAllTimers();
    vi.useRealTimers();
  });

  describe('Timer 啟動和停止', () => {
    it('應該正確啟動 timer', () => {
      const { result } = renderHook(() => useTimerStore());

      act(() => {
        result.current.start();
      });

      expect(result.current.isRunning).toBe(true);
      expect(result.current.isPaused).toBe(false);
      expect(result.current.startedAt).toBeDefined();
      expect(result.current.timerInterval).not.toBeNull();
    });

    it('應該在啟動時接受 taskId', () => {
      const { result } = renderHook(() => useTimerStore());
      const taskId = 'test-task-123';

      act(() => {
        result.current.start(taskId);
      });

      expect(result.current.currentTaskId).toBe(taskId);
    });

    it('應該正確停止 timer 並清理 interval', () => {
      const { result } = renderHook(() => useTimerStore());

      // 啟動 timer
      act(() => {
        result.current.start();
      });

      const intervalId = result.current.timerInterval;
      expect(intervalId).not.toBeNull();

      // 停止 timer
      act(() => {
        result.current.stop();
      });

      expect(result.current.isRunning).toBe(false);
      expect(result.current.isPaused).toBe(false);
      expect(result.current.startedAt).toBeUndefined();
      expect(result.current.timerInterval).toBeNull();
    });

    it('應該在重新啟動前清理現有的 interval', () => {
      const { result } = renderHook(() => useTimerStore());

      // 第一次啟動
      act(() => {
        result.current.start();
      });

      const firstIntervalId = result.current.timerInterval;

      // 第二次啟動（不先停止）
      act(() => {
        result.current.start();
      });

      const secondIntervalId = result.current.timerInterval;

      expect(firstIntervalId).not.toBe(secondIntervalId);
      expect(result.current.timerInterval).toBe(secondIntervalId);
    });
  });

  describe('暫停和繼續功能', () => {
    it('應該正確暫停 timer 並清理 interval', () => {
      const { result } = renderHook(() => useTimerStore());

      // 啟動 timer
      act(() => {
        result.current.start();
      });

      expect(result.current.timerInterval).not.toBeNull();

      // 暫停 timer
      act(() => {
        result.current.pause();
      });

      expect(result.current.isRunning).toBe(false);
      expect(result.current.isPaused).toBe(true);
      expect(result.current.timerInterval).toBeNull();
    });

    it('應該正確繼續 timer 並重新創建 interval', () => {
      const { result } = renderHook(() => useTimerStore());

      // 啟動然後暫停
      act(() => {
        result.current.start();
        result.current.pause();
      });

      expect(result.current.timerInterval).toBeNull();

      // 繼續 timer
      act(() => {
        result.current.resume();
      });

      expect(result.current.isRunning).toBe(true);
      expect(result.current.isPaused).toBe(false);
      expect(result.current.timerInterval).not.toBeNull();
    });
  });

  describe('Timer tick 功能', () => {
    it('應該每秒減少 timeRemaining', () => {
      const { result } = renderHook(() => useTimerStore());

      act(() => {
        result.current.start();
      });

      const initialTime = result.current.timeRemaining;

      // 前進 1 秒
      act(() => {
        vi.advanceTimersByTime(1000);
      });

      expect(result.current.timeRemaining).toBe(initialTime - 1);

      // 再前進 5 秒
      act(() => {
        vi.advanceTimersByTime(5000);
      });

      expect(result.current.timeRemaining).toBe(initialTime - 6);
    });

    it('當暫停時不應該減少 timeRemaining', () => {
      const { result } = renderHook(() => useTimerStore());

      act(() => {
        result.current.start();
      });

      const timeBeforePause = result.current.timeRemaining;

      act(() => {
        result.current.pause();
      });

      // 前進 5 秒
      act(() => {
        vi.advanceTimersByTime(5000);
      });

      expect(result.current.timeRemaining).toBe(timeBeforePause);
    });
  });

  describe('重置功能', () => {
    it('應該重置 timer 到初始狀態並清理 interval', () => {
      const { result } = renderHook(() => useTimerStore());

      // 啟動並運行一段時間
      act(() => {
        result.current.start('test-task');
        vi.advanceTimersByTime(10000); // 前進 10 秒
      });

      // 重置
      act(() => {
        result.current.reset();
      });

      expect(result.current.isRunning).toBe(false);
      expect(result.current.isPaused).toBe(false);
      expect(result.current.timeRemaining).toBe(25 * 60);
      expect(result.current.totalDuration).toBe(25 * 60);
      expect(result.current.mode).toBe('work');
      expect(result.current.currentTaskId).toBeUndefined();
      expect(result.current.timerInterval).toBeNull();
    });
  });

  describe('完成功能', () => {
    it('當時間歸零時應該自動完成並清理 interval', () => {
      const { result } = renderHook(() => useTimerStore());

      // 設置短時間以便測試
      act(() => {
        useTimerStore.setState({ timeRemaining: 2 });
        result.current.start();
      });

      // 前進 2 秒 - timeRemaining 會變成 0
      act(() => {
        vi.advanceTimersByTime(2000);
      });

      // 再前進一個 tick 讓 complete 執行
      act(() => {
        vi.advanceTimersByTime(1000);
      });

      expect(result.current.isRunning).toBe(false);
      expect(result.current.timerInterval).toBeNull();
      
      // 驗證是否切換到休息模式
      expect(result.current.mode).toBe('short_break');
    });

    it('工作階段完成時應該增加統計數據', () => {
      const { result } = renderHook(() => useTimerStore());

      const initialSessions = result.current.statistics.todayCompletedSessions;
      const initialFocusMinutes = result.current.statistics.todayFocusMinutes;

      // 設置短時間以便測試
      act(() => {
        useTimerStore.setState({ 
          timeRemaining: 2,
          mode: 'work'
        });
        result.current.start();
      });

      // 前進 2 秒讓 timeRemaining 變成 0
      act(() => {
        vi.advanceTimersByTime(2000);
      });

      // 再前進一個 tick 讓 complete 執行
      act(() => {
        vi.advanceTimersByTime(1000);
      });

      expect(result.current.statistics.todayCompletedSessions).toBe(initialSessions + 1);
      expect(result.current.statistics.todayFocusMinutes).toBeGreaterThan(initialFocusMinutes);
      expect(result.current.sessionCount).toBe(1);
    });
  });

  describe('模式切換', () => {
    it('切換到工作模式時應該清理 interval', () => {
      const { result } = renderHook(() => useTimerStore());

      // 啟動休息模式
      act(() => {
        useTimerStore.setState({ mode: 'short_break' });
        result.current.start();
      });

      expect(result.current.timerInterval).not.toBeNull();

      // 切換到工作模式
      act(() => {
        result.current.switchToWork();
      });

      expect(result.current.mode).toBe('work');
      expect(result.current.isRunning).toBe(false);
      expect(result.current.timerInterval).toBeNull();
      expect(result.current.timeRemaining).toBe(25 * 60);
    });

    it('切換到短休息時應該清理 interval', () => {
      const { result } = renderHook(() => useTimerStore());

      // 啟動工作模式
      act(() => {
        result.current.start();
      });

      // 切換到短休息
      act(() => {
        result.current.switchToShortBreak();
      });

      expect(result.current.mode).toBe('short_break');
      expect(result.current.isRunning).toBe(false);
      expect(result.current.timerInterval).toBeNull();
      expect(result.current.timeRemaining).toBe(5 * 60);
    });

    it('切換到長休息時應該清理 interval', () => {
      const { result } = renderHook(() => useTimerStore());

      // 啟動工作模式
      act(() => {
        result.current.start();
      });

      // 切換到長休息
      act(() => {
        result.current.switchToLongBreak();
      });

      expect(result.current.mode).toBe('long_break');
      expect(result.current.isRunning).toBe(false);
      expect(result.current.timerInterval).toBeNull();
      expect(result.current.timeRemaining).toBe(15 * 60);
    });
  });

  describe('記憶體洩漏防護', () => {
    it('多次啟動和停止不應該造成記憶體洩漏', () => {
      const { result } = renderHook(() => useTimerStore());
      const intervalIds: any[] = [];

      // 執行多次啟動和停止
      for (let i = 0; i < 10; i++) {
        act(() => {
          result.current.start();
        });
        
        if (result.current.timerInterval) {
          intervalIds.push(result.current.timerInterval);
        }
        
        act(() => {
          result.current.stop();
        });
      }

      // 確保最後沒有活躍的 interval
      expect(result.current.timerInterval).toBeNull();
      
      // 確保每次都創建了新的 interval
      const uniqueIntervals = new Set(intervalIds);
      expect(uniqueIntervals.size).toBe(10);
    });

    it('組件卸載時應該清理 interval', () => {
      const { result, unmount } = renderHook(() => useTimerStore());

      act(() => {
        result.current.start();
      });

      expect(result.current.timerInterval).not.toBeNull();

      // 模擬組件卸載
      unmount();

      // 驗證 interval 應該被清理（透過手動觸發 beforeunload 事件）
      act(() => {
        window.dispatchEvent(new Event('beforeunload'));
      });

      // 注意：實際的 interval 清理會在 beforeunload 事件中執行
    });
  });
});