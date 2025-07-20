import React, { useState, useMemo } from 'react';
import { EnergyLevel } from '@/types';
import clsx from 'clsx';
import { Plus, MoreHorizontal, Zap } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { MatrixQuadrant } from './MatrixQuadrant';
import { VirtualizedMatrixQuadrant } from './VirtualizedMatrixQuadrant';
import { MatrixFilters } from './MatrixFilters';
import { useFilteredTasks } from '@/hooks/useFilteredTasks';
import { useTaskDragDrop } from '@/hooks/useTaskDragDrop';
import { useTaskStore } from '@/stores/useTaskStore';
import { quadrants } from './constants';

interface PriorityMatrixProps {
  compact?: boolean;
  showFilters?: boolean;
  className?: string;
  enableVirtualization?: boolean;
  performanceMode?: boolean;
}

export const PriorityMatrix: React.FC<PriorityMatrixProps> = ({
  compact = false,
  showFilters = true,
  className,
  enableVirtualization = true,
  performanceMode = false,
}) => {
  const { tasks } = useTaskStore();
  const [selectedQuadrant, setSelectedQuadrant] = useState<string | null>(null);
  const [energyFilter, setEnergyFilter] = useState<EnergyLevel | null>(null);
  const [tagFilter, setTagFilter] = useState<string | null>(null);
  const [virtualizationEnabled, setVirtualizationEnabled] = useState(enableVirtualization);

  const { categorizedTasks, availableTags } = useFilteredTasks({
    tasks,
    energyFilter,
    tagFilter,
  });

  const { handleTaskMove } = useTaskDragDrop();

  // 自動效能檢測
  const totalTasks = useMemo(() => {
    return Object.values(categorizedTasks).reduce((sum, taskArray) => sum + taskArray.length, 0);
  }, [categorizedTasks]);

  // 決定是否需要虛擬化
  const shouldUseVirtualization = useMemo(() => {
    if (performanceMode) return true;
    return virtualizationEnabled && (totalTasks > 50 || 
      Object.values(categorizedTasks).some(taskArray => taskArray.length > 20));
  }, [virtualizationEnabled, totalTasks, categorizedTasks, performanceMode]);

  // 切換虛擬化模式
  const toggleVirtualization = () => {
    setVirtualizationEnabled(!virtualizationEnabled);
  };

  const matrixClasses = clsx(
    'priority-matrix',
    {
      'priority-matrix-compact': compact,
    },
    className
  );

  return (
    <div className="priority-matrix-container">
      <div className="matrix-controls">
        <h2 className="matrix-title">
          Priority Matrix
          {shouldUseVirtualization && (
            <span className="ml-2 px-2 py-1 text-xs bg-green-100 text-green-700 rounded-full">
              <Zap className="w-3 h-3 inline mr-1" />
              Optimized
            </span>
          )}
        </h2>
        <div className="matrix-actions space-x-2">
          <div className="text-xs text-gray-500 px-2 py-1">
            {totalTasks} tasks total
          </div>
          {totalTasks > 20 && (
            <Button 
              variant={shouldUseVirtualization ? "primary" : "outline"} 
              size="sm" 
              icon={<Zap className="w-4 h-4" />}
              onClick={toggleVirtualization}
              title="切換虛擬滾動以提升效能"
            >
              {shouldUseVirtualization ? 'Virtual' : 'Standard'}
            </Button>
          )}
          <Button variant="outline" size="sm" icon={<Plus className="w-4 h-4" />}>
            Add Task
          </Button>
          <Button variant="ghost" size="sm" icon={<MoreHorizontal className="w-4 h-4" />}>
            Options
          </Button>
        </div>
      </div>

      {showFilters && (
        <MatrixFilters
          energyFilter={energyFilter}
          tagFilter={tagFilter}
          availableTags={availableTags}
          onEnergyFilterChange={setEnergyFilter}
          onTagFilterChange={setTagFilter}
        />
      )}

      <div className="matrix-legend">
        {quadrants.map(quadrant => (
          <div key={quadrant.id} className="legend-item">
            <div 
              className="legend-color"
              style={{ backgroundColor: `var(--color-priority-${quadrant.color})` }}
            />
            <div className="legend-label">{quadrant.description}</div>
          </div>
        ))}
      </div>

      <div className={matrixClasses}>
        {quadrants.map(quadrant => {
          const quadrantTasks = categorizedTasks[quadrant.id as keyof typeof categorizedTasks];
          
          return shouldUseVirtualization ? (
            <VirtualizedMatrixQuadrant
              key={quadrant.id}
              quadrant={quadrant}
              tasks={quadrantTasks}
              compact={compact}
              onTaskMove={handleTaskMove}
              isSelected={selectedQuadrant === quadrant.id}
              onSelect={() => setSelectedQuadrant(
                selectedQuadrant === quadrant.id ? null : quadrant.id
              )}
              enableVirtualization={shouldUseVirtualization}
              maxHeight={performanceMode ? 600 : 400}
            />
          ) : (
            <MatrixQuadrant
              key={quadrant.id}
              quadrant={quadrant}
              tasks={quadrantTasks}
              compact={compact}
              onTaskMove={handleTaskMove}
              isSelected={selectedQuadrant === quadrant.id}
              onSelect={() => setSelectedQuadrant(
                selectedQuadrant === quadrant.id ? null : quadrant.id
              )}
            />
          );
        })}
      </div>
    </div>
  );
};

export default PriorityMatrix;