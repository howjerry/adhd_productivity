import React, { useState, useEffect } from 'react';
import { UUID } from '@/types';
import clsx from 'clsx';
import { Play, Pause, Square, SkipForward, Settings } from 'lucide-react';
import { useTimer } from '@/hooks/useTimer';
import { TimerSettings } from './TimerSettings';

interface VisualTimerProps {
  compact?: boolean;
  showSettings?: boolean;
  currentTaskId?: UUID;
  className?: string;
}

export const VisualTimer: React.FC<VisualTimerProps> = React.memo(({
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
    sessionCount,
    settings,
    progress,
    formatTime,
    modeText,
    handlePlayPause,
    handleStop,
    handleSkip,
    updateSettings,
  } = useTimer(currentTaskId);

  const [showSettingsPanel, setShowSettingsPanel] = useState(false);

  // 計算進度環的 stroke dash offset
  const circumference = 2 * Math.PI * 90; // radius = 90
  const strokeDashoffset = circumference - (progress / 100) * circumference;

  // 當計時器開始時關閉設定面板
  useEffect(() => {
    if (isRunning && showSettingsPanel) {
      setShowSettingsPanel(false);
    }
  }, [isRunning, showSettingsPanel]);

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
        <TimerSettings
          settings={settings}
          onSettingsUpdate={updateSettings}
        />
      )}
    </div>
  );
});

export default VisualTimer;