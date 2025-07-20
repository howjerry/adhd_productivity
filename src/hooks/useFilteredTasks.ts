import { useMemo } from 'react';
import { Task, Priority, EnergyLevel, TaskStatus } from '@/types';

interface UseFilteredTasksProps {
  tasks: Task[];
  energyFilter: EnergyLevel | null;
  tagFilter: string | null;
}

interface CategorizedTasks {
  'urgent-important': Task[];
  'not-urgent-important': Task[];
  'urgent-not-important': Task[];
  'not-urgent-not-important': Task[];
}

export const useFilteredTasks = ({ tasks, energyFilter, tagFilter }: UseFilteredTasksProps) => {
  // 獲取所有唯一的標籤
  const availableTags = useMemo(() => {
    const tags = new Set<string>();
    tasks.forEach(task => task.tags.forEach(tag => tags.add(tag)));
    return Array.from(tags).sort();
  }, [tasks]);

  // 分類任務到四個象限
  const categorizedTasks = useMemo(() => {
    const now = new Date();
    const tomorrow = new Date(now);
    tomorrow.setDate(tomorrow.getDate() + 1);

    const categorized: CategorizedTasks = {
      'urgent-important': [],
      'not-urgent-important': [],
      'urgent-not-important': [],
      'not-urgent-not-important': [],
    };

    tasks
      .filter(task => task.status !== TaskStatus.COMPLETED && task.status !== TaskStatus.CANCELLED)
      .filter(task => !energyFilter || task.energyLevel === energyFilter)
      .filter(task => !tagFilter || task.tags.includes(tagFilter))
      .forEach(task => {
        const isUrgent = task.dueDate ? new Date(task.dueDate) <= tomorrow : false;
        const isImportant = task.priority === Priority.HIGH || task.priority === Priority.MEDIUM;

        if (isUrgent && isImportant) {
          categorized['urgent-important'].push(task);
        } else if (!isUrgent && isImportant) {
          categorized['not-urgent-important'].push(task);
        } else if (isUrgent && !isImportant) {
          categorized['urgent-not-important'].push(task);
        } else {
          categorized['not-urgent-not-important'].push(task);
        }
      });

    return categorized;
  }, [tasks, energyFilter, tagFilter]);

  return {
    categorizedTasks,
    availableTags,
  };
};