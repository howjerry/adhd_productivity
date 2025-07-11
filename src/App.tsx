import React, { useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/useAuthStore';
import { useTimerStore } from '@/stores/useTimerStore';
import { signalRService } from '@/services/signalRService';

// Layout components
import { MainLayout } from '@/components/layout/MainLayout';
import { AuthLayout } from '@/components/layout/AuthLayout';

// Page components
import { DashboardPage } from '@/pages/DashboardPage';
import { TasksPage } from '@/pages/TasksPage';
import { CalendarPage } from '@/pages/CalendarPage';
import { CapturePage } from '@/pages/CapturePage';
import { StatsPage } from '@/pages/StatsPage';
import { SettingsPage } from '@/pages/SettingsPage';
import { LoginPage } from '@/pages/LoginPage';
import { RegisterPage } from '@/pages/RegisterPage';

// Floating components
import QuickCapture from '@/components/features/QuickCapture';
import { useFloatingQuickCapture } from '@/hooks/useFloatingQuickCapture';
import { FloatingActionButton } from '@/components/ui/Button';

// Global styles
import '@/styles/main.scss';

const App: React.FC = () => {
  const { isAuthenticated, user } = useAuthStore();
  const { initialize: initializeTimer } = useTimerStore();
  const {
    isVisible: quickCaptureVisible,
    isMinimized: quickCaptureMinimized,
    toggle: toggleQuickCapture,
    minimize: minimizeQuickCapture,
  } = useFloatingQuickCapture();

  // Initialize application
  useEffect(() => {
    // Initialize timer store
    initializeTimer();

    // Initialize SignalR connection if authenticated
    if (isAuthenticated && user) {
      // SignalR service is automatically initialized
      console.log('App initialized for authenticated user');
    }

    // Cleanup function
    return () => {
      signalRService.disconnect();
    };
  }, [isAuthenticated, user, initializeTimer]);

  // Handle keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      // Global shortcuts for ADHD workflow
      if ((event.ctrlKey || event.metaKey) && event.shiftKey) {
        switch (event.key) {
          case 'N':
            event.preventDefault();
            toggleQuickCapture();
            break;
          case 'T':
            event.preventDefault();
            // TODO: Toggle timer
            break;
          case 'D':
            event.preventDefault();
            // TODO: Navigate to dashboard
            break;
        }
      }

      // Escape key to minimize floating elements
      if (event.key === 'Escape') {
        if (quickCaptureVisible && !quickCaptureMinimized) {
          minimizeQuickCapture();
        }
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [toggleQuickCapture, quickCaptureVisible, quickCaptureMinimized, minimizeQuickCapture]);

  // Authenticated app with main layout
  if (isAuthenticated && user) {
    return (
      <Router>
        <div className="app">
          <MainLayout>
            <Routes>
              <Route path="/" element={<Navigate to="/dashboard" replace />} />
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/tasks" element={<TasksPage />} />
              <Route path="/calendar" element={<CalendarPage />} />
              <Route path="/capture" element={<CapturePage />} />
              <Route path="/stats" element={<StatsPage />} />
              <Route path="/settings" element={<SettingsPage />} />
              <Route path="*" element={<Navigate to="/dashboard" replace />} />
            </Routes>
          </MainLayout>

          {/* Floating Quick Capture */}
          {quickCaptureVisible && (
            <QuickCapture
              floating
              minimized={quickCaptureMinimized}
              onToggleMinimized={toggleQuickCapture}
              onCapture={minimizeQuickCapture}
            />
          )}

          {/* Floating Action Button for Quick Capture */}
          {!quickCaptureVisible && (
            <FloatingActionButton
              icon={<span className="text-2xl">+</span>}
              onClick={toggleQuickCapture}
              aria-label="Quick capture task"
            />
          )}
        </div>
      </Router>
    );
  }

  // Unauthenticated app with auth layout
  return (
    <Router>
      <div className="app">
        <AuthLayout>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="*" element={<Navigate to="/login" replace />} />
          </Routes>
        </AuthLayout>
      </div>
    </Router>
  );
};

export default App;