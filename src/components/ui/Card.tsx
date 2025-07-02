import React from 'react';
import { CardProps } from '@/types';
import clsx from 'clsx';

interface CardComponentProps extends CardProps {
  hover?: boolean;
  loading?: boolean;
}

export const Card: React.FC<CardComponentProps> = ({
  elevation = 'sm',
  padding = 'md',
  hover = false,
  loading = false,
  className,
  children,
  ...props
}) => {
  const cardClasses = clsx(
    'card',
    `card-${padding}`,
    {
      'card-hover': hover,
      'card-loading': loading,
      'card-flat': elevation === 'sm',
      'card-elevated': elevation === 'lg',
    },
    className
  );

  return (
    <div className={cardClasses} {...props}>
      {children}
    </div>
  );
};

// Card Header Component
interface CardHeaderProps {
  title: string;
  subtitle?: string;
  actions?: React.ReactNode;
  className?: string;
}

export const CardHeader: React.FC<CardHeaderProps> = ({
  title,
  subtitle,
  actions,
  className,
}) => (
  <div className={clsx('card-header', className)}>
    <div>
      <h3 className="card-title">{title}</h3>
      {subtitle && <p className="card-subtitle">{subtitle}</p>}
    </div>
    {actions && <div className="card-actions">{actions}</div>}
  </div>
);

// Card Body Component
interface CardBodyProps {
  children: React.ReactNode;
  className?: string;
}

export const CardBody: React.FC<CardBodyProps> = ({ children, className }) => (
  <div className={clsx('card-body', className)}>{children}</div>
);

// Card Footer Component
interface CardFooterProps {
  children: React.ReactNode;
  className?: string;
}

export const CardFooter: React.FC<CardFooterProps> = ({ children, className }) => (
  <div className={clsx('card-footer', className)}>
    <div className="card-actions">{children}</div>
  </div>
);

// Task Card Component - Specialized for ADHD workflow
interface TaskCardProps {
  title: string;
  description?: string;
  priority: 'high' | 'medium' | 'low';
  status: 'active' | 'in_progress' | 'completed' | 'overdue';
  energyLevel?: 'high' | 'medium' | 'low' | 'depleted';
  estimatedTime?: number;
  tags?: string[];
  onClick?: () => void;
  onComplete?: () => void;
  onEdit?: () => void;
  className?: string;
}

export const TaskCard: React.FC<TaskCardProps> = ({
  title,
  description,
  priority,
  status,
  energyLevel,
  estimatedTime,
  tags = [],
  onClick,
  onComplete,
  onEdit,
  className,
}) => {
  const cardClasses = clsx(
    'card',
    'card-task',
    'card-md',
    `task-priority-${priority}`,
    `task-${status}`,
    {
      'card-hover': !!onClick,
    },
    className
  );

  const handleClick = () => {
    if (onClick) {
      onClick();
    }
  };

  const handleKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      handleClick();
    }
  };

  return (
    <div
      className={cardClasses}
      onClick={handleClick}
      onKeyDown={handleKeyDown}
      tabIndex={onClick ? 0 : undefined}
      role={onClick ? 'button' : undefined}
      aria-label={onClick ? `Open task: ${title}` : undefined}
    >
      <div className="flex items-start justify-between mb-2">
        <h3 className="task-title font-medium text-gray-900 flex-1">{title}</h3>
        <div className="flex items-center gap-2 ml-4">
          {energyLevel && (
            <span className={clsx('card-energy-indicator', `energy-${energyLevel}`)}>
              {energyLevel}
            </span>
          )}
          {estimatedTime && (
            <span className="text-sm text-gray-500">
              {estimatedTime}m
            </span>
          )}
        </div>
      </div>

      {description && (
        <p className="text-gray-600 text-sm mb-3 line-clamp-2">{description}</p>
      )}

      {tags.length > 0 && (
        <div className="flex flex-wrap gap-1 mb-3">
          {tags.map((tag, index) => (
            <span
              key={index}
              className="inline-flex items-center px-2 py-1 rounded-full text-xs bg-gray-100 text-gray-700"
            >
              {tag}
            </span>
          ))}
        </div>
      )}

      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <span className={clsx(
            'inline-flex items-center px-2 py-1 rounded-full text-xs font-medium',
            {
              'bg-red-100 text-red-800': priority === 'high',
              'bg-yellow-100 text-yellow-800': priority === 'medium',
              'bg-green-100 text-green-800': priority === 'low',
            }
          )}>
            {priority}
          </span>
          <span className={clsx(
            'inline-flex items-center px-2 py-1 rounded-full text-xs',
            {
              'bg-blue-100 text-blue-800': status === 'active',
              'bg-purple-100 text-purple-800': status === 'in_progress',
              'bg-green-100 text-green-800': status === 'completed',
              'bg-red-100 text-red-800': status === 'overdue',
            }
          )}>
            {status.replace('_', ' ')}
          </span>
        </div>

        <div className="flex items-center gap-1">
          {onComplete && status !== 'completed' && (
            <button
              onClick={(e) => {
                e.stopPropagation();
                onComplete();
              }}
              className="p-1 text-gray-400 hover:text-green-600 transition-colors"
              aria-label="Mark as complete"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </button>
          )}
          {onEdit && (
            <button
              onClick={(e) => {
                e.stopPropagation();
                onEdit();
              }}
              className="p-1 text-gray-400 hover:text-blue-600 transition-colors"
              aria-label="Edit task"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
              </svg>
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

// Progress Card for gamification
interface ProgressCardProps {
  value: number;
  label: string;
  icon?: React.ReactNode;
  className?: string;
}

export const ProgressCard: React.FC<ProgressCardProps> = ({
  value,
  label,
  icon,
  className,
}) => (
  <Card className={clsx('card-progress text-center', className)}>
    {icon && <div className="mb-2">{icon}</div>}
    <div className="progress-number">{value}</div>
    <div className="progress-label">{label}</div>
  </Card>
);

// Achievement Card
interface AchievementCardProps {
  title: string;
  description: string;
  icon: React.ReactNode;
  unlocked?: boolean;
  className?: string;
}

export const AchievementCard: React.FC<AchievementCardProps> = ({
  title,
  description,
  icon,
  unlocked = false,
  className,
}) => (
  <Card className={clsx(
    'card-achievement',
    { 'achievement-unlocked': unlocked },
    className
  )}>
    <div className="achievement-icon">{icon}</div>
    <h4 className="achievement-title">{title}</h4>
    <p className="achievement-description">{description}</p>
  </Card>
);

export default Card;