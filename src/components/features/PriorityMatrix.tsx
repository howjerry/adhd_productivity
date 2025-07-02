import React, { useState, useMemo } from 'react';
import { Task, Priority, EnergyLevel } from '@/types';
import { useTaskStore } from '@/stores/useTaskStore';
import clsx from 'clsx';
import { 
  AlertTriangle, 
  Target, 
  Clock, 
  Pause,
  Filter,
  MoreHorizontal,
  Plus,
  Zap,
  Timer
} from 'lucide-react';
import { Button } from '@/components/ui/Button';

interface PriorityMatrixProps {
  compact?: boolean;
  showFilters?: boolean;
  className?: string;
}

interface MatrixQuadrant {
  id: string;
  title: string;
  description: string;
  icon: React.ReactNode;
  urgent: boolean;
  important: boolean;
  color: string;
}

const quadrants: MatrixQuadrant[] = [
  {
    id: 'urgent-important',
    title: 'Do First',
    description: 'Urgent & Important',
    icon: <AlertTriangle className="w-5 h-5" />,
    urgent: true,
    important: true,
    color: 'red',
  },
  {
    id: 'not-urgent-important',
    title: 'Schedule',
    description: 'Not Urgent & Important',
    icon: <Target className="w-5 h-5" />,
    urgent: false,
    important: true,
    color: 'blue',
  },
  {
    id: 'urgent-not-important',
    title: 'Delegate',
    description: 'Urgent & Not Important',
    icon: <Clock className="w-5 h-5" />,
    urgent: true,
    important: false,
    color: 'yellow',
  },
  {
    id: 'not-urgent-not-important',
    title: 'Eliminate',
    description: 'Not Urgent & Not Important',
    icon: <Pause className="w-5 h-5" />,
    urgent: false,
    important: false,
    color: 'green',
  },
];

export const PriorityMatrix: React.FC<PriorityMatrixProps> = ({
  compact = false,
  showFilters = true,
  className,
}) => {
  const { tasks, updateTask } = useTaskStore();
  const [selectedQuadrant, setSelectedQuadrant] = useState<string | null>(null);
  const [energyFilter, setEnergyFilter] = useState<EnergyLevel | null>(null);
  const [tagFilter, setTagFilter] = useState<string | null>(null);

  // Categorize tasks into quadrants based on priority and due date
  const categorizedTasks = useMemo(() => {
    const now = new Date();
    const tomorrow = new Date(now);
    tomorrow.setDate(tomorrow.getDate() + 1);

    const categorized = {
      'urgent-important': [] as Task[],
      'not-urgent-important': [] as Task[],
      'urgent-not-important': [] as Task[],
      'not-urgent-not-important': [] as Task[],
    };

    tasks
      .filter(task => task.status !== 'completed' && task.status !== 'cancelled')
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

  // Get all unique tags for filtering
  const availableTags = useMemo(() => {
    const tags = new Set<string>();
    tasks.forEach(task => task.tags.forEach(tag => tags.add(tag)));
    return Array.from(tags).sort();
  }, [tasks]);

  // Handle task move between quadrants
  const handleTaskMove = async (taskId: string, newQuadrant: string) => {
    const task = tasks.find(t => t.id === taskId);
    if (!task) return;

    const quadrant = quadrants.find(q => q.id === newQuadrant);
    if (!quadrant) return;

    // Determine new priority based on quadrant
    let newPriority: Priority;
    let newDueDate: string | undefined = task.dueDate;

    if (quadrant.urgent && quadrant.important) {
      newPriority = Priority.HIGH;
      // Set due date to today if not already urgent
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
        <div className="matrix-filters">
          <span className="text-sm text-gray-600 mr-2">Filters:</span>
          
          {/* Energy Level Filter */}
          <button
            className={clsx('filter-button', { 'filter-active': !energyFilter })}
            onClick={() => setEnergyFilter(null)}
          >
            All Energy
          </button>
          {Object.values(EnergyLevel).map(level => (
            <button
              key={level}
              className={clsx('filter-button', { 'filter-active': energyFilter === level })}
              onClick={() => setEnergyFilter(energyFilter === level ? null : level)}
            >
              <Zap className="w-3 h-3 mr-1" />
              {level}
            </button>
          ))}

          {/* Tag Filter */}
          {availableTags.length > 0 && (
            <>
              <span className="text-gray-300">|</span>
              <button
                className={clsx('filter-button', { 'filter-active': !tagFilter })}
                onClick={() => setTagFilter(null)}
              >
                All Tags
              </button>
              {availableTags.slice(0, 5).map(tag => (
                <button
                  key={tag}
                  className={clsx('filter-button', { 'filter-active': tagFilter === tag })}
                  onClick={() => setTagFilter(tagFilter === tag ? null : tag)}
                >
                  #{tag}
                </button>
              ))}
            </>
          )}
        </div>
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

// Individual Matrix Quadrant Component
interface MatrixQuadrantProps {
  quadrant: MatrixQuadrant;
  tasks: Task[];
  compact: boolean;
  onTaskMove: (taskId: string, quadrantId: string) => void;
  isSelected: boolean;
  onSelect: () => void;
}

const MatrixQuadrant: React.FC<MatrixQuadrantProps> = ({
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

// Simplified Task Card for Matrix View
interface MatrixTaskCardProps {
  task: Task;
  compact: boolean;
}

const MatrixTaskCard: React.FC<MatrixTaskCardProps> = ({ task, compact }) => {
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

export default PriorityMatrix;