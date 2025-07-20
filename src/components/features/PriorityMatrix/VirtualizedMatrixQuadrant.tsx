import React, { useState, useCallback, useMemo } from 'react';
import { Task } from '@/types';
import clsx from 'clsx';
import { Plus, Filter } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { MatrixTaskCard } from './MatrixTaskCard';
import { VirtualizedTaskList } from '@/components/ui/VirtualizedList';
import { MatrixQuadrant } from './MatrixQuadrant';

interface VirtualizedMatrixQuadrantProps {
  quadrant: MatrixQuadrant;
  tasks: Task[];
  compact: boolean;
  onTaskMove: (taskId: string, quadrantId: string) => void;
  isSelected: boolean;
  onSelect: () => void;
  enableVirtualization?: boolean;
  maxHeight?: number;
}

export const VirtualizedMatrixQuadrant: React.FC<VirtualizedMatrixQuadrantProps> = ({
  quadrant,
  tasks,
  compact,
  onTaskMove,
  isSelected,
  onSelect,
  enableVirtualization = true,
  maxHeight = 400,
}) => {
  const [dragOver, setDragOver] = useState(false);

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(true);
  }, []);

  const handleDragLeave = useCallback(() => {
    setDragOver(false);
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(false);
    
    const taskId = e.dataTransfer.getData('text/plain');
    if (taskId) {
      onTaskMove(taskId, quadrant.id);
    }
  }, [onTaskMove, quadrant.id]);

  // 任務渲染器，針對虛擬滾動優化
  const renderTask = useCallback((task: Task, _index: number, style: React.CSSProperties) => (
    <div key={task.id} style={style}>
      <MatrixTaskCard
        task={task}
        compact={compact}
      />
    </div>
  ), [compact]);

  // 決定是否使用虛擬滾動（基於任務數量）
  const shouldUseVirtualization = useMemo(() => {
    return enableVirtualization && tasks.length > 10;
  }, [enableVirtualization, tasks.length]);

  const quadrantClasses = clsx(
    'matrix-quadrant',
    `quadrant-${quadrant.id}`,
    {
      'drag-over': dragOver,
      'selected': isSelected,
      'virtualized': shouldUseVirtualization,
    }
  );

  const taskItemHeight = compact ? 60 : 80;

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
        <div className="quadrant-count">
          {tasks.length}
          {shouldUseVirtualization && (
            <span className="ml-1 text-xs text-gray-500">(虛擬)</span>
          )}
        </div>
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
        ) : shouldUseVirtualization ? (
          <VirtualizedTaskList
            tasks={tasks}
            renderTask={renderTask}
            itemHeight={taskItemHeight}
            maxHeight={maxHeight}
            minHeight={120}
            className="quadrant-virtualized-tasks"
            overscanCount={3}
          />
        ) : (
          <div className="quadrant-standard-tasks">
            {tasks.map(task => (
              <MatrixTaskCard
                key={task.id}
                task={task}
                compact={compact}
              />
            ))}
          </div>
        )}
      </div>

      {/* 效能指標 (僅在開發模式顯示) */}
      {process.env.NODE_ENV === 'development' && tasks.length > 50 && (
        <div className="quadrant-perf-indicator">
          <span className="text-xs text-gray-400">
            {tasks.length} tasks • {shouldUseVirtualization ? 'Virtualized' : 'Standard'}
          </span>
        </div>
      )}
    </div>
  );
};

export default VirtualizedMatrixQuadrant;