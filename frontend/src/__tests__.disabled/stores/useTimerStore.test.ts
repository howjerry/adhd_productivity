import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { useTimerStore } from '../../stores/useTimerStore'

// Mock window.Audio
Object.defineProperty(window, 'Audio', {
  writable: true,
  value: vi.fn().mockImplementation(() => ({
    play: vi.fn(),
    pause: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    currentTime: 0,
    duration: 0,
  })),
})

// Mock localStorage
const localStorageMock = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn(),
}
Object.defineProperty(window, 'localStorage', {
  value: localStorageMock,
})

describe('useTimerStore', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.useFakeTimers()
    
    // Reset store state
    const { result } = renderHook(() => useTimerStore())
    act(() => {
      result.current.reset()
    })
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('initializes with correct default state', () => {
    const { result } = renderHook(() => useTimerStore())

    expect(result.current.timeLeft).toBe(25 * 60) // 25 minutes in seconds
    expect(result.current.isRunning).toBe(false)
    expect(result.current.isPaused).toBe(false)
    expect(result.current.currentSession).toBe(1)
    expect(result.current.sessionType).toBe('focus')
    expect(result.current.settings.focusDuration).toBe(25)
    expect(result.current.settings.shortBreakDuration).toBe(5)
    expect(result.current.settings.longBreakDuration).toBe(15)
  })

  it('starts the timer correctly', () => {
    const { result } = renderHook(() => useTimerStore())

    act(() => {
      result.current.start()
    })

    expect(result.current.isRunning).toBe(true)
    expect(result.current.isPaused).toBe(false)

    // Advance timer by 1 second
    act(() => {
      vi.advanceTimersByTime(1000)
    })

    expect(result.current.timeLeft).toBe(25 * 60 - 1)
  })

  it('pauses and resumes the timer', () => {
    const { result } = renderHook(() => useTimerStore())

    act(() => {
      result.current.start()
    })

    act(() => {
      result.current.pause()
    })

    expect(result.current.isRunning).toBe(false)
    expect(result.current.isPaused).toBe(true)

    act(() => {
      result.current.resume()
    })

    expect(result.current.isRunning).toBe(true)
    expect(result.current.isPaused).toBe(false)
  })

  it('stops and resets the timer', () => {
    const { result } = renderHook(() => useTimerStore())

    act(() => {
      result.current.start()
    })

    act(() => {
      vi.advanceTimersByTime(5000) // 5 seconds
    })

    act(() => {
      result.current.stop()
    })

    expect(result.current.isRunning).toBe(false)
    expect(result.current.isPaused).toBe(false)
    expect(result.current.timeLeft).toBe(25 * 60) // Reset to original time
  })

  it('completes a focus session and transitions to break', () => {
    const { result } = renderHook(() => useTimerStore())

    act(() => {
      result.current.start()
    })

    // Fast forward to complete the session
    act(() => {
      vi.advanceTimersByTime(25 * 60 * 1000)
    })

    expect(result.current.sessionType).toBe('short-break')
    expect(result.current.timeLeft).toBe(5 * 60) // 5 minutes break
    expect(result.current.isRunning).toBe(false)
  })

  it('cycles through sessions correctly', () => {
    const { result } = renderHook(() => useTimerStore())

    // Complete 4 focus sessions to trigger long break
    for (let i = 0; i < 4; i++) {
      act(() => {
        result.current.start()
        vi.advanceTimersByTime(25 * 60 * 1000) // Complete focus session
        
        if (i < 3) {
          result.current.start()
          vi.advanceTimersByTime(5 * 60 * 1000) // Complete short break
        }
      })
    }

    expect(result.current.sessionType).toBe('long-break')
    expect(result.current.timeLeft).toBe(15 * 60) // 15 minutes long break
  })

  it('updates settings and applies them immediately', () => {
    const { result } = renderHook(() => useTimerStore())

    const newSettings = {
      focusDuration: 30,
      shortBreakDuration: 10,
      longBreakDuration: 20,
      sessionsUntilLongBreak: 3,
      autoStartBreaks: true,
      autoStartFocus: false,
      soundEnabled: false,
      notificationsEnabled: true,
    }

    act(() => {
      result.current.updateSettings(newSettings)
    })

    expect(result.current.settings).toEqual(newSettings)
    expect(result.current.timeLeft).toBe(30 * 60) // Updated to new focus duration
    expect(localStorageMock.setItem).toHaveBeenCalledWith(
      'adhd-timer-settings',
      JSON.stringify(newSettings)
    )
  })

  it('persists and restores settings from localStorage', () => {
    const savedSettings = {
      focusDuration: 30,
      shortBreakDuration: 10,
      longBreakDuration: 20,
      sessionsUntilLongBreak: 3,
      autoStartBreaks: true,
      autoStartFocus: false,
      soundEnabled: false,
      notificationsEnabled: true,
    }

    localStorageMock.getItem.mockReturnValue(JSON.stringify(savedSettings))

    const { result } = renderHook(() => useTimerStore())

    expect(result.current.settings).toEqual(savedSettings)
    expect(result.current.timeLeft).toBe(30 * 60)
  })

  it('handles malformed localStorage data gracefully', () => {
    localStorageMock.getItem.mockReturnValue('invalid json')

    const { result } = renderHook(() => useTimerStore())

    // Should fallback to default settings
    expect(result.current.settings.focusDuration).toBe(25)
  })

  it('formats time correctly', () => {
    const { result } = renderHook(() => useTimerStore())

    expect(result.current.formattedTime).toBe('25:00')

    act(() => {
      result.current.start()
      vi.advanceTimersByTime(90000) // 1 minute 30 seconds
    })

    expect(result.current.formattedTime).toBe('23:30')
  })

  it('calculates progress percentage correctly', () => {
    const { result } = renderHook(() => useTimerStore())

    expect(result.current.progress).toBe(0)

    act(() => {
      result.current.start()
      vi.advanceTimersByTime(300000) // 5 minutes
    })

    expect(result.current.progress).toBe(20) // 5/25 * 100 = 20%
  })

  it('tracks total time correctly', () => {
    const { result } = renderHook(() => useTimerStore())

    act(() => {
      result.current.start()
      vi.advanceTimersByTime(600000) // 10 minutes
      result.current.stop()
    })

    expect(result.current.totalTimeSpent).toBe(600) // 10 minutes in seconds
  })

  it('auto-starts breaks when enabled', () => {
    const { result } = renderHook(() => useTimerStore())

    act(() => {
      result.current.updateSettings({
        ...result.current.settings,
        autoStartBreaks: true,
      })
    })

    act(() => {
      result.current.start()
      vi.advanceTimersByTime(25 * 60 * 1000) // Complete focus session
    })

    expect(result.current.isRunning).toBe(true) // Break should auto-start
    expect(result.current.sessionType).toBe('short-break')
  })

  it('handles notifications when enabled', () => {
    const mockNotification = vi.fn()
    Object.defineProperty(window, 'Notification', {
      value: mockNotification,
      writable: true,
    })

    const { result } = renderHook(() => useTimerStore())

    act(() => {
      result.current.updateSettings({
        ...result.current.settings,
        notificationsEnabled: true,
      })
    })

    act(() => {
      result.current.start()
      vi.advanceTimersByTime(25 * 60 * 1000) // Complete session
    })

    expect(mockNotification).toHaveBeenCalled()
  })

  it('plays sound when enabled', () => {
    const { result } = renderHook(() => useTimerStore())

    act(() => {
      result.current.updateSettings({
        ...result.current.settings,
        soundEnabled: true,
      })
    })

    act(() => {
      result.current.start()
      vi.advanceTimersByTime(25 * 60 * 1000) // Complete session
    })

    expect(window.Audio).toHaveBeenCalled()
  })

  it('maintains state consistency during rapid state changes', () => {
    const { result } = renderHook(() => useTimerStore())

    act(() => {
      result.current.start()
      result.current.pause()
      result.current.resume()
      result.current.stop()
      result.current.start()
    })

    expect(result.current.isRunning).toBe(true)
    expect(result.current.isPaused).toBe(false)
    expect(result.current.timeLeft).toBe(25 * 60)
  })
})