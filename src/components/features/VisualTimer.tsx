import React, { useEffect, useState } from 'react';
import { useTimerStore } from '@/stores/useTimerStore';
import { UUID } from '@/types';
import clsx from 'clsx';
import { Play, Pause, Square, SkipForward, Settings } from 'lucide-react';

interface VisualTimerProps {
  compact?: boolean;
  showSettings?: boolean;
  currentTaskId?: UUID;
  className?: string;
}

export const VisualTimer: React.FC<VisualTimerProps> = ({
  compact = false,
  showSettings = false,
  currentTaskId,
  className,
}) => {
  const {
    isRunning,
    isPaused,
    mode,
    timeRemaining,
    totalDuration,
    sessionCount,
    settings,
    start,
    pause,
    resume,
    stop,
    skip,
    updateSettings,
  } = useTimerStore();

  const [showSettingsPanel, setShowSettingsPanel] = useState(false);
  const [localSettings, setLocalSettings] = useState(settings);

  // Calculate progress percentage
  const progress = totalDuration > 0 ? ((totalDuration - timeRemaining) / totalDuration) * 100 : 0;

  // Format time display
  const formatTime = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  // Calculate stroke dash offset for progress ring
  const circumference = 2 * Math.PI * 90; // radius = 90
  const strokeDashoffset = circumference - (progress / 100) * circumference;

  // Handle timer controls
  const handlePlayPause = () => {
    if (isRunning) {
      pause();
    } else if (isPaused) {
      resume();
    } else {
      start(currentTaskId);
    }
  };

  const handleStop = () => {
    stop();
  };

  const handleSkip = () => {
    skip();
  };

  // Handle settings updates
  const handleSettingsUpdate = (newSettings: Partial<typeof settings>) => {
    setLocalSettings(prev => ({ ...prev, ...newSettings }));
    updateSettings(newSettings);
  };

  // Close settings panel when timer starts
  useEffect(() => {
    if (isRunning && showSettingsPanel) {
      setShowSettingsPanel(false);
    }
  }, [isRunning, showSettingsPanel]);

  // Request notification permission on mount
  useEffect(() => {
    if ('Notification' in window && Notification.permission === 'default') {
      Notification.requestPermission();
    }
  }, []);

  const timerClasses = clsx(
    'visual-timer',
    {
      'visual-timer-compact': compact,
      'timer-running': isRunning,
      'timer-paused': isPaused,
      'timer-break': mode !== 'work',
    },
    className
  );

  const modeText = {
    work: 'Focus Time',
    short_break: 'Short Break',
    long_break: 'Long Break',
  };

  return (
    <div className={timerClasses}>
      {!compact && (
        <div className="timer-mode-indicators">
          <div className={clsx('mode-indicator', { active: mode === 'work', inactive: mode !== 'work' })}>
            Work
          </div>
          <div className={clsx('mode-indicator', { active: mode === 'short_break', inactive: mode !== 'short_break' })}>
            Break
          </div>
          <div className={clsx('mode-indicator', { active: mode === 'long_break', inactive: mode !== 'long_break' })}>
            Long Break
          </div>
        </div>
      )}

      <div className="timer-circle">
        <svg className="progress-ring" width="200" height="200">
          <circle
            className="progress-background"
            cx="100"
            cy="100"
            r="90"
          />
          <circle
            className={clsx('progress-foreground', { 'progress-ring-animate': isRunning })}
            cx="100"
            cy="100"
            r="90"
            style={{
              strokeDashoffset: strokeDashoffset,
            }}
          />
        </svg>
        
        <div className="timer-display">
          <div className="timer-time">{formatTime(timeRemaining)}</div>
          <div className="timer-mode">{modeText[mode]}</div>
        </div>
      </div>

      <div className="timer-controls">
        <button
          className={clsx('timer-btn', isRunning ? 'timer-pause' : 'timer-play')}
          onClick={handlePlayPause}
          aria-label={isRunning ? 'Pause timer' : isPaused ? 'Resume timer' : 'Start timer'}
        >
          {isRunning ? <Pause /> : <Play />}
        </button>

        <button
          className="timer-btn timer-stop"
          onClick={handleStop}
          disabled={!isRunning && !isPaused}
          aria-label="Stop timer"
        >
          <Square />
        </button>

        <button
          className="timer-btn timer-skip"
          onClick={handleSkip}
          aria-label="Skip to next phase"
        >
          <SkipForward />
        </button>

        {showSettings && (
          <button
            className="timer-btn"
            onClick={() => setShowSettingsPanel(!showSettingsPanel)}
            aria-label="Timer settings"
            style={{ background: showSettingsPanel ? '#6366f1' : '#6b7280' }}
          >
            <Settings />
          </button>
        )}
      </div>

      {!compact && (
        <div className="timer-info">
          <div className="timer-session-count">
            Session {sessionCount + 1}
          </div>
          {currentTaskId && (
            <div className="timer-task-name">
              Focus Task Active
            </div>
          )}
        </div>
      )}

      {showSettingsPanel && !compact && (
        <div className="timer-settings">
          <div className="setting-group">
            <label className="setting-label">Work Duration (min)</label>
            <input
              type="number"
              className="setting-input"
              value={localSettings.workDuration}
              onChange={(e) => handleSettingsUpdate({ workDuration: parseInt(e.target.value) || 25 })}
              min="1"
              max="60"
            />
          </div>

          <div className="setting-group">
            <label className="setting-label">Short Break (min)</label>
            <input
              type="number"
              className="setting-input"
              value={localSettings.shortBreakDuration}
              onChange={(e) => handleSettingsUpdate({ shortBreakDuration: parseInt(e.target.value) || 5 })}
              min="1"
              max="30"
            />
          </div>

          <div className="setting-group">
            <label className="setting-label">Long Break (min)</label>
            <input
              type="number"
              className="setting-input"
              value={localSettings.longBreakDuration}
              onChange={(e) => handleSettingsUpdate({ longBreakDuration: parseInt(e.target.value) || 15 })}
              min="1"
              max="60"
            />
          </div>

          <div className="setting-group">
            <label className="setting-label">Sessions until Long Break</label>
            <input
              type="number"
              className="setting-input"
              value={localSettings.sessionsUntilLongBreak}
              onChange={(e) => handleSettingsUpdate({ sessionsUntilLongBreak: parseInt(e.target.value) || 4 })}
              min="2"
              max="10"
            />
          </div>

          <div className="setting-group setting-toggle">
            <label className="setting-label">Auto-start Breaks</label>
            <input
              type="checkbox"
              checked={localSettings.autoStartBreaks}
              onChange={(e) => handleSettingsUpdate({ autoStartBreaks: e.target.checked })}
            />
          </div>

          <div className="setting-group setting-toggle">
            <label className="setting-label">Auto-start Pomodoros</label>
            <input
              type="checkbox"
              checked={localSettings.autoStartPomodoros}
              onChange={(e) => handleSettingsUpdate({ autoStartPomodoros: e.target.checked })}
            />
          </div>

          <div className="setting-group setting-toggle">
            <label className="setting-label">Sound Notifications</label>
            <input
              type="checkbox"
              checked={localSettings.soundEnabled}
              onChange={(e) => handleSettingsUpdate({ soundEnabled: e.target.checked })}
            />
          </div>

          <div className="setting-group setting-toggle">
            <label className="setting-label">Desktop Notifications</label>
            <input
              type="checkbox"
              checked={localSettings.desktopNotifications}
              onChange={(e) => handleSettingsUpdate({ desktopNotifications: e.target.checked })}
            />
          </div>
        </div>
      )}
    </div>
  );
};

