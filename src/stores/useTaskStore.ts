import { create } from 'zustand';
import { immer } from 'zustand/middleware/immer';
import { Task, TaskStatus, Priority, TaskFormData, TaskFilters, UUID } from '@/types';
import { useAuthStore } from './useAuthStore';

interface TaskState {
  tasks: Task[];
  filteredTasks: Task[];
  selectedTask: Task | null;
  filters: TaskFilters;
  isLoading: boolean;
  error: string | null;
  lastUpdated: string | null;
}

interface TaskActions {
  // CRUD operations
  createTask: (data: TaskFormData) => Promise<Task>;
  updateTask: (id: UUID, updates: Partial<Task>) => Promise<Task>;
  deleteTask: (id: UUID) => Promise<void>;
  completeTask: (id: UUID) => Promise<Task>;
  
  // Task status management
  moveToInProgress: (id: UUID) => Promise<Task>;
  moveToSomeday: (id: UUID) => Promise<Task>;
  archiveTask: (id: UUID) => Promise<Task>;
  
  // Bulk operations
  completeTasks: (ids: UUID[]) => Promise<void>;
  deleteTasks: (ids: UUID[]) => Promise<void>;
  
  // Data fetching
  fetchTasks: () => Promise<void>;
  refreshTasks: () => Promise<void>;
  
  // Filtering and search
  setFilters: (filters: Partial<TaskFilters>) => void;
  clearFilters: () => void;
  applyFilters: () => void;
  
  // Selection
  selectTask: (task: Task | null) => void;
  
  // Optimistic updates
  optimisticUpdate: (id: UUID, updates: Partial<Task>) => void;
  revertOptimisticUpdate: (id: UUID, originalTask: Task) => void;
  
  // Error handling
  clearError: () => void;
  setLoading: (loading: boolean) => void;
  
  // Priority matrix operations
  getTasksByPriority: () => Record<Priority, Task[]>;
  getTasksByStatus: () => Record<TaskStatus, Task[]>;
  getTasksForToday: () => Task[];
  getOverdueTasks: () => Task[];
  getUpcomingTasks: () => Task[];
}

type TaskStore = TaskState & TaskActions;

const initialState: TaskState = {
  tasks: [],
  filteredTasks: [],
  selectedTask: null,
  filters: {},
  isLoading: false,
  error: null,
  lastUpdated: null,
};

