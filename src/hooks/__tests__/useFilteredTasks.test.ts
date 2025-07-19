import { renderHook } from '@testing-library/react';
import { useFilteredTasks } from '../useFilteredTasks';
import { Task, Priority, TaskStatus, EnergyLevel } from '@/types';

describe('useFilteredTasks', () => {
  const mockTasks: Task[] = [
    {
      id: '1',
      title: 'High Priority Task',
      description: 'Test task 1',
      priority: Priority.HIGH,
      status: TaskStatus.PENDING,
      dueDate: new Date().toISOString(), // Today - urgent
      energyLevel: EnergyLevel.HIGH,
      tags: ['work', 'project'],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
    {
      id: '2',
      title: 'Medium Priority Task',
      description: 'Test task 2',
      priority: Priority.MEDIUM,
      status: TaskStatus.PENDING,
      dueDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(), // 7 days - not urgent
      energyLevel: EnergyLevel.MEDIUM,
      tags: ['personal'],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
    {
      id: '3',
      title: 'Low Priority Urgent Task',
      description: 'Test task 3',
      priority: Priority.LOW,
      status: TaskStatus.PENDING,
      dueDate: new Date().toISOString(), // Today - urgent
      energyLevel: EnergyLevel.LOW,
      tags: ['hobby'],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
    {
      id: '4',
      title: 'Completed Task',
      description: 'Test task 4',
      priority: Priority.HIGH,
      status: TaskStatus.COMPLETED,
      dueDate: new Date().toISOString(),
      energyLevel: EnergyLevel.HIGH,
      tags: ['work'],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    },
  ];

  it('filters out completed and cancelled tasks', () => {
    const { result } = renderHook(() => useFilteredTasks({
      tasks: mockTasks,
      energyFilter: null,
      tagFilter: null,
    }));

    const allCategorizedTasks = [
      ...result.current.categorizedTasks['urgent-important'],
      ...result.current.categorizedTasks['not-urgent-important'],
      ...result.current.categorizedTasks['urgent-not-important'],
      ...result.current.categorizedTasks['not-urgent-not-important'],
    ];

    expect(allCategorizedTasks).toHaveLength(3); // Only 3 pending tasks
    expect(allCategorizedTasks.find(t => t.id === '4')).toBeUndefined();
  });

  it('categorizes tasks correctly based on urgency and importance', () => {
    const { result } = renderHook(() => useFilteredTasks({
      tasks: mockTasks,
      energyFilter: null,
      tagFilter: null,
    }));

    // Task 1: High priority (important) + Today (urgent) = urgent-important
    expect(result.current.categorizedTasks['urgent-important']).toContainEqual(
      expect.objectContaining({ id: '1' })
    );

    // Task 2: Medium priority (important) + 7 days (not urgent) = not-urgent-important
    expect(result.current.categorizedTasks['not-urgent-important']).toContainEqual(
      expect.objectContaining({ id: '2' })
    );

    // Task 3: Low priority (not important) + Today (urgent) = urgent-not-important
    expect(result.current.categorizedTasks['urgent-not-important']).toContainEqual(
      expect.objectContaining({ id: '3' })
    );
  });

  it('filters tasks by energy level', () => {
    const { result } = renderHook(() => useFilteredTasks({
      tasks: mockTasks,
      energyFilter: EnergyLevel.HIGH,
      tagFilter: null,
    }));

    const allCategorizedTasks = [
      ...result.current.categorizedTasks['urgent-important'],
      ...result.current.categorizedTasks['not-urgent-important'],
      ...result.current.categorizedTasks['urgent-not-important'],
      ...result.current.categorizedTasks['not-urgent-not-important'],
    ];

    expect(allCategorizedTasks).toHaveLength(1);
    expect(allCategorizedTasks[0].id).toBe('1');
  });

  it('filters tasks by tag', () => {
    const { result } = renderHook(() => useFilteredTasks({
      tasks: mockTasks,
      energyFilter: null,
      tagFilter: 'work',
    }));

    const allCategorizedTasks = [
      ...result.current.categorizedTasks['urgent-important'],
      ...result.current.categorizedTasks['not-urgent-important'],
      ...result.current.categorizedTasks['urgent-not-important'],
      ...result.current.categorizedTasks['not-urgent-not-important'],
    ];

    expect(allCategorizedTasks).toHaveLength(1);
    expect(allCategorizedTasks[0].id).toBe('1');
  });

  it('extracts and sorts available tags', () => {
    const { result } = renderHook(() => useFilteredTasks({
      tasks: mockTasks,
      energyFilter: null,
      tagFilter: null,
    }));

    expect(result.current.availableTags).toEqual(['hobby', 'personal', 'project', 'work']);
  });

  it('applies both energy and tag filters together', () => {
    const tasksWithOverlap: Task[] = [
      ...mockTasks,
      {
        id: '5',
        title: 'High Energy Personal Task',
        description: 'Test task 5',
        priority: Priority.MEDIUM,
        status: TaskStatus.PENDING,
        dueDate: new Date().toISOString(),
        energyLevel: EnergyLevel.HIGH,
        tags: ['personal'],
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      },
    ];

    const { result } = renderHook(() => useFilteredTasks({
      tasks: tasksWithOverlap,
      energyFilter: EnergyLevel.HIGH,
      tagFilter: 'personal',
    }));

    const allCategorizedTasks = [
      ...result.current.categorizedTasks['urgent-important'],
      ...result.current.categorizedTasks['not-urgent-important'],
      ...result.current.categorizedTasks['urgent-not-important'],
      ...result.current.categorizedTasks['not-urgent-not-important'],
    ];

    expect(allCategorizedTasks).toHaveLength(1);
    expect(allCategorizedTasks[0].id).toBe('5');
  });

  it('considers tasks due tomorrow as urgent', () => {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);

    const taskDueTomorrow: Task = {
      id: '6',
      title: 'Tomorrow Task',
      description: 'Due tomorrow',
      priority: Priority.HIGH,
      status: TaskStatus.PENDING,
      dueDate: tomorrow.toISOString(),
      energyLevel: EnergyLevel.MEDIUM,
      tags: ['test'],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };

    const { result } = renderHook(() => useFilteredTasks({
      tasks: [taskDueTomorrow],
      energyFilter: null,
      tagFilter: null,
    }));

    expect(result.current.categorizedTasks['urgent-important']).toContainEqual(
      expect.objectContaining({ id: '6' })
    );
  });
});