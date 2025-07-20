import React, { useEffect, Suspense } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/useAuthStore';
import { useTimerStore } from '@/stores/useTimerStore';
import { signalRService } from '@/services/signalRService';

// Layout components
import { MainLayout } from '@/components/layout/MainLayout';
import { AuthLayout } from '@/components/layout/AuthLayout';

// Loading component
import { PageLoadingSpinner } from '@/components/ui/LoadingSpinner';

// Lazy loaded page components with proper error boundaries
const DashboardPage = React.lazy(() => import('@/pages/DashboardPage'));
const TasksPage = React.lazy(() => import('@/pages/TasksPage'));
const CalendarPage = React.lazy(() => import('@/pages/CalendarPage'));
const CapturePage = React.lazy(() => import('@/pages/CapturePage'));
const StatsPage = React.lazy(() => import('@/pages/StatsPage'));
const SettingsPage = React.lazy(() => import('@/pages/SettingsPage'));
const LoginPage = React.lazy(() => import('@/pages/LoginPage'));
const RegisterPage = React.lazy(() => import('@/pages/RegisterPage'));

// Floating components (not lazy loaded as they're frequently used)
import QuickCapture from '@/components/features/QuickCapture';
import { useFloatingQuickCapture } from '@/hooks/useFloatingQuickCapture';
import { FloatingActionButton } from '@/components/ui/Button';

// Global styles
import '@/styles/main.scss';

// Development tools
import PerformancePanel from '@/components/dev/PerformancePanel';

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
            <Suspense fallback={<PageLoadingSpinner />}>
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
            </Suspense>
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

          {/* Performance Panel - Only in development */}
          {process.env.NODE_ENV === 'development' && (
            <PerformancePanel componentName="AuthenticatedApp" />
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
          <Suspense fallback={<PageLoadingSpinner />}>
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route path="/register" element={<RegisterPage />} />
              <Route path="*" element={<Navigate to="/login" replace />} />
            </Routes>
          </Suspense>
        </AuthLayout>

        {/* Performance Panel - Only in development */}
        {process.env.NODE_ENV === 'development' && (
          <PerformancePanel componentName="UnauthenticatedApp" />
        )}
      </div>
    </Router>
  );
};

export default App;