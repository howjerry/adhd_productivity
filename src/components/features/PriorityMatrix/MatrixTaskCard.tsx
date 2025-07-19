import React, { useState } from 'react';
import { Task } from '@/types';
import clsx from 'clsx';
import { Timer, Clock, Zap } from 'lucide-react';

interface MatrixTaskCardProps {
  task: Task;
  compact: boolean;
}

export const MatrixTaskCard: React.FC<MatrixTaskCardProps> = ({ task, compact }) => {
  const [isDragging, setIsDragging] = useState(false);

  const handleDragStart = (e: React.DragEvent) => {
    e.dataTransfer.setData('text/plain', task.id);
    setIsDragging(true);
  };

  const handleDragEnd = () => {
    setIsDragging(false);
  };

  const formatDueDate = (dueDate: string) => {
    const date = new Date(dueDate);
    const now = new Date();
    const diffTime = date.getTime() - now.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Tomorrow';
    if (diffDays === -1) return 'Yesterday';
    if (diffDays < 0) return `${Math.abs(diffDays)} days ago`;
    return `${diffDays} days`;
  };

  const cardClasses = clsx(
    'matrix-task-card',
    {
      'task-dragging': isDragging,
    }
  );

  return (
    <div
      className={cardClasses}
      draggable
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
    >
      <h4 className="task-title">{task.title}</h4>
      
      <div className="task-meta">
        <div className="task-time">
          {task.estimatedMinutes && (
            <>
              <Timer className="w-3 h-3" />
              {task.estimatedMinutes}m
            </>
          )}
          {task.dueDate && (
            <>
              <Clock className="w-3 h-3" />
              {formatDueDate(task.dueDate)}
            </>
          )}
        </div>
        
        {task.energyLevel && (
          <div className="task-energy">
            <Zap className="w-3 h-3" />
            <span className={`energy-${task.energyLevel}`}>
              {task.energyLevel}
            </span>
          </div>
        )}
      </div>

      {task.tags.length > 0 && !compact && (
        <div className="task-tags">
          {task.tags.slice(0, 3).map((tag, index) => (
            <span key={index} className="task-tag">
              {tag}
            </span>
          ))}
          {task.tags.length > 3 && (
            <span className="task-tag">+{task.tags.length - 3}</span>
          )}
        </div>
      )}
    </div>
  );
};