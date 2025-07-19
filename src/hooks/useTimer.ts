import { useEffect, useCallback } from 'react';
import { useTimerStore } from '@/stores/useTimerStore';
import { UUID } from '@/types';

export const useTimer = (currentTaskId?: UUID) => {
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
    statistics,
  } = useTimerStore();

  // 計算進度百分比
  const progress = totalDuration > 0 ? ((totalDuration - timeRemaining) / totalDuration) * 100 : 0;

  // 格式化時間顯示
  const formatTime = useCallback((seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  }, []);

  // 處理計時器控制
  const handlePlayPause = useCallback(() => {
    if (isRunning) {
      pause();
    } else if (isPaused) {
      resume();
    } else {
      start(currentTaskId);
    }
  }, [isRunning, isPaused, start, pause, resume, currentTaskId]);

  const handleStop = useCallback(() => {
    stop();
  }, [stop]);

  const handleSkip = useCallback(() => {
    skip();
  }, [skip]);

  // 請求通知權限
  useEffect(() => {
    if ('Notification' in window && Notification.permission === 'default') {
      Notification.requestPermission();
    }
  }, []);

  // 模式文字
  const modeText = {
    work: 'Focus Time',
    short_break: 'Short Break',
    long_break: 'Long Break',
  };

  return {
    // 狀態
    isRunning,
    isPaused,
    mode,
    timeRemaining,
    totalDuration,
    sessionCount,
    settings,
    statistics,
    progress,
    
    // 格式化函數
    formatTime,
    modeText,
    
    // 控制函數
    handlePlayPause,
    handleStop,
    handleSkip,
    updateSettings,
  };
};