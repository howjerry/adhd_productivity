import React from 'react';
import { ButtonProps } from '@/types';
import clsx from 'clsx';

interface ButtonComponentProps extends ButtonProps {
  children: React.ReactNode;
  icon?: React.ReactNode;
  iconPosition?: 'left' | 'right';
  fullWidth?: boolean;
  priority?: 'high' | 'medium' | 'low';
}

export const Button: React.FC<ButtonComponentProps> = ({
  variant = 'primary',
  size = 'md',
  disabled = false,
  loading = false,
  onClick,
  type = 'button',
  className,
  children,
  icon,
  iconPosition = 'left',
  fullWidth = false,
  priority,
  ...props
}) => {
  const buttonClasses = clsx(
    'btn',
    `btn-${variant}`,
    `btn-${size}`,
    {
      'btn-loading': loading,
      'w-full': fullWidth,
      [`btn-priority-${priority}`]: priority,
    },
    className
  );

  const handleClick = (event: React.MouseEvent<HTMLButtonElement>) => {
    if (!disabled && !loading && onClick) {
      onClick();
    }
  };

  const renderIcon = () => {
    if (loading) {
      return (
        <div className="btn-spinner">
          <svg
            className="animate-spin h-4 w-4"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle
              className="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              strokeWidth="4"
            ></circle>
            <path
              className="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            ></path>
          </svg>
        </div>
      );
    }

    if (icon) {
      return <span className="btn-icon">{icon}</span>;
    }

    return null;
  };

  const content = (
    <>
      {iconPosition === 'left' && renderIcon()}
      <span className={loading ? 'btn-content' : undefined}>{children}</span>
      {iconPosition === 'right' && !loading && renderIcon()}
    </>
  );

  return (
    <button
      type={type}
      className={buttonClasses}
      disabled={disabled || loading}
      onClick={handleClick}
      aria-disabled={disabled || loading}
      {...props}
    >
      {content}
    </button>
  );
};

// Specialized button variants for ADHD workflow
export const PrimaryButton: React.FC<Omit<ButtonComponentProps, 'variant'>> = (props) => (
  <Button variant="primary" {...props} />
);

export const SecondaryButton: React.FC<Omit<ButtonComponentProps, 'variant'>> = (props) => (
  <Button variant="secondary" {...props} />
);

export const OutlineButton: React.FC<Omit<ButtonComponentProps, 'variant'>> = (props) => (
  <Button variant="outline" {...props} />
);

export const GhostButton: React.FC<Omit<ButtonComponentProps, 'variant'>> = (props) => (
  <Button variant="ghost" {...props} />
);

export const DangerButton: React.FC<Omit<ButtonComponentProps, 'variant'>> = (props) => (
  <Button variant="danger" {...props} />
);

export const SuccessButton: React.FC<Omit<ButtonComponentProps, 'variant'>> = (props) => (
  <Button variant="success" {...props} />
);

// Icon-only button for compact layouts
export const IconButton: React.FC<Omit<ButtonComponentProps, 'children'> & { 
  icon: React.ReactNode;
  'aria-label': string;
}> = ({ icon, className, ...props }) => (
  <Button
    className={clsx('btn-icon-only', className)}
    {...props}
  >
    {icon}
  </Button>
);

// Floating Action Button for quick actions
export const FloatingActionButton: React.FC<Omit<ButtonComponentProps, 'children' | 'variant'> & {
  icon: React.ReactNode;
  'aria-label': string;
}> = ({ icon, className, ...props }) => (
  <Button
    variant="primary"
    className={clsx('btn-fab btn-icon-only', className)}
    {...props}
  >
    {icon}
  </Button>
);

export default Button;