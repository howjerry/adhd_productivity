import React, { useState, useEffect } from 'react';

interface TimerSettings {
  workDuration: number;
  shortBreakDuration: number;
  longBreakDuration: number;
  sessionsUntilLongBreak: number;
  autoStartBreaks: boolean;
  autoStartPomodoros: boolean;
  soundEnabled: boolean;
  desktopNotifications: boolean;
}

interface TimerSettingsProps {
  settings: TimerSettings;
  onSettingsUpdate: (newSettings: Partial<TimerSettings>) => void;
}

export const TimerSettings: React.FC<TimerSettingsProps> = React.memo(({
  settings,
  onSettingsUpdate,
}) => {
  const [localSettings, setLocalSettings] = useState(settings);

  useEffect(() => {
    setLocalSettings(settings);
  }, [settings]);

  const handleSettingsUpdate = (field: keyof TimerSettings, value: any) => {
    const newSettings = { ...localSettings, [field]: value };
    setLocalSettings(newSettings);
    onSettingsUpdate({ [field]: value });
  };

  return (
    <div className="timer-settings">
      <div className="setting-group">
        <label className="setting-label">Work Duration (min)</label>
        <input
          type="number"
          className="setting-input"
          value={localSettings.workDuration}
          onChange={(e) => handleSettingsUpdate('workDuration', parseInt(e.target.value) || 25)}
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
          onChange={(e) => handleSettingsUpdate('shortBreakDuration', parseInt(e.target.value) || 5)}
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
          onChange={(e) => handleSettingsUpdate('longBreakDuration', parseInt(e.target.value) || 15)}
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
          onChange={(e) => handleSettingsUpdate('sessionsUntilLongBreak', parseInt(e.target.value) || 4)}
          min="2"
          max="10"
        />
      </div>

      <div className="setting-group setting-toggle">
        <label className="setting-label">Auto-start Breaks</label>
        <input
          type="checkbox"
          checked={localSettings.autoStartBreaks}
          onChange={(e) => handleSettingsUpdate('autoStartBreaks', e.target.checked)}
        />
      </div>

      <div className="setting-group setting-toggle">
        <label className="setting-label">Auto-start Pomodoros</label>
        <input
          type="checkbox"
          checked={localSettings.autoStartPomodoros}
          onChange={(e) => handleSettingsUpdate('autoStartPomodoros', e.target.checked)}
        />
      </div>

      <div className="setting-group setting-toggle">
        <label className="setting-label">Sound Notifications</label>
        <input
          type="checkbox"
          checked={localSettings.soundEnabled}
          onChange={(e) => handleSettingsUpdate('soundEnabled', e.target.checked)}
        />
      </div>

      <div className="setting-group setting-toggle">
        <label className="setting-label">Desktop Notifications</label>
        <input
          type="checkbox"
          checked={localSettings.desktopNotifications}
          onChange={(e) => handleSettingsUpdate('desktopNotifications', e.target.checked)}
        />
      </div>
    </div>
  );
});