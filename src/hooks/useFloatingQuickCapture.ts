import { useState } from 'react';

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