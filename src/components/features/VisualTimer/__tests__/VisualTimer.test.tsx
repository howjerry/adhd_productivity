import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { vi } from 'vitest';
import { VisualTimer } from '../index';
import { useTimer } from '@/hooks/useTimer';

// Mock the useTimer hook
vi.mock('@/hooks/useTimer');

const mockUseTimer = useTimer as unknown as ReturnType<typeof vi.fn>;

describe('VisualTimer', () => {
  const mockTimerData = {
    isRunning: false,
    isPaused: false,
    mode: 'work' as const,
    timeRemaining: 1500,
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
    progress: 0,
    formatTime: (seconds: number) => {
      const mins = Math.floor(seconds / 60);
      const secs = seconds % 60;
      return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    },
    modeText: {
      work: 'Focus Time',
      short_break: 'Short Break',
      long_break: 'Long Break',
    },
    handlePlayPause: vi.fn(),
    handleStop: vi.fn(),
    handleSkip: vi.fn(),
    updateSettings: vi.fn(),
  };

  beforeEach(() => {
    mockUseTimer.mockReturnValue(mockTimerData);
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders timer display with correct time', () => {
    render(<VisualTimer />);
    expect(screen.getByText('25:00')).toBeInTheDocument();
  });

  it('renders mode text correctly', () => {
    render(<VisualTimer />);
    expect(screen.getByText('Focus Time')).toBeInTheDocument();
  });

  it('shows mode indicators in non-compact mode', () => {
    render(<VisualTimer compact={false} />);
    expect(screen.getByText('Work')).toBeInTheDocument();
    expect(screen.getByText('Break')).toBeInTheDocument();
    expect(screen.getByText('Long Break')).toBeInTheDocument();
  });

  it('hides mode indicators in compact mode', () => {
    render(<VisualTimer compact={true} />);
    expect(screen.queryByText('Work')).not.toBeInTheDocument();
    expect(screen.queryByText('Break')).not.toBeInTheDocument();
    expect(screen.queryByText('Long Break')).not.toBeInTheDocument();
  });

  it('displays session count in non-compact mode', () => {
    render(<VisualTimer compact={false} />);
    expect(screen.getByText('Session 1')).toBeInTheDocument();
  });

  it('hides session count in compact mode', () => {
    render(<VisualTimer compact={true} />);
    expect(screen.queryByText('Session 1')).not.toBeInTheDocument();
  });

  it('shows play button when timer is stopped', () => {
    render(<VisualTimer />);
    const playButton = screen.getByLabelText('Start timer');
    expect(playButton).toBeInTheDocument();
    
    fireEvent.click(playButton);
    expect(mockTimerData.handlePlayPause).toHaveBeenCalled();
  });

  it('shows pause button when timer is running', () => {
    mockUseTimer.mockReturnValue({
      ...mockTimerData,
      isRunning: true,
    });

    render(<VisualTimer />);
    const pauseButton = screen.getByLabelText('Pause timer');
    expect(pauseButton).toBeInTheDocument();
  });

  it('calls handleStop when stop button is clicked', () => {
    render(<VisualTimer />);
    const stopButton = screen.getByLabelText('Stop timer');
    
    fireEvent.click(stopButton);
    expect(mockTimerData.handleStop).toHaveBeenCalled();
  });

  it('disables stop button when timer is not running or paused', () => {
    render(<VisualTimer />);
    const stopButton = screen.getByLabelText('Stop timer');
    expect(stopButton).toBeDisabled();
  });

  it('enables stop button when timer is running', () => {
    mockUseTimer.mockReturnValue({
      ...mockTimerData,
      isRunning: true,
    });

    render(<VisualTimer />);
    const stopButton = screen.getByLabelText('Stop timer');
    expect(stopButton).not.toBeDisabled();
  });

  it('calls handleSkip when skip button is clicked', () => {
    render(<VisualTimer />);
    const skipButton = screen.getByLabelText('Skip to next phase');
    
    fireEvent.click(skipButton);
    expect(mockTimerData.handleSkip).toHaveBeenCalled();
  });

  it('shows settings button when showSettings is true', () => {
    render(<VisualTimer showSettings={true} />);
    const settingsButton = screen.getByLabelText('Timer settings');
    expect(settingsButton).toBeInTheDocument();
  });

  it('hides settings button when showSettings is false', () => {
    render(<VisualTimer showSettings={false} />);
    const settingsButton = screen.queryByLabelText('Timer settings');
    expect(settingsButton).not.toBeInTheDocument();
  });

  it('toggles settings panel when settings button is clicked', () => {
    render(<VisualTimer showSettings={true} compact={false} />);
    const settingsButton = screen.getByLabelText('Timer settings');
    
    // Settings panel should not be visible initially
    expect(screen.queryByText('Work Duration (min)')).not.toBeInTheDocument();
    
    // Click to show settings
    fireEvent.click(settingsButton);
    expect(screen.getByText('Work Duration (min)')).toBeInTheDocument();
    
    // Click again to hide settings
    fireEvent.click(settingsButton);
    expect(screen.queryByText('Work Duration (min)')).not.toBeInTheDocument();
  });

  it('hides settings panel when timer starts', () => {
    mockUseTimer.mockReturnValue({
      ...mockTimerData,
      isRunning: false,
    });

    const { rerender } = render(<VisualTimer showSettings={true} compact={false} />);
    
    // Open settings panel
    const settingsButton = screen.getByLabelText('Timer settings');
    fireEvent.click(settingsButton);
    expect(screen.getByText('Work Duration (min)')).toBeInTheDocument();
    
    // Simulate timer starting
    mockUseTimer.mockReturnValue({
      ...mockTimerData,
      isRunning: true,
    });
    
    rerender(<VisualTimer showSettings={true} compact={false} />);
    expect(screen.queryByText('Work Duration (min)')).not.toBeInTheDocument();
  });

  it('shows "Focus Task Active" when currentTaskId is provided', () => {
    render(<VisualTimer currentTaskId="task-123" compact={false} />);
    expect(screen.getByText('Focus Task Active')).toBeInTheDocument();
  });

  it('applies correct CSS classes based on timer state', () => {
    const { container } = render(<VisualTimer />);
    const timerElement = container.querySelector('.visual-timer');
    
    expect(timerElement).toHaveClass('visual-timer');
    expect(timerElement).not.toHaveClass('timer-running');
    expect(timerElement).not.toHaveClass('timer-paused');
  });

  it('applies timer-running class when timer is running', () => {
    mockUseTimer.mockReturnValue({
      ...mockTimerData,
      isRunning: true,
    });

    const { container } = render(<VisualTimer />);
    const timerElement = container.querySelector('.visual-timer');
    
    expect(timerElement).toHaveClass('timer-running');
  });

  it('applies timer-break class when in break mode', () => {
    mockUseTimer.mockReturnValue({
      ...mockTimerData,
      mode: 'short_break',
    });

    const { container } = render(<VisualTimer />);
    const timerElement = container.querySelector('.visual-timer');
    
    expect(timerElement).toHaveClass('timer-break');
  });

  it('calculates and applies correct stroke-dashoffset for progress', () => {
    mockUseTimer.mockReturnValue({
      ...mockTimerData,
      progress: 50,
    });

    const { container } = render(<VisualTimer />);
    const progressCircle = container.querySelector('.progress-foreground');
    
    // Circumference = 2 * PI * 90 ≈ 565.49
    // 50% progress = strokeDashoffset ≈ 282.74
    expect(progressCircle).toHaveStyle({ strokeDashoffset: expect.stringMatching(/282\.\d+/) });
  });
});