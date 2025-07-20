import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useTaskStore } from '../useTaskStore';
import { useAuthStore } from '../useAuthStore';
import type { Task, TaskFormData, TaskFilters } from '@/types';
import { Priority, TaskStatus, EnergyLevel } from '@/types';

// Mock fetch
const mockFetch = vi.fn();
global.fetch = mockFetch;

// Mock useAuthStore
vi.mock('../useAuthStore', () => ({
  useAuthStore: {
    getState: vi.fn(() => ({ token: 'mock-token' })),
  },
}));

// Mock task data
const mockTask: Task = {
  id: 'task-123',
  userId: 'user-123',
  title: 'Test Task',
  description: 'Test task description',
  status: TaskStatus.PENDING,
  priority: Priority.HIGH,
  energyLevel: EnergyLevel.MEDIUM,
  estimatedMinutes: 30,
  actualMinutes: 25,
  dueDate: '2024-12-31T23:59:59Z',
  scheduledDate: '2024-12-25T09:00:00Z',
  context: 'work',
  tags: ['important', 'urgent'],
  parentTaskId: undefined,
  subtasks: [],
  timeBlocks: [],
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
  completedAt: undefined,
};

const mockTaskFormData: TaskFormData = {
  title: 'New Task',
  description: 'New task description',
  priority: Priority.MEDIUM,
  energyLevel: EnergyLevel.HIGH,
  estimatedMinutes: 45,
  dueDate: '2024-12-31T23:59:59Z',
  scheduledDate: '2024-12-25T09:00:00Z',
  context: 'personal',
  tags: ['test'],
};

