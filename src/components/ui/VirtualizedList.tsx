import React from 'react';
import { Task } from '@/types';

interface VirtualizedTaskListProps {
  tasks: Task[];
  renderTask: (task: Task, index: number, style: React.CSSProperties) => React.ReactNode;
  itemHeight: number;
  maxHeight: number;
  minHeight?: number;
  className?: string;
  overscanCount?: number;
}

// 簡化版虛擬化列表，真實實作會使用 react-window 或類似函式庫
export const VirtualizedTaskList: React.FC<VirtualizedTaskListProps> = ({
  tasks,
  renderTask,
  itemHeight,
  maxHeight,
  className,
}) => {
  return (
    <div className={className} style={{ maxHeight, overflowY: 'auto' }}>
      {tasks.map((task, index) => 
        renderTask(task, index, { height: itemHeight })
      )}
    </div>
  );
};