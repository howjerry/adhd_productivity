import { useCallback } from 'react';
import { Task, Priority } from '@/types';
import { useTaskStore } from '@/stores/useTaskStore';
import { quadrants } from '@/components/features/PriorityMatrix/constants';

export const useTaskDragDrop = () => {
  const { tasks, updateTask } = useTaskStore();

  const handleTaskMove = useCallback(async (taskId: string, newQuadrant: string) => {
    const task = tasks.find(t => t.id === taskId);
    if (!task) return;

    const quadrant = quadrants.find(q => q.id === newQuadrant);
    if (!quadrant) return;

    // 根據象限決定新的優先級
    let newPriority: Priority;
    let newDueDate: string | undefined = task.dueDate;

    if (quadrant.urgent && quadrant.important) {
      newPriority = Priority.HIGH;
      // 如果還不緊急，設定截止日期為今天
      if (!task.dueDate || new Date(task.dueDate) > new Date()) {
        newDueDate = new Date().toISOString();
      }
    } else if (!quadrant.urgent && quadrant.important) {
      newPriority = Priority.MEDIUM;
    } else {
      newPriority = Priority.LOW;
    }

    try {
      await updateTask(taskId, {
        priority: newPriority,
        dueDate: newDueDate,
      });
    } catch (error) {
      console.error('Failed to update task:', error);
    }
  }, [tasks, updateTask]);

  return {
    handleTaskMove,
  };
};