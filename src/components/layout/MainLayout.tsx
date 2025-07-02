import React, { useState } from 'react';
import { useLocation } from 'react-router-dom';
import { Header } from './Header';
import { Sidebar } from './Sidebar';
import clsx from 'clsx';

interface MainLayoutProps {
  children: React.ReactNode;
}

export const MainLayout: React.FC<MainLayoutProps> = ({ children }) => {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const location = useLocation();

  const toggleSidebar = () => {
    setSidebarOpen(!sidebarOpen);
  };

  const collapseSidebar = () => {
    setSidebarCollapsed(!sidebarCollapsed);
  };

  const closeSidebar = () => {
    setSidebarOpen(false);
  };

  // Get page title from route
  const getPageTitle = () => {
    const routes: Record<string, string> = {
      '/dashboard': 'Dashboard',
      '/tasks': 'Tasks',
      '/calendar': 'Calendar',
      '/capture': 'Capture',
      '/stats': 'Statistics',
      '/settings': 'Settings',
    };
    return routes[location.pathname] || 'ADHD Productivity';
  };

  return (
    <div className="min-h-screen bg-gray-50 flex">
      {/* Sidebar */}
      <Sidebar
        isOpen={sidebarOpen}
        isCollapsed={sidebarCollapsed}
        onToggle={toggleSidebar}
        onCollapse={collapseSidebar}
        onClose={closeSidebar}
      />

      {/* Main content area */}
      <div
        className={clsx(
          'main-content flex-1 flex flex-col transition-all duration-200',
          {
            'ml-64': sidebarOpen && !sidebarCollapsed,
            'ml-16': sidebarOpen && sidebarCollapsed,
            'ml-0': !sidebarOpen,
          }
        )}
      >
        {/* Header */}
        <Header
          title={getPageTitle()}
          onMenuClick={toggleSidebar}
          showMenuButton={true}
        />

        {/* Page content */}
        <main className="flex-1 overflow-y-auto">
          {children}
        </main>
      </div>

      {/* Mobile sidebar overlay */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 bg-black bg-opacity-50 z-40 lg:hidden"
          onClick={closeSidebar}
        />
      )}
    </div>
  );
};

export default MainLayout;