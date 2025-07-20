import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useTaskDragDrop } from '../useTaskDragDrop';
import { useTaskStore } from '@/stores/useTaskStore';
import { Priority } from '@/types';

// Mock useTaskStore
vi.mock('@/stores/useTaskStore', () => ({
  useTaskStore: vi.fn(),
}));

// Mock quadrants constants
vi.mock('@/components/features/PriorityMatrix/constants', () => ({
  quadrants: [
    {
      id: 'urgent-important',
      urgent: true,
      important: true,
    },
    {
      id: 'not-urgent-important',
      urgent: false,
      important: true,
    },
    {
      id: 'urgent-not-important',
      urgent: true,
      important: false,
    },
    {
      id: 'not-urgent-not-important',
      urgent: false,
      important: false,
    },
  ],
}));

describe('useTaskDragDrop', () => {
  const mockTasks = [
    {
      id: 'task-1',
      title: 'Test Task 1',
      priority: Priority.MEDIUM,
      dueDate: '2025-12-31T23:59:59Z', // 確實是未來日期
    },
    {
      id: 'task-2',
      title: 'Test Task 2',
      priority: Priority.LOW,
      dueDate: undefined,
    },
    {
      id: 'task-3',
      title: 'Test Task 3',
      priority: Priority.HIGH,
      dueDate: '2024-01-01T00:00:00Z', // 過去的日期
    },
  ];

  const mockUpdateTask = vi.fn();

  beforeEach(() => {
    vi.mocked(useTaskStore).mockReturnValue({
      tasks: mockTasks,
      updateTask: mockUpdateTask,
    });

    // Mock console.error
    vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    vi.clearAllMocks();
    vi.restoreAllMocks();
  });

  describe('基本功能', () => {
    it('應該返回 handleTaskMove 函數', () => {
      const { result } = renderHook(() => useTaskDragDrop());
      
      expect(typeof result.current.handleTaskMove).toBe('function');
    });

    it('應該使用 useCallback 優化性能', () => {
      const { result, rerender } = renderHook(() => useTaskDragDrop());
      
      const initialHandler = result.current.handleTaskMove;
      
      // 重新渲染但 dependencies 沒變
      rerender();
      
      // 函數引用應該保持相同
      expect(result.current.handleTaskMove).toBe(initialHandler);
    });
  });

  describe('任務移動到緊急且重要象限', () => {
    it('應該設置高優先級', async () => {
      const { result } = renderHook(() => useTaskDragDrop());
      
      await act(async () => {
        await result.current.handleTaskMove('task-1', 'urgent-important');
      });
      
      expect(mockUpdateTask).toHaveBeenCalledWith('task-1', {
        priority: Priority.HIGH,
        dueDate: expect.any(String), // 應該更新為今天
      });
    });

    it('應該為沒有截止日期的任務設置今日截止日期', async () => {
      const { result } = renderHook(() => useTaskDragDrop());
      
      const todayStart = new Date();
      todayStart.setHours(0, 0, 0, 0);
      
      await act(async () => {
        await result.current.handleTaskMove('task-2', 'urgent-important');
      });
      
      expect(mockUpdateTask).toHaveBeenCalledWith('task-2', {
        priority: Priority.HIGH,
        dueDate: expect.any(String),
      });
      
      // 檢查設置的日期是今天
      const call = mockUpdateTask.mock.calls[0];
      const updatedDate = new Date(call[1].dueDate);
      const today = new Date();
      
      expect(updatedDate.toDateString()).toBe(today.toDateString());
    });

    it('應該為未來截止日期的任務更新為今日', async () => {
      const { result } = renderHook(() => useTaskDragDrop());
      
      await act(async () => {
        await result.current.handleTaskMove('task-1', 'urgent-important');
      });
      
      expect(mockUpdateTask).toHaveBeenCalledWith('task-1', {
        priority: Priority.HIGH,
        dueDate: expect.any(String),
      });
      
      // 檢查日期被更新為今天
      const call = mockUpdateTask.mock.calls[0];
      const updatedDate = new Date(call[1].dueDate);
      const today = new Date();
      
      // 檢查日期是否被設為今天（允許時區差異）
      const timeDiff = Math.abs(updatedDate.getTime() - today.getTime());
      const daysDiff = timeDiff / (1000 * 60 * 60 * 24);
      expect(daysDiff).toBeLessThan(1); // 應該在同一天內
    });

    it('應該保留過去的截止日期', async () => {
      const { result } = renderHook(() => useTaskDragDrop());
      
      await act(async () => {
        await result.current.handleTaskMove('task-3', 'urgent-important');
      });
      
      expect(mockUpdateTask).toHaveBeenCalledWith('task-3', {
        priority: Priority.HIGH,
        dueDate: '2024-01-01T00:00:00Z', // 保持原來的過去日期
      });
    });
  });

  describe('任務移動到不緊急但重要象限', () => {
    it('應該設置中等優先級', async () => {
      const { result } = renderHook(() => useTaskDragDrop());
      
      await act(async () => {
        await result.current.handleTaskMove('task-1', 'not-urgent-important');
      });
      
      expect(mockUpdateTask).toHaveBeenCalledWith('task-1', {
        priority: Priority.MEDIUM,
        dueDate: '2025-12-31T23:59:59Z', // 保持原有日期
      });
    });
  });

  describe('任務移動到其他象限', () => {
    it('應該設置低優先級 - 緊急但不重要', async () => {
      const { result } = renderHook(() => useTaskDragDrop());
      
      await act(async () => {
        await result.current.handleTaskMove('task-1', 'urgent-not-important');
      });
      
      expect(mockUpdateTask).toHaveBeenCalledWith('task-1', {
        priority: Priority.LOW,
        dueDate: '2025-12-31T23:59:59Z',
      });
    });

    it('應該設置低優先級 - 不緊急且不重要', async () => {
      const { result } = renderHook(() => useTaskDragDrop());
      
      await act(async () => {
        await result.current.handleTaskMove('task-1', 'not-urgent-not-important');
      });
      
      expect(mockUpdateTask).toHaveBeenCalledWith('task-1', {
        priority: Priority.LOW,
        dueDate: '2025-12-31T23:59:59Z',
      });
    });
  });

  describe('錯誤處理', () => {
    it('應該處理不存在的任務 ID', async () => {
      const { result } = renderHook(() => useTaskDragDrop());
      
      await act(async () => {
        await result.current.handleTaskMove('non-existent-task', 'urgent-important');
      });
      
      expect(mockUpdateTask).not.toHaveBeenCalled();
    });

    it('應該處理不存在的象限 ID', async () => {
      const { result } = renderHook(() => useTaskDragDrop());
      
      await act(async () => {
        await result.current.handleTaskMove('task-1', 'non-existent-quadrant');
      });
      
      expect(mockUpdateTask).not.toHaveBeenCalled();
    });

    it('應該處理 updateTask 失敗', async () => {
      const { result } = renderHook(() => useTaskDragDrop());
      
      mockUpdateTask.mockRejectedValueOnce(new Error('Update failed'));
      
      await act(async () => {
        await result.current.handleTaskMove('task-1', 'urgent-important');
      });
      
      expect(console.error).toHaveBeenCalledWith('Failed to update task:', expect.any(Error));
    });
  });

  describe('邊界情況', () => {
    it('應該處理空的任務列表', () => {
      vi.mocked(useTaskStore).mockReturnValue({
        tasks: [],
        updateTask: mockUpdateTask,
      });

      const { result } = renderHook(() => useTaskDragDrop());
      
      act(() => {
        result.current.handleTaskMove('task-1', 'urgent-important');
      });
      
      expect(mockUpdateTask).not.toHaveBeenCalled();
    });

    it('應該處理任務沒有 dueDate 屬性', async () => {
      const taskWithoutDueDate = {
        id: 'task-no-date',
        title: 'Task without due date',
        priority: Priority.MEDIUM,
        // 沒有 dueDate 屬性
      };

      vi.mocked(useTaskStore).mockReturnValue({
        tasks: [taskWithoutDueDate],
        updateTask: mockUpdateTask,
      });

      const { result } = renderHook(() => useTaskDragDrop());
      
      await act(async () => {
        await result.current.handleTaskMove('task-no-date', 'urgent-important');
      });
      
      expect(mockUpdateTask).toHaveBeenCalledWith('task-no-date', {
        priority: Priority.HIGH,
        dueDate: expect.any(String), // 應該設置新的日期
      });
    });

    it('應該處理無效的日期格式', async () => {
      const taskWithInvalidDate = {
        id: 'task-invalid-date',
        title: 'Task with invalid date',
        priority: Priority.MEDIUM,
        dueDate: 'invalid-date-string',
      };

      vi.mocked(useTaskStore).mockReturnValue({
        tasks: [taskWithInvalidDate],
        updateTask: mockUpdateTask,
      });

      const { result } = renderHook(() => useTaskDragDrop());
      
      await act(async () => {
        await result.current.handleTaskMove('task-invalid-date', 'urgent-important');
      });
      
      // 應該更新為今日日期，因為無效日期會被當作需要更新
      expect(mockUpdateTask).toHaveBeenCalledWith('task-invalid-date', {
        priority: Priority.HIGH,
        dueDate: expect.any(String),
      });
    });
  });

  describe('性能測試', () => {
    it('應該高效處理大量任務', async () => {
      const manyTasks = Array.from({ length: 1000 }, (_, i) => ({
        id: `task-${i}`,
        title: `Task ${i}`,
        priority: Priority.MEDIUM,
        dueDate: '2025-12-31T23:59:59Z',
      }));

      vi.mocked(useTaskStore).mockReturnValue({
        tasks: manyTasks,
        updateTask: mockUpdateTask,
      });

      const { result } = renderHook(() => useTaskDragDrop());
      
      const startTime = performance.now();
      
      await act(async () => {
        await result.current.handleTaskMove('task-500', 'urgent-important');
      });
      
      const endTime = performance.now();
      const executionTime = endTime - startTime;
      
      // 執行時間應該少於 10ms
      expect(executionTime).toBeLessThan(10);
      expect(mockUpdateTask).toHaveBeenCalledTimes(1);
    });
  });

  describe('並發處理', () => {
    it('應該正確處理並發的任務移動', async () => {
      const { result } = renderHook(() => useTaskDragDrop());
      
      // 同時移動多個任務
      const promises = [
        result.current.handleTaskMove('task-1', 'urgent-important'),
        result.current.handleTaskMove('task-2', 'not-urgent-important'),
        result.current.handleTaskMove('task-3', 'urgent-not-important'),
      ];
      
      await act(async () => {
        await Promise.all(promises);
      });
      
      expect(mockUpdateTask).toHaveBeenCalledTimes(3);
      
      // 檢查每個調用的參數
      expect(mockUpdateTask).toHaveBeenNthCalledWith(1, 'task-1', {
        priority: Priority.HIGH,
        dueDate: expect.any(String),
      });
      
      expect(mockUpdateTask).toHaveBeenNthCalledWith(2, 'task-2', {
        priority: Priority.MEDIUM,
        dueDate: undefined,
      });
      
      expect(mockUpdateTask).toHaveBeenNthCalledWith(3, 'task-3', {
        priority: Priority.LOW,
        dueDate: '2024-01-01T00:00:00Z',
      });
    });
  });
});