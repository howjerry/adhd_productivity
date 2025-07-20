import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { vi } from 'vitest';
import { MatrixTaskCard } from '../MatrixTaskCard';
import { Task, Priority, TaskStatus, EnergyLevel } from '@/types';

describe('MatrixTaskCard', () => {
  const mockTask: Task = {
    id: '1',
    title: 'Test Task',
    description: 'Test task description',
    priority: Priority.HIGH,
    status: TaskStatus.PENDING,
    dueDate: new Date().toISOString(),
    estimatedMinutes: 30,
    energyLevel: EnergyLevel.HIGH,
    tags: ['work', 'urgent', 'project'],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };

  it('renders task title', () => {
    render(<MatrixTaskCard task={mockTask} compact={false} />);
    expect(screen.getByText('Test Task')).toBeInTheDocument();
  });

  it('displays estimated time when present', () => {
    render(<MatrixTaskCard task={mockTask} compact={false} />);
    expect(screen.getByText('30', { exact: false })).toBeInTheDocument();
    expect(screen.getByText('m', { exact: false })).toBeInTheDocument();
  });

  it('displays due date as "Today" for current date', () => {
    const { container } = render(<MatrixTaskCard task={mockTask} compact={false} />);
    expect(container.textContent).toContain('Today');
  });

  it('displays energy level when present', () => {
    render(<MatrixTaskCard task={mockTask} compact={false} />);
    expect(screen.getByText('high')).toBeInTheDocument();
    expect(screen.getByText('high').className).toContain('energy-high');
  });

  it('shows first 3 tags in normal mode', () => {
    render(<MatrixTaskCard task={mockTask} compact={false} />);
    expect(screen.getByText('work')).toBeInTheDocument();
    expect(screen.getByText('urgent')).toBeInTheDocument();
    expect(screen.getByText('project')).toBeInTheDocument();
  });

  it('shows tag overflow count when more than 3 tags', () => {
    const taskWithManyTags = {
      ...mockTask,
      tags: ['tag1', 'tag2', 'tag3', 'tag4', 'tag5'],
    };
    render(<MatrixTaskCard task={taskWithManyTags} compact={false} />);
    expect(screen.getByText('+2')).toBeInTheDocument();
  });

  it('hides tags in compact mode', () => {
    render(<MatrixTaskCard task={mockTask} compact={true} />);
    expect(screen.queryByText('work')).not.toBeInTheDocument();
    expect(screen.queryByText('urgent')).not.toBeInTheDocument();
  });

  it('handles drag start and end events', () => {
    const { container } = render(<MatrixTaskCard task={mockTask} compact={false} />);
    const card = container.querySelector('.matrix-task-card');

    // Mock dataTransfer
    const dataTransfer = {
      setData: vi.fn(),
    };

    // Fire drag start
    fireEvent.dragStart(card!, { dataTransfer });
    expect(dataTransfer.setData).toHaveBeenCalledWith('text/plain', '1');
    expect(card).toHaveClass('task-dragging');

    // Fire drag end
    fireEvent.dragEnd(card!);
    expect(card).not.toHaveClass('task-dragging');
  });

  it('formats future due dates correctly', () => {
    const futureTask = {
      ...mockTask,
      dueDate: new Date(Date.now() + 2 * 24 * 60 * 60 * 1000).toISOString(), // 2 days from now
    };
    const { container } = render(<MatrixTaskCard task={futureTask} compact={false} />);
    expect(container.textContent).toContain('2 days');
  });

  it('formats past due dates correctly', () => {
    const pastTask = {
      ...mockTask,
      dueDate: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000).toISOString(), // 3 days ago
    };
    const { container } = render(<MatrixTaskCard task={pastTask} compact={false} />);
    expect(container.textContent).toContain('3 days ago');
  });

  it('does not display time info when estimatedMinutes is not present', () => {
    const taskWithoutTime = {
      ...mockTask,
      estimatedMinutes: undefined,
    };
    render(<MatrixTaskCard task={taskWithoutTime} compact={false} />);
    // Check that Timer icon is not present when no estimated time
    expect(screen.queryByText('30')).not.toBeInTheDocument();
  });
});