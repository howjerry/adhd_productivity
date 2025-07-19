import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { vi } from 'vitest';
import { PriorityMatrix } from '../index';
import { useTaskStore } from '@/stores/useTaskStore';
import { Task, Priority, TaskStatus, EnergyLevel } from '@/types';

// Mock the task store
vi.mock('@/stores/useTaskStore');

const mockUseTaskStore = useTaskStore as unknown as ReturnType<typeof vi.fn>;

describe('PriorityMatrix', () => {
  const mockTasks: Task[] = [
    {
      id: '1',
      title: 'Urgent Important Task',
      description: 'Test task 1',
      priority: Priority.HIGH,
      status: TaskStatus.PENDING,
      dueDate: new Date().toISOString(),
      energyLevel: EnergyLevel.HIGH,
      tags: ['work'],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
    {
      id: '2',
      title: 'Not Urgent Important Task',
      description: 'Test task 2',
      priority: Priority.MEDIUM,
      status: TaskStatus.PENDING,
      dueDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(), // 7 days from now
      energyLevel: EnergyLevel.MEDIUM,
      tags: ['personal'],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
    {
      id: '3',
      title: 'Low Priority Task',
      description: 'Test task 3',
      priority: Priority.LOW,
      status: TaskStatus.PENDING,
      energyLevel: EnergyLevel.LOW,
      tags: ['hobby'],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
  ];

  const mockUpdateTask = vi.fn();

  beforeEach(() => {
    mockUseTaskStore.mockReturnValue({
      tasks: mockTasks,
      updateTask: mockUpdateTask,
      // Add other required store methods if needed
    } as any);
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders the priority matrix with all quadrants', () => {
    render(<PriorityMatrix />);

    expect(screen.getByText('Priority Matrix')).toBeInTheDocument();
    expect(screen.getByText('Do First')).toBeInTheDocument();
    expect(screen.getByText('Schedule')).toBeInTheDocument();
    expect(screen.getByText('Delegate')).toBeInTheDocument();
    expect(screen.getByText('Eliminate')).toBeInTheDocument();
  });

  it('displays tasks in correct quadrants based on priority and due date', () => {
    render(<PriorityMatrix />);

    // 緊急重要任務應該在 "Do First" 象限
    const doFirstQuadrant = screen.getByText('Do First').closest('.matrix-quadrant');
    expect(doFirstQuadrant).toHaveTextContent('Urgent Important Task');

    // 不緊急重要任務應該在 "Schedule" 象限
    const scheduleQuadrant = screen.getByText('Schedule').closest('.matrix-quadrant');
    expect(scheduleQuadrant).toHaveTextContent('Not Urgent Important Task');
  });

  it('shows filters when showFilters prop is true', () => {
    render(<PriorityMatrix showFilters={true} />);

    expect(screen.getByText('Filters:')).toBeInTheDocument();
    expect(screen.getByText('All Energy')).toBeInTheDocument();
    expect(screen.getByText('All Tags')).toBeInTheDocument();
  });

  it('hides filters when showFilters prop is false', () => {
    render(<PriorityMatrix showFilters={false} />);

    expect(screen.queryByText('Filters:')).not.toBeInTheDocument();
  });

  it('applies energy filter when energy level button is clicked', () => {
    render(<PriorityMatrix showFilters={true} />);

    const highEnergyButton = screen.getByText('HIGH');
    fireEvent.click(highEnergyButton);

    // 只有高能量任務應該顯示
    expect(screen.getByText('Urgent Important Task')).toBeInTheDocument();
    expect(screen.queryByText('Not Urgent Important Task')).not.toBeInTheDocument();
    expect(screen.queryByText('Low Priority Task')).not.toBeInTheDocument();
  });

  it('applies tag filter when tag button is clicked', () => {
    render(<PriorityMatrix showFilters={true} />);

    const workTagButton = screen.getByText('#work');
    fireEvent.click(workTagButton);

    // 只有有 'work' 標籤的任務應該顯示
    expect(screen.getByText('Urgent Important Task')).toBeInTheDocument();
    expect(screen.queryByText('Not Urgent Important Task')).not.toBeInTheDocument();
    expect(screen.queryByText('Low Priority Task')).not.toBeInTheDocument();
  });

  it('applies compact style when compact prop is true', () => {
    const { container } = render(<PriorityMatrix compact={true} />);

    expect(container.querySelector('.priority-matrix-compact')).toBeInTheDocument();
  });

  it('shows legend with all quadrant descriptions', () => {
    render(<PriorityMatrix />);

    expect(screen.getByText('Urgent & Important')).toBeInTheDocument();
    expect(screen.getByText('Not Urgent & Important')).toBeInTheDocument();
    expect(screen.getByText('Urgent & Not Important')).toBeInTheDocument();
    expect(screen.getByText('Not Urgent & Not Important')).toBeInTheDocument();
  });
});