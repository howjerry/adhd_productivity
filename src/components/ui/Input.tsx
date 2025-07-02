import React, { forwardRef } from 'react';
import clsx from 'clsx';
import { Search, X, ChevronUp, ChevronDown, AlertCircle, CheckCircle } from 'lucide-react';

// Base Input Props
interface BaseInputProps {
  id?: string;
  name?: string;
  value?: string;
  defaultValue?: string;
  placeholder?: string;
  disabled?: boolean;
  required?: boolean;
  className?: string;
  size?: 'sm' | 'md' | 'lg';
  error?: boolean;
  success?: boolean;
  onChange?: (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => void;
  onFocus?: (e: React.FocusEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => void;
  onBlur?: (e: React.FocusEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => void;
}

// Text Input Component
interface InputProps extends BaseInputProps {
  type?: 'text' | 'email' | 'password' | 'url' | 'tel';
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
  priority?: 'high' | 'medium' | 'low';
}

export const Input = forwardRef<HTMLInputElement, InputProps>(({
  type = 'text',
  size = 'md',
  error = false,
  success = false,
  leftIcon,
  rightIcon,
  priority,
  className,
  disabled,
  ...props
}, ref) => {
  const inputClasses = clsx(
    'input',
    `input-${size}`,
    {
      'input-error': error,
      'input-success': success,
      'has-icon-left': !!leftIcon,
      'has-icon-right': !!rightIcon,
      'input-adhd-enhanced': !!priority,
      [`priority-${priority}`]: !!priority,
    },
    className
  );

  const inputElement = (
    <input
      ref={ref}
      type={type}
      className={inputClasses}
      disabled={disabled}
      {...props}
    />
  );

  if (leftIcon || rightIcon) {
    return (
      <div className="input-with-icon">
        {leftIcon && <div className="input-icon icon-left">{leftIcon}</div>}
        {inputElement}
        {rightIcon && <div className="input-icon icon-right">{rightIcon}</div>}
      </div>
    );
  }

  return inputElement;
});

Input.displayName = 'Input';

// Textarea Component
interface TextareaProps extends Omit<BaseInputProps, 'onChange'> {
  rows?: number;
  resize?: boolean;
  autoResize?: boolean;
  onChange?: (e: React.ChangeEvent<HTMLTextAreaElement>) => void;
}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(({
  size = 'md',
  error = false,
  success = false,
  rows = 3,
  resize = true,
  autoResize = false,
  className,
  disabled,
  onChange,
  ...props
}, ref) => {
  const textareaClasses = clsx(
    'input',
    'textarea',
    `input-${size}`,
    {
      'input-error': error,
      'input-success': success,
      'resize-none': !resize,
    },
    className
  );

  const handleChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    if (autoResize) {
      e.target.style.height = 'auto';
      e.target.style.height = `${e.target.scrollHeight}px`;
    }
    onChange?.(e);
  };

  return (
    <textarea
      ref={ref}
      rows={rows}
      className={textareaClasses}
      disabled={disabled}
      onChange={handleChange}
      {...props}
    />
  );
});

Textarea.displayName = 'Textarea';

// Select Component
interface SelectOption {
  value: string;
  label: string;
  disabled?: boolean;
}

interface SelectProps extends Omit<BaseInputProps, 'onChange'> {
  options: SelectOption[];
  onChange?: (e: React.ChangeEvent<HTMLSelectElement>) => void;
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(({
  options,
  size = 'md',
  error = false,
  success = false,
  className,
  disabled,
  placeholder,
  ...props
}, ref) => {
  const selectClasses = clsx(
    'select',
    `input-${size}`,
    {
      'input-error': error,
      'input-success': success,
    },
    className
  );

  return (
    <select
      ref={ref}
      className={selectClasses}
      disabled={disabled}
      {...props}
    >
      {placeholder && (
        <option value="" disabled>
          {placeholder}
        </option>
      )}
      {options.map((option) => (
        <option
          key={option.value}
          value={option.value}
          disabled={option.disabled}
        >
          {option.label}
        </option>
      ))}
    </select>
  );
});

Select.displayName = 'Select';

// Search Input Component
interface SearchInputProps extends Omit<InputProps, 'type' | 'leftIcon' | 'rightIcon'> {
  onClear?: () => void;
  showClearButton?: boolean;
}

export const SearchInput: React.FC<SearchInputProps> = ({
  value,
  onClear,
  showClearButton = true,
  className,
  ...props
}) => {
  const handleClear = () => {
    onClear?.();
  };

  return (
    <div className={clsx('search-input', className)}>
      <Search className="search-icon w-4 h-4" />
      <Input
        type="text"
        value={value}
        className="pl-10 pr-10"
        {...props}
      />
      {showClearButton && value && (
        <button
          type="button"
          className="clear-button"
          onClick={handleClear}
          aria-label="Clear search"
        >
          <X className="w-4 h-4" />
        </button>
      )}
    </div>
  );
};

// Number Input with Steppers
interface NumberInputProps extends Omit<InputProps, 'type' | 'onChange'> {
  min?: number;
  max?: number;
  step?: number;
  onChange?: (value: number) => void;
}

export const NumberInput: React.FC<NumberInputProps> = ({
  value,
  min,
  max,
  step = 1,
  disabled,
  onChange,
  className,
  ...props
}) => {
  const numericValue = typeof value === 'string' ? parseInt(value) || 0 : value || 0;

  const handleIncrement = () => {
    const newValue = numericValue + step;
    if (max === undefined || newValue <= max) {
      onChange?.(newValue);
    }
  };

  const handleDecrement = () => {
    const newValue = numericValue - step;
    if (min === undefined || newValue >= min) {
      onChange?.(newValue);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = parseInt(e.target.value) || 0;
    onChange?.(newValue);
  };

  const canIncrement = max === undefined || numericValue < max;
  const canDecrement = min === undefined || numericValue > min;

  return (
    <div className={clsx('number-input', className)}>
      <Input
        type="number"
        value={numericValue.toString()}
        min={min}
        max={max}
        step={step}
        disabled={disabled}
        onChange={handleInputChange}
        {...props}
      />
      <div className="number-steppers">
        <button
          type="button"
          className="stepper-button"
          onClick={handleIncrement}
          disabled={disabled || !canIncrement}
          aria-label="Increase value"
        >
          <ChevronUp className="w-3 h-3" />
        </button>
        <button
          type="button"
          className="stepper-button"
          onClick={handleDecrement}
          disabled={disabled || !canDecrement}
          aria-label="Decrease value"
        >
          <ChevronDown className="w-3 h-3" />
        </button>
      </div>
    </div>
  );
};

// Input Group Component
interface InputGroupProps {
  label?: string;
  required?: boolean;
  error?: string;
  success?: string;
  help?: string;
  children: React.ReactNode;
  className?: string;
}

export const InputGroup: React.FC<InputGroupProps> = ({
  label,
  required,
  error,
  success,
  help,
  children,
  className,
}) => {
  return (
    <div className={clsx('input-group', className)}>
      {label && (
        <label className={clsx('input-label', { 'label-required': required })}>
          {label}
        </label>
      )}
      {children}
      {help && !error && !success && (
        <div className="input-help">{help}</div>
      )}
      {error && (
        <div className="input-error-text">
          <AlertCircle className="w-4 h-4" />
          {error}
        </div>
      )}
      {success && (
        <div className="input-success-text">
          <CheckCircle className="w-4 h-4" />
          {success}
        </div>
      )}
    </div>
  );
};

// Checkbox Component
interface CheckboxProps {
  id?: string;
  name?: string;
  checked?: boolean;
  defaultChecked?: boolean;
  disabled?: boolean;
  required?: boolean;
  label?: string;
  onChange?: (checked: boolean) => void;
  className?: string;
}

export const Checkbox: React.FC<CheckboxProps> = ({
  id,
  label,
  checked,
  onChange,
  className,
  ...props
}) => {
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onChange?.(e.target.checked);
  };

  return (
    <div className={clsx('checkbox-group', className)}>
      <input
        id={id}
        type="checkbox"
        className="checkbox"
        checked={checked}
        onChange={handleChange}
        {...props}
      />
      {label && (
        <label htmlFor={id} className="checkbox-label">
          {label}
        </label>
      )}
    </div>
  );
};

// Radio Group Component
interface RadioOption {
  value: string;
  label: string;
  disabled?: boolean;
}

interface RadioGroupProps {
  name: string;
  value?: string;
  options: RadioOption[];
  onChange?: (value: string) => void;
  className?: string;
}

export const RadioGroup: React.FC<RadioGroupProps> = ({
  name,
  value,
  options,
  onChange,
  className,
}) => {
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onChange?.(e.target.value);
  };

  return (
    <div className={clsx('radio-group', className)}>
      {options.map((option) => (
        <div key={option.value} className="radio-option">
          <input
            id={`${name}-${option.value}`}
            type="radio"
            name={name}
            value={option.value}
            checked={value === option.value}
            disabled={option.disabled}
            onChange={handleChange}
            className="radio"
          />
          <label
            htmlFor={`${name}-${option.value}`}
            className="radio-label"
          >
            {option.label}
          </label>
        </div>
      ))}
    </div>
  );
};

export default Input;