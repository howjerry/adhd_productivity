import { renderHook, act } from '@testing-library/react';
import { vi } from 'vitest';
import { useTimer } from '../useTimer';
import { useTimerStore } from '@/stores/useTimerStore';

// Mock the timer store
vi.mock('@/stores/useTimerStore');

const mockUseTimerStore = useTimerStore as unknown as ReturnType<typeof vi.fn>;

describe('useTimer', () => {
  const mockStore = {
    isRunning: false,
    isPaused: false,
    mode: 'work' as const,
    timeRemaining: 1500, // 25 minutes
    totalDuration: 1500,
    sessionCount: 0,
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
      todayCompletedSessions: 5,
      todayFocusMinutes: 125,
      weeklyFocusMinutes: 600,
      totalSessions: 50,
    },
    start: vi.fn(),
    pause: vi.fn(),
    resume: vi.fn(),
    stop: vi.fn(),
    skip: vi.fn(),
    updateSettings: vi.fn(),
  };

  beforeEach(() => {
    mockUseTimerStore.mockReturnValue(mockStore);
    // Mock Notification API
    Object.defineProperty(window, 'Notification', {
      value: {
        permission: 'default',
        requestPermission: vi.fn(),
      },
      writable: true,
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('returns timer state from store', () => {
    const { result } = renderHook(() => useTimer());

    expect(result.current.isRunning).toBe(false);
    expect(result.current.isPaused).toBe(false);
    expect(result.current.mode).toBe('work');
    expect(result.current.timeRemaining).toBe(1500);
    expect(result.current.sessionCount).toBe(0);
  });

  it('calculates progress correctly', () => {
    const { result } = renderHook(() => useTimer());

    // Full time remaining = 0% progress
    expect(result.current.progress).toBe(0);

    // Half time remaining = 50% progress
    mockUseTimerStore.mockReturnValue({
      ...mockStore,
      timeRemaining: 750,
      totalDuration: 1500,
    });
    const { result: halfResult } = renderHook(() => useTimer());
    expect(halfResult.current.progress).toBe(50);
  });

  it('formats time correctly', () => {
    const { result } = renderHook(() => useTimer());

    expect(result.current.formatTime(0)).toBe('00:00');
    expect(result.current.formatTime(60)).toBe('01:00');
    expect(result.current.formatTime(90)).toBe('01:30');
    expect(result.current.formatTime(1500)).toBe('25:00');
  });

  it('provides correct mode text', () => {
    const { result } = renderHook(() => useTimer());

    expect(result.current.modeText.work).toBe('Focus Time');
    expect(result.current.modeText.short_break).toBe('Short Break');
    expect(result.current.modeText.long_break).toBe('Long Break');
  });

  it('handles play/pause correctly when timer is stopped', () => {
    const taskId = 'test-task-id';
    const { result } = renderHook(() => useTimer(taskId));

    act(() => {
      result.current.handlePlayPause();
    });

    expect(mockStore.start).toHaveBeenCalledWith(taskId);
    expect(mockStore.pause).not.toHaveBeenCalled();
    expect(mockStore.resume).not.toHaveBeenCalled();
  });

  it('handles play/pause correctly when timer is running', () => {
    mockUseTimerStore.mockReturnValue({
      ...mockStore,
      isRunning: true,
    });

    const { result } = renderHook(() => useTimer());

    act(() => {
      result.current.handlePlayPause();
    });

    expect(mockStore.pause).toHaveBeenCalled();
    expect(mockStore.start).not.toHaveBeenCalled();
    expect(mockStore.resume).not.toHaveBeenCalled();
  });

  it('handles play/pause correctly when timer is paused', () => {
    mockUseTimerStore.mockReturnValue({
      ...mockStore,
      isPaused: true,
    });

    const { result } = renderHook(() => useTimer());

    act(() => {
      result.current.handlePlayPause();
    });

    expect(mockStore.resume).toHaveBeenCalled();
    expect(mockStore.start).not.toHaveBeenCalled();
    expect(mockStore.pause).not.toHaveBeenCalled();
  });

  it('handles stop action', () => {
    const { result } = renderHook(() => useTimer());

    act(() => {
      result.current.handleStop();
    });

    expect(mockStore.stop).toHaveBeenCalled();
  });

  it('handles skip action', () => {
    const { result } = renderHook(() => useTimer());

    act(() => {
      result.current.handleSkip();
    });

    expect(mockStore.skip).toHaveBeenCalled();
  });

  it('passes through updateSettings function', () => {
    const { result } = renderHook(() => useTimer());
    const newSettings = { workDuration: 30 };

    act(() => {
      result.current.updateSettings(newSettings);
    });

    expect(mockStore.updateSettings).toHaveBeenCalledWith(newSettings);
  });

  it('requests notification permission on mount', () => {
    renderHook(() => useTimer());

    expect(window.Notification.requestPermission).toHaveBeenCalled();
  });

  it('does not request notification permission if already granted', () => {
    Object.defineProperty(window.Notification, 'permission', {
      value: 'granted',
      writable: true,
    });

    renderHook(() => useTimer());

    expect(window.Notification.requestPermission).not.toHaveBeenCalled();
  });

  it('returns statistics from store', () => {
    const { result } = renderHook(() => useTimer());

    expect(result.current.statistics).toEqual({
      todayCompletedSessions: 5,
      todayFocusMinutes: 125,
      weeklyFocusMinutes: 600,
      totalSessions: 50,
    });
  });
});