export const useTaskStore = create<TaskStore>()(
  immer((set, get) => ({
    ...initialState,

    createTask: async (data: TaskFormData) => {
      set((state) => {
        state.isLoading = true;
        state.error = null;
      });

      try {
        // TODO: Replace with actual API call
        const response = await fetch('/api/tasks', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${useAuthStore.getState().token}`,
          },
          body: JSON.stringify(data),
        });

        if (!response.ok) {
          throw new Error('Failed to create task');
        }

        const newTask = await response.json();

        set((state) => {
          state.tasks.push(newTask);
          state.isLoading = false;
          state.lastUpdated = new Date().toISOString();
        });

        get().applyFilters();
        return newTask;
      } catch (error) {
        set((state) => {
          state.error = error instanceof Error ? error.message : 'Failed to create task';
          state.isLoading = false;
        });
        throw error;
      }
    },

    updateTask: async (id: UUID, updates: Partial<Task>) => {
      const originalTask = get().tasks.find(t => t.id === id);
      if (!originalTask) {
        throw new Error('Task not found');
      }

      // Optimistic update
      get().optimisticUpdate(id, updates);

      try {
        // TODO: Replace with actual API call
        const response = await fetch(`/api/tasks/${id}`, {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${useAuthStore.getState().token}`,
          },
          body: JSON.stringify(updates),
        });

        if (!response.ok) {
          throw new Error('Failed to update task');
        }

        const updatedTask = await response.json();

        set((state) => {
          const index = state.tasks.findIndex(t => t.id === id);
          if (index !== -1) {
            state.tasks[index] = updatedTask;
          }
          state.lastUpdated = new Date().toISOString();
        });

        get().applyFilters();
        return updatedTask;
      } catch (error) {
        // Revert optimistic update
        get().revertOptimisticUpdate(id, originalTask);
        set((state) => {
          state.error = error instanceof Error ? error.message : 'Failed to update task';
        });
        throw error;
      }
    },

    deleteTask: async (id: UUID) => {
      const originalTask = get().tasks.find(t => t.id === id);
      if (!originalTask) {
        throw new Error('Task not found');
      }

      // Optimistic delete
      set((state) => {
        state.tasks = state.tasks.filter(t => t.id !== id);
      });

      try {
        // TODO: Replace with actual API call
        const response = await fetch(`/api/tasks/${id}`, {
          method: 'DELETE',
          headers: {
            'Authorization': `Bearer ${useAuthStore.getState().token}`,
          },
        });

        if (!response.ok) {
          throw new Error('Failed to delete task');
        }

        set((state) => {
          state.lastUpdated = new Date().toISOString();
        });

        get().applyFilters();
      } catch (error) {
        // Revert optimistic delete
        set((state) => {
          state.tasks.push(originalTask);
          state.error = error instanceof Error ? error.message : 'Failed to delete task';
        });
        throw error;
      }
    },

    completeTask: async (id: UUID) => {
      return get().updateTask(id, {
        status: TaskStatus.COMPLETED,
        completedAt: new Date().toISOString(),
      });
    },

    moveToInProgress: async (id: UUID) => {
      return get().updateTask(id, {
        status: TaskStatus.IN_PROGRESS,
      });
    },

    moveToSomeday: async (id: UUID) => {
      return get().updateTask(id, {
        status: TaskStatus.SOMEDAY,
      });
    },

    archiveTask: async (id: UUID) => {
      return get().updateTask(id, {
        status: TaskStatus.CANCELLED,
      });
    },

    completeTasks: async (ids: UUID[]) => {
      await Promise.all(ids.map(id => get().completeTask(id)));
    },

    deleteTasks: async (ids: UUID[]) => {
      await Promise.all(ids.map(id => get().deleteTask(id)));
    },

    fetchTasks: async () => {
      set((state) => {
        state.isLoading = true;
        state.error = null;
      });

      try {
        // TODO: Replace with actual API call
        const response = await fetch('/api/tasks', {
          headers: {
            'Authorization': `Bearer ${useAuthStore.getState().token}`,
          },
        });

        if (!response.ok) {
          throw new Error('Failed to fetch tasks');
        }

        const tasks = await response.json();

        set((state) => {
          state.tasks = tasks;
          state.isLoading = false;
          state.lastUpdated = new Date().toISOString();
        });

        get().applyFilters();
      } catch (error) {
        set((state) => {
          state.error = error instanceof Error ? error.message : 'Failed to fetch tasks';
          state.isLoading = false;
        });
      }
    },

    refreshTasks: async () => {
      await get().fetchTasks();
    },

    setFilters: (filters: Partial<TaskFilters>) => {
      set((state) => {
        state.filters = { ...state.filters, ...filters };
      });
      get().applyFilters();
    },

    clearFilters: () => {
      set((state) => {
        state.filters = {};
      });
      get().applyFilters();
    },

    applyFilters: () => {
      const { tasks, filters } = get();
      let filtered = [...tasks];

      if (filters.status?.length) {
        filtered = filtered.filter(task => filters.status!.includes(task.status));
      }

      if (filters.priority?.length) {
        filtered = filtered.filter(task => filters.priority!.includes(task.priority));
      }

      if (filters.energyLevel?.length) {
        filtered = filtered.filter(task => 
          task.energyLevel && filters.energyLevel!.includes(task.energyLevel)
        );
      }

      if (filters.tags?.length) {
        filtered = filtered.filter(task =>
          filters.tags!.some(tag => task.tags.includes(tag))
        );
      }

      if (filters.context) {
        filtered = filtered.filter(task =>
          task.context?.toLowerCase().includes(filters.context!.toLowerCase())
        );
      }

      if (filters.search) {
        const searchTerm = filters.search.toLowerCase();
        filtered = filtered.filter(task =>
          task.title.toLowerCase().includes(searchTerm) ||
          task.description?.toLowerCase().includes(searchTerm)
        );
      }

      if (filters.dueDate?.start || filters.dueDate?.end) {
        filtered = filtered.filter(task => {
          if (!task.dueDate) return false;
          const dueDate = new Date(task.dueDate);
          
          if (filters.dueDate?.start) {
            const startDate = new Date(filters.dueDate.start);
            if (dueDate < startDate) return false;
          }
          
          if (filters.dueDate?.end) {
            const endDate = new Date(filters.dueDate.end);
            if (dueDate > endDate) return false;
          }
          
          return true;
        });
      }

      set((state) => {
        state.filteredTasks = filtered;
      });
    },

    selectTask: (task: Task | null) => {
      set((state) => {
        state.selectedTask = task;
      });
    },

    optimisticUpdate: (id: UUID, updates: Partial<Task>) => {
      set((state) => {
        const index = state.tasks.findIndex(t => t.id === id);
        if (index !== -1) {
          state.tasks[index] = { ...state.tasks[index], ...updates };
        }
      });
      get().applyFilters();
    },

    revertOptimisticUpdate: (id: UUID, originalTask: Task) => {
      set((state) => {
        const index = state.tasks.findIndex(t => t.id === id);
        if (index !== -1) {
          state.tasks[index] = originalTask;
        }
      });
      get().applyFilters();
    },

    clearError: () => {
      set((state) => {
        state.error = null;
      });
    },

    setLoading: (loading: boolean) => {
      set((state) => {
        state.isLoading = loading;
      });
    },

    getTasksByPriority: () => {
      const { tasks } = get();
      return {
        [Priority.HIGH]: tasks.filter(t => t.priority === Priority.HIGH),
        [Priority.MEDIUM]: tasks.filter(t => t.priority === Priority.MEDIUM),
        [Priority.LOW]: tasks.filter(t => t.priority === Priority.LOW),
      };
    },

    getTasksByStatus: () => {
      const { tasks } = get();
      return Object.values(TaskStatus).reduce((acc, status) => {
        acc[status] = tasks.filter(t => t.status === status);
        return acc;
      }, {} as Record<TaskStatus, Task[]>);
    },

    getTasksForToday: () => {
      const { tasks } = get();
      const today = new Date().toDateString();
      return tasks.filter(task => {
        if (task.scheduledDate) {
          return new Date(task.scheduledDate).toDateString() === today;
        }
        if (task.dueDate) {
          return new Date(task.dueDate).toDateString() === today;
        }
        return false;
      });
    },

    getOverdueTasks: () => {
      const { tasks } = get();
      const now = new Date();
      return tasks.filter(task => 
        task.dueDate && 
        new Date(task.dueDate) < now && 
        task.status !== TaskStatus.COMPLETED
      );
    },

    getUpcomingTasks: () => {
      const { tasks } = get();
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      const nextWeek = new Date();
      nextWeek.setDate(nextWeek.getDate() + 7);
      
      return tasks.filter(task => {
        if (!task.dueDate) return false;
        const dueDate = new Date(task.dueDate);
        return dueDate >= tomorrow && dueDate <= nextWeek;
      });
    },
  }))
);