import React from 'react';
import { useAuthStore } from '@/stores/useAuthStore';
import { useTimerStore } from '@/stores/useTimerStore';
import { VisualTimer } from '@/components/features/VisualTimer';
import { Button } from '@/components/ui/Button';
import {
  Menu,
  Bell,
  Settings,
  User,
  LogOut,
  Search,
  Plus,
} from 'lucide-react';
import clsx from 'clsx';

interface HeaderProps {
  title: string;
  onMenuClick: () => void;
  showMenuButton?: boolean;
  className?: string;
}

export const Header: React.FC<HeaderProps> = ({
  title,
  onMenuClick,
  showMenuButton = true,
  className,
}) => {
  const { user, logout } = useAuthStore();
  const { isRunning, timeRemaining } = useTimerStore();

  const formatTime = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  const handleLogout = () => {
    logout();
  };

  return (
    <header className={clsx('bg-white border-b border-gray-200 px-6 py-4', className)}>
      <div className="flex items-center justify-between">
        {/* Left section */}
        <div className="flex items-center gap-4">
          {showMenuButton && (
            <Button
              variant="ghost"
              size="sm"
              onClick={onMenuClick}
              icon={<Menu className="w-5 h-5" />}
              className="lg:hidden"
              aria-label="Toggle menu"
            />
          )}
          
          <div>
            <h1 className="text-2xl font-bold text-gray-900">{title}</h1>
            {user && (
              <p className="text-sm text-gray-600">
                Welcome back, {user.displayName || user.username}!
              </p>
            )}
          </div>
        </div>

        {/* Center section - Compact timer when running */}
        {isRunning && (
          <div className="hidden md:block">
            <VisualTimer compact />
          </div>
        )}

        {/* Right section */}
        <div className="flex items-center gap-3">
          {/* Quick search */}
          <Button
            variant="ghost"
            size="sm"
            icon={<Search className="w-5 h-5" />}
            className="hidden md:flex"
            aria-label="Search"
          />

          {/* Quick add */}
          <Button
            variant="ghost"
            size="sm"
            icon={<Plus className="w-5 h-5" />}
            aria-label="Quick add"
          />

          {/* Notifications */}
          <div className="relative">
            <Button
              variant="ghost"
              size="sm"
              icon={<Bell className="w-5 h-5" />}
              aria-label="Notifications"
            />
            {/* Notification badge */}
            <span className="absolute -top-1 -right-1 bg-red-500 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center">
              3
            </span>
          </div>

          {/* Timer display (mobile) */}
          {isRunning && (
            <div className="md:hidden bg-gray-100 px-3 py-1 rounded-full">
              <span className="text-sm font-mono font-medium">
                {formatTime(timeRemaining)}
              </span>
            </div>
          )}

          {/* User menu */}
          <div className="relative group">
            <Button
              variant="ghost"
              size="sm"
              className="flex items-center gap-2"
            >
              {user?.avatar ? (
                <img
                  src={user.avatar}
                  alt={user.displayName || user.username}
                  className="w-6 h-6 rounded-full"
                />
              ) : (
                <User className="w-5 h-5" />
              )}
              <span className="hidden sm:block text-sm font-medium">
                {user?.displayName || user?.username}
              </span>
            </Button>

            {/* Dropdown menu */}
            <div className="absolute right-0 top-full mt-2 w-48 bg-white rounded-lg shadow-lg border border-gray-200 py-2 opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all duration-200 z-50">
              <div className="px-4 py-2 border-b border-gray-100">
                <p className="text-sm font-medium text-gray-900">
                  {user?.displayName || user?.username}
                </p>
                <p className="text-xs text-gray-500">{user?.email}</p>
              </div>
              
              <button className="flex items-center gap-3 w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-50">
                <User className="w-4 h-4" />
                Profile
              </button>
              
              <button className="flex items-center gap-3 w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-50">
                <Settings className="w-4 h-4" />
                Settings
              </button>
              
              <div className="border-t border-gray-100 mt-2 pt-2">
                <button
                  onClick={handleLogout}
                  className="flex items-center gap-3 w-full px-4 py-2 text-sm text-red-600 hover:bg-red-50"
                >
                  <LogOut className="w-4 h-4" />
                  Sign out
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </header>
  );
};

export default Header;