describe('useTaskStore', () => {
  beforeEach(() => {
    // Reset store state
    useTaskStore.setState({
      tasks: [],
      filteredTasks: [],
      selectedTask: null,
      filters: {},
      isLoading: false,
      error: null,
      lastUpdated: null,
    });
    
    // Reset mocks
    mockFetch.mockReset();
    vi.mocked(useAuthStore.getState).mockReturnValue({ token: 'mock-token' });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('初始狀態', () => {
    it('應該有正確的初始狀態', () => {
      const { result } = renderHook(() => useTaskStore());
      
      expect(result.current.tasks).toEqual([]);
      expect(result.current.filteredTasks).toEqual([]);
      expect(result.current.selectedTask).toBeNull();
      expect(result.current.filters).toEqual({});
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
      expect(result.current.lastUpdated).toBeNull();
    });
  });

  describe('CRUD 操作 - 創建任務', () => {
    it('應該成功創建任務', async () => {
      const newTask = { ...mockTask, id: 'new-task-123' };
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ data: newTask }),
      });

      const { result } = renderHook(() => useTaskStore());

      await act(async () => {
        const createdTask = await result.current.createTask(mockTaskFormData);
        expect(createdTask).toEqual(newTask);
      });

      expect(result.current.tasks).toContain(newTask);
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
      expect(result.current.lastUpdated).toBeTruthy();
    });

    it('應該處理創建任務失敗', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        text: async () => 'Failed to create task',
        statusText: 'Bad Request'
      });

      const { result } = renderHook(() => useTaskStore());

      await act(async () => {
        try {
          await result.current.createTask(mockTaskFormData);
        } catch (error) {
          expect(error).toBeInstanceOf(Error);
        }
      });

      expect(result.current.tasks).toEqual([]);
      expect(result.current.error).toBe('Failed to create task');
      expect(result.current.isLoading).toBe(false);
    });

    it('應該在創建過程中設置 loading 狀態', async () => {
      let resolvePromise: (value: any) => void;
      const promise = new Promise((resolve) => {
        resolvePromise = resolve;
      });

      mockFetch.mockReturnValueOnce(promise);

      const { result } = renderHook(() => useTaskStore());

      act(() => {
        result.current.createTask(mockTaskFormData);
      });

      expect(result.current.isLoading).toBe(true);

      await act(async () => {
        resolvePromise!({
          ok: true,
          json: async () => mockTask,
        });
        await promise;
      });

      expect(result.current.isLoading).toBe(false);
    });
  });

  describe('CRUD 操作 - 更新任務', () => {
    beforeEach(() => {
      useTaskStore.setState({
        tasks: [mockTask],
        filteredTasks: [mockTask],
      });
    });

    it('應該成功更新任務', async () => {
      const updates = { title: 'Updated Title', priority: Priority.LOW };
      const updatedTask = { ...mockTask, ...updates };
      
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => updatedTask,
      });

      const { result } = renderHook(() => useTaskStore());

      await act(async () => {
        const result_task = await result.current.updateTask(mockTask.id, updates);
        expect(result_task).toEqual(updatedTask);
      });

      expect(result.current.tasks[0]).toEqual(updatedTask);
      expect(result.current.lastUpdated).toBeTruthy();
    });

    it('應該處理更新不存在的任務', async () => {
      const { result } = renderHook(() => useTaskStore());

      await act(async () => {
        try {
          await result.current.updateTask('non-existent', { title: 'New Title' });
        } catch (error) {
          expect(error).toBeInstanceOf(Error);
          expect((error as Error).message).toBe('Task not found');
        }
      });
    });

    it('應該在更新失敗時回退樂觀更新', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        text: async () => 'Failed to update task',
        statusText: 'Bad Request'
      });

      const { result } = renderHook(() => useTaskStore());
      const originalTask = result.current.tasks[0];

      await act(async () => {
        try {
          await result.current.updateTask(mockTask.id, { title: 'Failed Update' });
        } catch (error) {
          // Expected to throw
        }
      });

      // 應該回退到原始狀態
      expect(result.current.tasks[0]).toEqual(originalTask);
      expect(result.current.error).toBe('Failed to update task');
    });
  });

  describe('CRUD 操作 - 刪除任務', () => {
    beforeEach(() => {
      useTaskStore.setState({
        tasks: [mockTask],
        filteredTasks: [mockTask],
      });
    });

    it('應該成功刪除任務', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ data: null }),
      });

      const { result } = renderHook(() => useTaskStore());

      await act(async () => {
        await result.current.deleteTask(mockTask.id);
      });

      expect(result.current.tasks).toEqual([]);
      expect(result.current.lastUpdated).toBeTruthy();
    });

    it('應該處理刪除不存在的任務', async () => {
      const { result } = renderHook(() => useTaskStore());

      await act(async () => {
        try {
          await result.current.deleteTask('non-existent');
        } catch (error) {
          expect(error).toBeInstanceOf(Error);
          expect((error as Error).message).toBe('Task not found');
        }
      });
    });

    it('應該在刪除失敗時回退樂觀刪除', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        text: async () => 'Failed to delete task',
        statusText: 'Bad Request'
      });

      const { result } = renderHook(() => useTaskStore());

      await act(async () => {
        try {
          await result.current.deleteTask(mockTask.id);
        } catch (error) {
          // Expected to throw
        }
      });

      // 應該恢復被刪除的任務
      expect(result.current.tasks).toContain(mockTask);
      expect(result.current.error).toBe('Failed to delete task');
    });
  });

  describe('任務狀態管理', () => {
    beforeEach(() => {
      useTaskStore.setState({
        tasks: [mockTask],
        filteredTasks: [mockTask],
      });
      
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({ data: { ...mockTask, status: TaskStatus.COMPLETED } }),
      });
    });

    it('應該能完成任務', async () => {
      const { result } = renderHook(() => useTaskStore());

      await act(async () => {
        await result.current.completeTask(mockTask.id);
      });

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining(`/api/tasks/${mockTask.id}`),
        expect.objectContaining({
          method: 'PATCH',
          body: expect.stringContaining('"status":"completed"'),
        })
      );
    });

    it('應該能移動任務到進行中', async () => {
      const { result } = renderHook(() => useTaskStore());

      await act(async () => {
        await result.current.moveToInProgress(mockTask.id);
      });

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining(`/api/tasks/${mockTask.id}`),
        expect.objectContaining({
          body: expect.stringContaining('"status":"in_progress"'),
        })
      );
    });

    it('應該能移動任務到 Someday', async () => {
      const { result } = renderHook(() => useTaskStore());

      await act(async () => {
        await result.current.moveToSomeday(mockTask.id);
      });

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining(`/api/tasks/${mockTask.id}`),
        expect.objectContaining({
          body: expect.stringContaining('"status":"someday"'),
        })
      );
    });

    it('應該能封存任務', async () => {
      const { result } = renderHook(() => useTaskStore());

      await act(async () => {
        await result.current.archiveTask(mockTask.id);
      });

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining(`/api/tasks/${mockTask.id}`),
        expect.objectContaining({
          body: expect.stringContaining('"status":"cancelled"'),
        })
      );
    });
  });

  describe('批次操作', () => {
    const tasks = [
      { ...mockTask, id: 'task-1' },
      { ...mockTask, id: 'task-2' },
      { ...mockTask, id: 'task-3' },
    ];

    beforeEach(() => {
      useTaskStore.setState({
        tasks,
        filteredTasks: tasks,
      });
      
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({}),
      });
    });

    it('應該能批次完成任務', async () => {
      const { result } = renderHook(() => useTaskStore());
      const taskIds = ['task-1', 'task-2'];

      await act(async () => {
        await result.current.completeTasks(taskIds);
      });

      expect(mockFetch).toHaveBeenCalledTimes(2);
    });

    it('應該能批次刪除任務', async () => {
      const { result } = renderHook(() => useTaskStore());
      const taskIds = ['task-1', 'task-2'];

      await act(async () => {
        await result.current.deleteTasks(taskIds);
      });

      expect(mockFetch).toHaveBeenCalledTimes(2);
    });
  });

  describe('資料獲取', () => {
    it('應該成功獲取任務', async () => {
      const tasks = [mockTask];
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => tasks,
      });

      const { result } = renderHook(() => useTaskStore());

      await act(async () => {
        await result.current.fetchTasks();
      });

      expect(result.current.tasks).toEqual(tasks);
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
      expect(result.current.lastUpdated).toBeTruthy();
    });

    it('應該處理獲取任務失敗', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
      });

      const { result } = renderHook(() => useTaskStore());

      await act(async () => {
        await result.current.fetchTasks();
      });

      expect(result.current.tasks).toEqual([]);
      expect(result.current.error).toBe('Failed to fetch tasks');
      expect(result.current.isLoading).toBe(false);
    });

    it('應該能刷新任務', async () => {
      const tasks = [mockTask];
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => tasks,
      });

      const { result } = renderHook(() => useTaskStore());

      await act(async () => {
        await result.current.refreshTasks();
      });

      expect(result.current.tasks).toEqual(tasks);
    });
  });

  describe('篩選功能', () => {
    const tasks = [
      { ...mockTask, id: 'task-1', status: TaskStatus.ACTIVE, priority: Priority.HIGH },
      { ...mockTask, id: 'task-2', status: TaskStatus.COMPLETED, priority: Priority.LOW },
      { ...mockTask, id: 'task-3', status: TaskStatus.IN_PROGRESS, priority: Priority.MEDIUM },
    ];

    beforeEach(() => {
      useTaskStore.setState({
        tasks,
        filteredTasks: tasks,
      });
    });

    it('應該能依狀態篩選任務', () => {
      const { result } = renderHook(() => useTaskStore());

      act(() => {
        result.current.setFilters({ status: [TaskStatus.ACTIVE] });
      });

      expect(result.current.filteredTasks).toHaveLength(1);
      expect(result.current.filteredTasks[0].status).toBe(TaskStatus.ACTIVE);
    });

    it('應該能依優先級篩選任務', () => {
      const { result } = renderHook(() => useTaskStore());

      act(() => {
        result.current.setFilters({ priority: [Priority.HIGH, Priority.MEDIUM] });
      });

      expect(result.current.filteredTasks).toHaveLength(2);
    });

    it('應該能依文字搜尋篩選任務', () => {
      const tasksWithDifferentTitles = [
        { ...mockTask, id: 'task-1', title: 'Important meeting' },
        { ...mockTask, id: 'task-2', title: 'Buy groceries' },
        { ...mockTask, id: 'task-3', title: 'Meeting notes' },
      ];

      useTaskStore.setState({
        tasks: tasksWithDifferentTitles,
        filteredTasks: tasksWithDifferentTitles,
      });

      const { result } = renderHook(() => useTaskStore());

      act(() => {
        result.current.setFilters({ search: 'meeting' });
      });

      expect(result.current.filteredTasks).toHaveLength(2);
      expect(result.current.filteredTasks.every(task => 
        task.title.toLowerCase().includes('meeting')
      )).toBe(true);
    });

    it('應該能清除篩選', () => {
      const { result } = renderHook(() => useTaskStore());

      act(() => {
        result.current.setFilters({ status: [TaskStatus.ACTIVE] });
      });

      expect(result.current.filteredTasks).toHaveLength(1);

      act(() => {
        result.current.clearFilters();
      });

      expect(result.current.filteredTasks).toHaveLength(tasks.length);
      expect(result.current.filters).toEqual({});
    });

    it('應該能組合多個篩選條件', () => {
      const { result } = renderHook(() => useTaskStore());

      act(() => {
        result.current.setFilters({
          status: [TaskStatus.ACTIVE, TaskStatus.IN_PROGRESS],
          priority: [Priority.HIGH],
        });
      });

      expect(result.current.filteredTasks).toHaveLength(1);
      expect(result.current.filteredTasks[0].status).toBe(TaskStatus.ACTIVE);
      expect(result.current.filteredTasks[0].priority).toBe(Priority.HIGH);
    });
  });

  describe('任務選擇', () => {
    it('應該能選擇任務', () => {
      const { result } = renderHook(() => useTaskStore());

      act(() => {
        result.current.selectTask(mockTask);
      });

      expect(result.current.selectedTask).toEqual(mockTask);
    });

    it('應該能清除任務選擇', () => {
      useTaskStore.setState({ selectedTask: mockTask });
      const { result } = renderHook(() => useTaskStore());

      act(() => {
        result.current.selectTask(null);
      });

      expect(result.current.selectedTask).toBeNull();
    });
  });

  describe('樂觀更新', () => {
    beforeEach(() => {
      useTaskStore.setState({
        tasks: [mockTask],
        filteredTasks: [mockTask],
      });
    });

    it('應該能執行樂觀更新', () => {
      const { result } = renderHook(() => useTaskStore());
      const updates = { title: 'Updated Title' };

      act(() => {
        result.current.optimisticUpdate(mockTask.id, updates);
      });

      expect(result.current.tasks[0].title).toBe('Updated Title');
    });

    it('應該能回退樂觀更新', () => {
      const { result } = renderHook(() => useTaskStore());
      const originalTask = { ...mockTask };

      act(() => {
        result.current.optimisticUpdate(mockTask.id, { title: 'Updated Title' });
      });

      expect(result.current.tasks[0].title).toBe('Updated Title');

      act(() => {
        result.current.revertOptimisticUpdate(mockTask.id, originalTask);
      });

      expect(result.current.tasks[0]).toEqual(originalTask);
    });
  });

  describe('錯誤處理', () => {
    it('應該能清除錯誤', () => {
      useTaskStore.setState({ error: 'Some error' });
      const { result } = renderHook(() => useTaskStore());

      act(() => {
        result.current.clearError();
      });

      expect(result.current.error).toBeNull();
    });

    it('應該能設置 loading 狀態', () => {
      const { result } = renderHook(() => useTaskStore());

      act(() => {
        result.current.setLoading(true);
      });

      expect(result.current.isLoading).toBe(true);

      act(() => {
        result.current.setLoading(false);
      });

      expect(result.current.isLoading).toBe(false);
    });
  });

  describe('任務查詢方法', () => {
    const tasks = [
      { ...mockTask, id: 'task-1', priority: Priority.HIGH, status: TaskStatus.ACTIVE },
      { ...mockTask, id: 'task-2', priority: Priority.MEDIUM, status: TaskStatus.COMPLETED },
      { ...mockTask, id: 'task-3', priority: Priority.LOW, status: TaskStatus.IN_PROGRESS },
    ];

    beforeEach(() => {
      useTaskStore.setState({ tasks });
    });

    it('應該能依優先級分組任務', () => {
      const { result } = renderHook(() => useTaskStore());

      const tasksByPriority = result.current.getTasksByPriority();

      expect(tasksByPriority[Priority.HIGH]).toHaveLength(1);
      expect(tasksByPriority[Priority.MEDIUM]).toHaveLength(1);
      expect(tasksByPriority[Priority.LOW]).toHaveLength(1);
    });

    it('應該能依狀態分組任務', () => {
      const { result } = renderHook(() => useTaskStore());

      const tasksByStatus = result.current.getTasksByStatus();

      expect(tasksByStatus[TaskStatus.ACTIVE]).toHaveLength(1);
      expect(tasksByStatus[TaskStatus.COMPLETED]).toHaveLength(1);
      expect(tasksByStatus[TaskStatus.IN_PROGRESS]).toHaveLength(1);
    });

    it('應該能獲取今日任務', () => {
      const today = new Date().toISOString();
      const todayTasks = [
        { ...mockTask, id: 'today-1', scheduledDate: today },
        { ...mockTask, id: 'today-2', dueDate: today },
      ];

      useTaskStore.setState({ tasks: todayTasks });
      const { result } = renderHook(() => useTaskStore());

      const tasksForToday = result.current.getTasksForToday();

      expect(tasksForToday).toHaveLength(2);
    });

    it('應該能獲取逾期任務', () => {
      const yesterday = new Date();
      yesterday.setDate(yesterday.getDate() - 1);
      
      const overdueTasks = [
        { ...mockTask, id: 'overdue-1', dueDate: yesterday.toISOString(), status: TaskStatus.ACTIVE },
      ];

      useTaskStore.setState({ tasks: overdueTasks });
      const { result } = renderHook(() => useTaskStore());

      const overdueTasksResult = result.current.getOverdueTasks();

      expect(overdueTasksResult).toHaveLength(1);
    });

    it('應該能獲取即將到期的任務', () => {
      const nextWeek = new Date();
      nextWeek.setDate(nextWeek.getDate() + 3);
      
      const upcomingTasks = [
        { ...mockTask, id: 'upcoming-1', dueDate: nextWeek.toISOString() },
      ];

      useTaskStore.setState({ tasks: upcomingTasks });
      const { result } = renderHook(() => useTaskStore());

      const upcomingTasksResult = result.current.getUpcomingTasks();

      expect(upcomingTasksResult).toHaveLength(1);
    });
  });

  describe('API 請求驗證', () => {
    it('應該在所有請求中包含認證標頭', async () => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => mockTask,
      });

      const { result } = renderHook(() => useTaskStore());

      await act(async () => {
        await result.current.createTask(mockTaskFormData);
      });

      expect(mockFetch).toHaveBeenCalledWith(
        '/api/tasks',
        expect.objectContaining({
          headers: expect.objectContaining({
            'Authorization': 'Bearer mock-token',
          }),
        })
      );
    });

    it('應該發送正確的請求格式', async () => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => mockTask,
      });

      const { result } = renderHook(() => useTaskStore());

      await act(async () => {
        await result.current.createTask(mockTaskFormData);
      });

      expect(mockFetch).toHaveBeenCalledWith('/api/tasks', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer mock-token',
        },
        body: JSON.stringify(mockTaskFormData),
      });
    });
  });
});