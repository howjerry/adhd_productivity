import React, { useState, useRef, useEffect } from 'react';
import { Priority, EnergyLevel, CaptureSource } from '@/types';
import { useTaskStore } from '@/stores/useTaskStore';
import clsx from 'clsx';
import { Mic, MicOff, Plus, Send, Lightbulb, Clock, Tag } from 'lucide-react';
import { Button } from '@/components/ui/Button';

interface QuickCaptureProps {
  floating?: boolean;
  minimized?: boolean;
  onToggleMinimized?: () => void;
  onCapture?: (content: string) => void;
  className?: string;
}

export const QuickCapture: React.FC<QuickCaptureProps> = ({
  floating = false,
  minimized = false,
  onToggleMinimized,
  onCapture,
  className,
}) => {
  const [content, setContent] = useState('');
  const [selectedPriority, setSelectedPriority] = useState<Priority>(Priority.MEDIUM);
  const [selectedEnergy, setSelectedEnergy] = useState<EnergyLevel | null>(null);
  const [selectedTags, setSelectedTags] = useState<string[]>([]);
  const [isRecording, setIsRecording] = useState(false);
  const [isProcessing, setIsProcessing] = useState(false);
  const [showShortcuts, setShowShortcuts] = useState(false);
  const [focused, setFocused] = useState(false);

  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const { createTask } = useTaskStore();

  // Common tags for quick selection
  const commonTags = [
    'work', 'personal', 'urgent', 'project', 'meeting', 'call', 'email', 
    'research', 'review', 'planning', 'creative', 'admin'
  ];

  // Auto-resize textarea
  const adjustTextareaHeight = () => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = `${textareaRef.current.scrollHeight}px`;
    }
  };

  useEffect(() => {
    adjustTextareaHeight();
  }, [content]);

  // Handle input change
  const handleContentChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setContent(e.target.value);
  };

  // Handle form submission
  const handleSubmit = async (e?: React.FormEvent) => {
    e?.preventDefault();
    
    if (!content.trim()) return;

    setIsProcessing(true);

    try {
      const taskData = {
        title: content.trim(),
        priority: selectedPriority,
        energyLevel: selectedEnergy || undefined,
        tags: selectedTags,
      };

      await createTask(taskData);
      
      // Clear form
      setContent('');
      setSelectedTags([]);
      setSelectedEnergy(null);
      setSelectedPriority(Priority.MEDIUM);
      
      // Call external callback
      if (onCapture) {
        onCapture(content);
      }

      // Auto-minimize if floating
      if (floating && onToggleMinimized) {
        onToggleMinimized();
      }
    } catch (error) {
      console.error('Failed to create task:', error);
    } finally {
      setIsProcessing(false);
    }
  };

  // Handle keyboard shortcuts
  const handleKeyDown = (e: React.KeyboardEvent) => {
    // Ctrl/Cmd + Enter to submit
    if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
      e.preventDefault();
      handleSubmit();
    }
    
    // Escape to clear or minimize
    if (e.key === 'Escape') {
      if (content) {
        setContent('');
      } else if (floating && onToggleMinimized) {
        onToggleMinimized();
      }
    }
    
    // Quick priority setting
    if (e.ctrlKey || e.metaKey) {
      switch (e.key) {
        case '1':
          e.preventDefault();
          setSelectedPriority(Priority.HIGH);
          break;
        case '2':
          e.preventDefault();
          setSelectedPriority(Priority.MEDIUM);
          break;
        case '3':
          e.preventDefault();
          setSelectedPriority(Priority.LOW);
          break;
      }
    }
  };

  // Handle tag selection
  const toggleTag = (tag: string) => {
    setSelectedTags(prev => 
      prev.includes(tag)
        ? prev.filter(t => t !== tag)
        : [...prev, tag]
    );
  };

  // Voice recording (placeholder)
  const toggleVoiceRecording = () => {
    if (isRecording) {
      setIsRecording(false);
      // Stop recording logic
    } else {
      setIsRecording(true);
      // Start recording logic
    }
  };

  // Smart suggestions based on content
  const getSmartSuggestions = () => {
    if (!content.trim()) return [];
    
    const suggestions = [];
    
    // Email detection
    if (content.toLowerCase().includes('email') || content.includes('@')) {
      suggestions.push({
        icon: <Tag className="w-4 h-4" />,
        text: 'Add "email" tag',
        action: () => toggleTag('email'),
      });
    }
    
    // Meeting detection
    if (content.toLowerCase().includes('meeting') || content.toLowerCase().includes('call')) {
      suggestions.push({
        icon: <Clock className="w-4 h-4" />,
        text: 'Schedule time block',
        action: () => {}, // TODO: Open time block scheduler
      });
    }
    
    // Urgency detection
    if (content.toLowerCase().includes('urgent') || content.toLowerCase().includes('asap')) {
      suggestions.push({
        icon: <Lightbulb className="w-4 h-4" />,
        text: 'Set high priority',
        action: () => setSelectedPriority(Priority.HIGH),
      });
    }
    
    return suggestions;
  };

  const suggestions = getSmartSuggestions();

  // If floating and minimized, show minimal button
  if (floating && minimized) {
    return (
      <div
        className={clsx(
          'quick-capture-floating',
          'capture-minimized',
          className
        )}
        onClick={onToggleMinimized}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            onToggleMinimized?.();
          }
        }}
        aria-label="Open quick capture"
      />
    );
  }

  const captureClasses = clsx(
    'quick-capture',
    {
      'quick-capture-floating': floating,
      'capture-focused': focused,
      'capture-processing': isProcessing,
    },
    className
  );

  return (
    <div className={captureClasses}>
      <form onSubmit={handleSubmit} className="capture-content">
        <div className="capture-input-area">
          <textarea
            ref={textareaRef}
            className="capture-input capture-input-expanding"
            placeholder="What's on your mind? (Ctrl+Enter to save)"
            value={content}
            onChange={handleContentChange}
            onKeyDown={handleKeyDown}
            onFocus={() => setFocused(true)}
            onBlur={() => setFocused(false)}
            rows={1}
          />
          
          {content.length > 0 && (
            <div className={clsx(
              'capture-counter',
              {
                'counter-warning': content.length > 200,
                'counter-limit': content.length > 300,
              }
            )}>
              {content.length}/300
            </div>
          )}
          
          {isRecording && (
            <div className="voice-capture">
              <div className="voice-icon">
                <Mic className="w-3 h-3" />
              </div>
              Recording...
            </div>
          )}
        </div>

        <div className="capture-actions">
          <div className="capture-actions-left">
            <div className="priority-selector">
              <button
                type="button"
                className={clsx('priority-option priority-high', {
                  selected: selectedPriority === Priority.HIGH
                })}
                onClick={() => setSelectedPriority(Priority.HIGH)}
                title="High Priority (Ctrl+1)"
              >
                H
              </button>
              <button
                type="button"
                className={clsx('priority-option priority-medium', {
                  selected: selectedPriority === Priority.MEDIUM
                })}
                onClick={() => setSelectedPriority(Priority.MEDIUM)}
                title="Medium Priority (Ctrl+2)"
              >
                M
              </button>
              <button
                type="button"
                className={clsx('priority-option priority-low', {
                  selected: selectedPriority === Priority.LOW
                })}
                onClick={() => setSelectedPriority(Priority.LOW)}
                title="Low Priority (Ctrl+3)"
              >
                L
              </button>
            </div>

            <div className="energy-selector">
              {Object.values(EnergyLevel).map((level) => (
                <button
                  key={level}
                  type="button"
                  className={clsx('energy-option', `energy-${level}`, {
                    selected: selectedEnergy === level
                  })}
                  onClick={() => setSelectedEnergy(selectedEnergy === level ? null : level)}
                  title={`${level} energy required`}
                />
              ))}
            </div>
          </div>

          <div className="capture-actions-right">
            <button
              type="button"
              className={clsx('timer-btn', {
                'timer-pause': isRecording,
                'timer-play': !isRecording,
              })}
              onClick={toggleVoiceRecording}
              title="Voice input"
            >
              {isRecording ? <MicOff className="w-4 h-4" /> : <Mic className="w-4 h-4" />}
            </button>

            <Button
              type="submit"
              variant="primary"
              size="sm"
              disabled={!content.trim() || isProcessing}
              loading={isProcessing}
              icon={<Send className="w-4 h-4" />}
            >
              Capture
            </Button>

            {floating && onToggleMinimized && (
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={onToggleMinimized}
                icon={<Plus className="w-4 h-4 transform rotate-45" />}
                aria-label="Minimize"
              />
            )}
          </div>
        </div>

        {focused && (
          <div className="tag-suggestions">
            {commonTags.map((tag) => (
              <button
                key={tag}
                type="button"
                className={clsx('tag-suggestion', {
                  'tag-selected': selectedTags.includes(tag)
                })}
                onClick={() => toggleTag(tag)}
              >
                {tag}
              </button>
            ))}
          </div>
        )}

        {suggestions.length > 0 && (
          <div className="capture-suggestions">
            <div className="suggestions-title">Smart suggestions:</div>
            {suggestions.map((suggestion, index) => (
              <div
                key={index}
                className="suggestion-item"
                onClick={suggestion.action}
              >
                <div className="suggestion-icon">{suggestion.icon}</div>
                <div className="suggestion-text">{suggestion.text}</div>
              </div>
            ))}
          </div>
        )}

        {focused && (
          <div className="capture-shortcuts">
            <div className="shortcuts-title">Keyboard shortcuts:</div>
            <div className="shortcut-list">
              <div className="shortcut-item">
                <span className="shortcut-key">Ctrl+Enter</span> Save
              </div>
              <div className="shortcut-item">
                <span className="shortcut-key">Ctrl+1/2/3</span> Priority
              </div>
              <div className="shortcut-item">
                <span className="shortcut-key">Escape</span> Clear/Close
              </div>
            </div>
          </div>
        )}
      </form>
    </div>
  );
};

// Floating Quick Capture Hook
export const useFloatingQuickCapture = () => {
  const [isVisible, setIsVisible] = useState(false);
  const [isMinimized, setIsMinimized] = useState(true);

  const show = () => {
    setIsVisible(true);
    setIsMinimized(false);
  };

  const hide = () => {
    setIsVisible(false);
  };

  const toggle = () => {
    if (!isVisible) {
      show();
    } else {
      setIsMinimized(!isMinimized);
    }
  };

  const minimize = () => {
    setIsMinimized(true);
  };

  return {
    isVisible,
    isMinimized,
    show,
    hide,
    toggle,
    minimize,
  };
};

export default QuickCapture;