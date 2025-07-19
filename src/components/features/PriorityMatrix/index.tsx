import React, { useState } from 'react';
import { EnergyLevel } from '@/types';
import clsx from 'clsx';
import { Plus, MoreHorizontal } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { MatrixQuadrant } from './MatrixQuadrant';
import { MatrixFilters } from './MatrixFilters';
import { useFilteredTasks } from '@/hooks/useFilteredTasks';
import { useTaskDragDrop } from '@/hooks/useTaskDragDrop';
import { useTaskStore } from '@/stores/useTaskStore';
import { quadrants } from './constants';

interface PriorityMatrixProps {
  compact?: boolean;
  showFilters?: boolean;
  className?: string;
}

export const PriorityMatrix: React.FC<PriorityMatrixProps> = ({
  compact = false,
  showFilters = true,
  className,
}) => {
  const { tasks } = useTaskStore();
  const [selectedQuadrant, setSelectedQuadrant] = useState<string | null>(null);
  const [energyFilter, setEnergyFilter] = useState<EnergyLevel | null>(null);
  const [tagFilter, setTagFilter] = useState<string | null>(null);

  const { categorizedTasks, availableTags } = useFilteredTasks({
    tasks,
    energyFilter,
    tagFilter,
  });

  const { handleTaskMove } = useTaskDragDrop();

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
        <h2 className="matrix-title">Priority Matrix</h2>
        <div className="matrix-actions">
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
          
          return (
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