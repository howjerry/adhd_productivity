import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { vi } from 'vitest';
import { TimerSettings } from '../TimerSettings';

describe('TimerSettings', () => {
  const mockSettings = {
    workDuration: 25,
    shortBreakDuration: 5,
    longBreakDuration: 15,
    sessionsUntilLongBreak: 4,
    autoStartBreaks: false,
    autoStartPomodoros: false,
    soundEnabled: true,
    desktopNotifications: true,
  };

  const mockOnSettingsUpdate = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders all settings fields', () => {
    render(<TimerSettings settings={mockSettings} onSettingsUpdate={mockOnSettingsUpdate} />);

    expect(screen.getByText('Work Duration (min)')).toBeInTheDocument();
    expect(screen.getByText('Short Break (min)')).toBeInTheDocument();
    expect(screen.getByText('Long Break (min)')).toBeInTheDocument();
    expect(screen.getByText('Sessions until Long Break')).toBeInTheDocument();
    expect(screen.getByText('Auto-start Breaks')).toBeInTheDocument();
    expect(screen.getByText('Auto-start Pomodoros')).toBeInTheDocument();
    expect(screen.getByText('Sound Notifications')).toBeInTheDocument();
    expect(screen.getByText('Desktop Notifications')).toBeInTheDocument();
  });

  it('displays current settings values', () => {
    render(<TimerSettings settings={mockSettings} onSettingsUpdate={mockOnSettingsUpdate} />);

    expect(screen.getByDisplayValue('25')).toBeInTheDocument();
    expect(screen.getByDisplayValue('5')).toBeInTheDocument();
    expect(screen.getByDisplayValue('15')).toBeInTheDocument();
    expect(screen.getByDisplayValue('4')).toBeInTheDocument();
  });

  it('updates work duration when changed', () => {
    render(<TimerSettings settings={mockSettings} onSettingsUpdate={mockOnSettingsUpdate} />);

    const workDurationInput = screen.getByDisplayValue('25');
    fireEvent.change(workDurationInput, { target: { value: '30' } });

    expect(mockOnSettingsUpdate).toHaveBeenCalledWith({ workDuration: 30 });
  });

  it('updates short break duration when changed', () => {
    render(<TimerSettings settings={mockSettings} onSettingsUpdate={mockOnSettingsUpdate} />);

    const shortBreakInput = screen.getByDisplayValue('5');
    fireEvent.change(shortBreakInput, { target: { value: '10' } });

    expect(mockOnSettingsUpdate).toHaveBeenCalledWith({ shortBreakDuration: 10 });
  });

  it('updates long break duration when changed', () => {
    render(<TimerSettings settings={mockSettings} onSettingsUpdate={mockOnSettingsUpdate} />);

    const longBreakInput = screen.getByDisplayValue('15');
    fireEvent.change(longBreakInput, { target: { value: '20' } });

    expect(mockOnSettingsUpdate).toHaveBeenCalledWith({ longBreakDuration: 20 });
  });

  it('updates sessions until long break when changed', () => {
    render(<TimerSettings settings={mockSettings} onSettingsUpdate={mockOnSettingsUpdate} />);

    const sessionsInput = screen.getByDisplayValue('4');
    fireEvent.change(sessionsInput, { target: { value: '6' } });

    expect(mockOnSettingsUpdate).toHaveBeenCalledWith({ sessionsUntilLongBreak: 6 });
  });

  it('handles invalid number input gracefully', () => {
    render(<TimerSettings settings={mockSettings} onSettingsUpdate={mockOnSettingsUpdate} />);

    const workDurationInput = screen.getByDisplayValue('25');
    fireEvent.change(workDurationInput, { target: { value: '' } });

    // Should default to 25 when empty
    expect(mockOnSettingsUpdate).toHaveBeenCalledWith({ workDuration: 25 });
  });

  it('updates checkbox settings correctly', () => {
    render(<TimerSettings settings={mockSettings} onSettingsUpdate={mockOnSettingsUpdate} />);

    // Find checkboxes by their label text
    const autoStartBreaksCheckbox = screen.getByText('Auto-start Breaks').parentElement?.querySelector('input[type="checkbox"]');
    const soundCheckbox = screen.getByText('Sound Notifications').parentElement?.querySelector('input[type="checkbox"]');

    if (autoStartBreaksCheckbox) {
      fireEvent.click(autoStartBreaksCheckbox);
      expect(mockOnSettingsUpdate).toHaveBeenCalledWith({ autoStartBreaks: true });
    }

    if (soundCheckbox) {
      fireEvent.click(soundCheckbox);
      expect(mockOnSettingsUpdate).toHaveBeenCalledWith({ soundEnabled: false });
    }
  });

  it('enforces min/max values on number inputs', () => {
    render(<TimerSettings settings={mockSettings} onSettingsUpdate={mockOnSettingsUpdate} />);

    const workDurationInput = screen.getByDisplayValue('25') as HTMLInputElement;
    expect(workDurationInput.min).toBe('1');
    expect(workDurationInput.max).toBe('60');

    const shortBreakInput = screen.getByDisplayValue('5') as HTMLInputElement;
    expect(shortBreakInput.min).toBe('1');
    expect(shortBreakInput.max).toBe('30');

    const longBreakInput = screen.getByDisplayValue('15') as HTMLInputElement;
    expect(longBreakInput.min).toBe('1');
    expect(longBreakInput.max).toBe('60');

    const sessionsInput = screen.getByDisplayValue('4') as HTMLInputElement;
    expect(sessionsInput.min).toBe('2');
    expect(sessionsInput.max).toBe('10');
  });

  it('updates local state when props change', () => {
    const { rerender } = render(<TimerSettings settings={mockSettings} onSettingsUpdate={mockOnSettingsUpdate} />);

    const newSettings = {
      ...mockSettings,
      workDuration: 30,
      shortBreakDuration: 10,
    };

    rerender(<TimerSettings settings={newSettings} onSettingsUpdate={mockOnSettingsUpdate} />);

    expect(screen.getByDisplayValue('30')).toBeInTheDocument();
    expect(screen.getByDisplayValue('10')).toBeInTheDocument();
  });
});