import React, { useState } from 'react';
import { Task } from '@/types';
import clsx from 'clsx';
import { Plus, Filter } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { MatrixTaskCard } from './MatrixTaskCard';

export interface MatrixQuadrant {
  id: string;
  title: string;
  description: string;
  icon: React.ReactNode;
  urgent: boolean;
  important: boolean;
  color: string;
}

interface MatrixQuadrantProps {
  quadrant: MatrixQuadrant;
  tasks: Task[];
  compact: boolean;
  onTaskMove: (taskId: string, quadrantId: string) => void;
  isSelected: boolean;
  onSelect: () => void;
}

export const MatrixQuadrant: React.FC<MatrixQuadrantProps> = ({
  quadrant,
  tasks,
  compact,
  onTaskMove,
  isSelected,
  onSelect,
}) => {
  const [dragOver, setDragOver] = useState(false);

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(true);
  };

  const handleDragLeave = () => {
    setDragOver(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(false);
    
    const taskId = e.dataTransfer.getData('text/plain');
    if (taskId) {
      onTaskMove(taskId, quadrant.id);
    }
  };

  const quadrantClasses = clsx(
    'matrix-quadrant',
    `quadrant-${quadrant.id}`,
    {
      'drag-over': dragOver,
      'selected': isSelected,
    }
  );

  return (
    <div
      className={quadrantClasses}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
      onClick={onSelect}
    >
      <div className="quadrant-header">
        <h3 className="quadrant-title">
          {quadrant.icon}
          {quadrant.title}
        </h3>
        <div className="quadrant-count">{tasks.length}</div>
        <div className="quadrant-actions">
          <Button variant="ghost" size="sm" icon={<Plus className="w-3 h-3" />} />
          <Button variant="ghost" size="sm" icon={<Filter className="w-3 h-3" />} />
        </div>
      </div>

      <div className="quadrant-tasks">
        {tasks.length === 0 ? (
          <div className="quadrant-empty">
            <div className="empty-icon">{quadrant.icon}</div>
            <div className="empty-text">No tasks in this quadrant</div>
            <div className="empty-hint">Drag tasks here to categorize</div>
          </div>
        ) : (
          tasks.map(task => (
            <MatrixTaskCard
              key={task.id}
              task={task}
              compact={compact}
            />
          ))
        )}
      </div>
    </div>
  );
};