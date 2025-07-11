import React from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { useTaskStore } from '@/stores/useTaskStore';
import { useTimerStore } from '@/stores/useTimerStore';
import clsx from 'clsx';
import {
  LayoutDashboard,
  CheckSquare,
  Calendar,
  Inbox,
  BarChart3,
  Settings,
  Timer,
  Target,
  ChevronLeft,
  ChevronRight,
  Brain,
} from 'lucide-react';

interface SidebarProps {
  isOpen: boolean;
  isCollapsed: boolean;
  onToggle: () => void;
  onCollapse: () => void;
  onClose: () => void;
}

interface NavigationItem {
  name: string;
  href: string;
  icon: React.ReactNode;
  badge?: string | number;
  description?: string;
}

export const Sidebar: React.FC<SidebarProps> = ({
  isOpen,
  isCollapsed,
  onToggle: _onToggle,
  onCollapse,
  onClose,
}) => {
  const location = useLocation();
  const { tasks } = useTaskStore();
  const { isRunning, sessionCount } = useTimerStore();

  // Calculate task counts for badges
  const activeTasks = tasks.filter(task => task.status === 'active').length;
  const todayTasks = tasks.filter(task => {
    if (!task.scheduledDate) return false;
    const today = new Date().toDateString();
    return new Date(task.scheduledDate).toDateString() === today;
  }).length;

  const navigation: NavigationItem[] = [
    {
      name: 'Dashboard',
      href: '/dashboard',
      icon: <LayoutDashboard className="w-5 h-5" />,
      description: 'Overview and quick actions',
    },
    {
      name: 'Tasks',
      href: '/tasks',
      icon: <CheckSquare className="w-5 h-5" />,
      badge: activeTasks > 0 ? activeTasks : undefined,
      description: 'Manage your tasks',
    },
    {
      name: 'Calendar',
      href: '/calendar',
      icon: <Calendar className="w-5 h-5" />,
      badge: todayTasks > 0 ? todayTasks : undefined,
      description: 'Schedule and time blocks',
    },
    {
      name: 'Capture',
      href: '/capture',
      icon: <Inbox className="w-5 h-5" />,
      description: 'Quick input and processing',
    },
    {
      name: 'Statistics',
      href: '/stats',
      icon: <BarChart3 className="w-5 h-5" />,
      description: 'Progress and insights',
    },
    {
      name: 'Settings',
      href: '/settings',
      icon: <Settings className="w-5 h-5" />,
      description: 'Preferences and configuration',
    },
  ];

  const isActive = (href: string) => location.pathname === href;

  return (
    <>
      {/* Sidebar */}
      <div
        className={clsx(
          'fixed inset-y-0 left-0 z-50 flex flex-col bg-white border-r border-gray-200 transition-all duration-200',
          {
            'w-64': isOpen && !isCollapsed,
            'w-16': isOpen && isCollapsed,
            '-translate-x-full lg:translate-x-0': !isOpen,
            'translate-x-0': isOpen,
          }
        )}
      >
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200">
          <div className={clsx('flex items-center gap-3', { 'justify-center': isCollapsed })}>
            <div className="flex items-center justify-center w-8 h-8 bg-indigo-600 rounded-lg">
              <Brain className="w-5 h-5 text-white" />
            </div>
            {!isCollapsed && (
              <div>
                <h2 className="text-lg font-bold text-gray-900">ADHD Focus</h2>
                <p className="text-xs text-gray-500">Productivity System</p>
              </div>
            )}
          </div>
          
          {/* Collapse button */}
          <button
            onClick={onCollapse}
            className={clsx(
              'p-1 rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-colors',
              { 'hidden': isCollapsed }
            )}
            aria-label={isCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          >
            {isCollapsed ? (
              <ChevronRight className="w-4 h-4" />
            ) : (
              <ChevronLeft className="w-4 h-4" />
            )}
          </button>
        </div>

        {/* Timer status */}
        {isRunning && (
          <div className={clsx('p-4 bg-indigo-50 border-b border-gray-200', { 'px-2': isCollapsed })}>
            <div className={clsx('flex items-center gap-3', { 'justify-center': isCollapsed })}>
              <div className="flex items-center justify-center w-8 h-8 bg-indigo-100 rounded-full">
                <Timer className="w-4 h-4 text-indigo-600" />
              </div>
              {!isCollapsed && (
                <div>
                  <p className="text-sm font-medium text-indigo-900">Focus Session</p>
                  <p className="text-xs text-indigo-600">Session {sessionCount + 1} running</p>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Navigation */}
        <nav className="flex-1 p-4 space-y-2 overflow-y-auto">
          {navigation.map((item) => {
            const active = isActive(item.href);
            
            return (
              <NavLink
                key={item.name}
                to={item.href}
                onClick={() => {
                  // Close sidebar on mobile when navigating
                  if (window.innerWidth < 1024) {
                    onClose();
                  }
                }}
                className={clsx(
                  'flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors group relative',
                  {
                    'bg-indigo-50 text-indigo-700 border border-indigo-200': active,
                    'text-gray-700 hover:bg-gray-100': !active,
                    'justify-center px-2': isCollapsed,
                  }
                )}
                title={isCollapsed ? item.name : ''}
              >
                <div className="flex-shrink-0 relative">
                  {item.icon}
                  {item.badge && (
                    <span className="absolute -top-1 -right-1 bg-red-500 text-white text-xs rounded-full w-4 h-4 flex items-center justify-center font-bold">
                      {item.badge}
                    </span>
                  )}
                </div>
                
                {!isCollapsed && (
                  <>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center justify-between">
                        <span className="truncate">{item.name}</span>
                        {item.badge && (
                          <span className="bg-red-500 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center font-bold ml-2">
                            {item.badge}
                          </span>
                        )}
                      </div>
                      {item.description && (
                        <p className="text-xs text-gray-500 mt-0.5 truncate">
                          {item.description}
                        </p>
                      )}
                    </div>
                  </>
                )}

                {/* Tooltip for collapsed state */}
                {isCollapsed && (
                  <div className="absolute left-full ml-2 px-2 py-1 bg-gray-900 text-white text-xs rounded opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all duration-200 whitespace-nowrap z-50">
                    {item.name}
                    {item.badge && (
                      <span className="ml-1 bg-red-500 rounded-full w-4 h-4 flex items-center justify-center text-xs">
                        {item.badge}
                      </span>
                    )}
                  </div>
                )}
              </NavLink>
            );
          })}
        </nav>

        {/* Priority Matrix Quick Access */}
        <div className={clsx('p-4 border-t border-gray-200', { 'px-2': isCollapsed })}>
          <div className={clsx('flex items-center gap-3', { 'justify-center': isCollapsed })}>
            <div className="flex items-center justify-center w-8 h-8 bg-green-100 rounded-full">
              <Target className="w-4 h-4 text-green-600" />
            </div>
            {!isCollapsed && (
              <div>
                <p className="text-sm font-medium text-gray-900">Quick Focus</p>
                <p className="text-xs text-gray-500">Priority matrix view</p>
              </div>
            )}
          </div>
        </div>

        {/* Footer */}
        <div className={clsx('p-4 border-t border-gray-200 text-center', { 'px-2': isCollapsed })}>
          {!isCollapsed ? (
            <p className="text-xs text-gray-400">
              v1.0.0 • Built for ADHD minds
            </p>
          ) : (
            <div className="w-2 h-2 bg-green-400 rounded-full mx-auto" title="System online" />
          )}
        </div>
      </div>
    </>
  );
};

export default Sidebar;