// Timer Stats Component
interface TimerStatsProps {
  className?: string;
}

export const TimerStats: React.FC<TimerStatsProps> = ({ className }) => {
  const { statistics } = useTimerStore();

  return (
    <div className={clsx('grid grid-cols-2 gap-4', className)}>
      <div className="text-center p-4 bg-white rounded-lg shadow">
        <div className="text-2xl font-bold text-blue-600">
          {statistics.todayCompletedSessions}
        </div>
        <div className="text-sm text-gray-600">Sessions Today</div>
      </div>
      
      <div className="text-center p-4 bg-white rounded-lg shadow">
        <div className="text-2xl font-bold text-green-600">
          {statistics.todayFocusMinutes}
        </div>
        <div className="text-sm text-gray-600">Focus Minutes</div>
      </div>
      
      <div className="text-center p-4 bg-white rounded-lg shadow">
        <div className="text-2xl font-bold text-purple-600">
          {Math.round(statistics.weeklyFocusMinutes / 60)}
        </div>
        <div className="text-sm text-gray-600">Hours This Week</div>
      </div>
      
      <div className="text-center p-4 bg-white rounded-lg shadow">
        <div className="text-2xl font-bold text-orange-600">
          {statistics.totalSessions}
        </div>
        <div className="text-sm text-gray-600">Total Sessions</div>
      </div>
    </div>
  );
};

export default VisualTimer;