import React from 'react'
import { BellIcon, SearchIcon, PlusIcon } from 'lucide-react'
import { useAuthStore } from '../../stores/useAuthStore'

const Header = () => {
  const { user } = useAuthStore()

  const getCurrentTime = () => {
    return new Date().toLocaleTimeString('zh-TW', {
      hour: '2-digit',
      minute: '2-digit'
    })
  }

  const getGreeting = () => {
    const hour = new Date().getHours()
    if (hour < 6) return '深夜好'
    if (hour < 12) return '早安'
    if (hour < 18) return '午安'
    return '晚安'
  }

  return (
    <header className="bg-white shadow-sm border-b border-gray-200">
      <div className="px-6 py-4">
        <div className="flex items-center justify-between">
          {/* Left side - Greeting */}
          <div className="flex-1">
            <h1 className="text-2xl font-semibold text-gray-900">
              {getGreeting()}，{user?.fullName || '使用者'}！
            </h1>
            <p className="text-sm text-gray-500 mt-1">
              今天是 {new Date().toLocaleDateString('zh-TW', {
                year: 'numeric',
                month: 'long',
                day: 'numeric',
                weekday: 'long'
              })} • {getCurrentTime()}
            </p>
          </div>

          {/* Right side - Actions */}
          <div className="flex items-center space-x-4">
            {/* Quick Add */}
            <button className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-md transition-colors">
              <PlusIcon className="h-5 w-5" />
            </button>

            {/* Search */}
            <button className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-md transition-colors">
              <SearchIcon className="h-5 w-5" />
            </button>

            {/* Notifications */}
            <button className="relative p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-md transition-colors">
              <BellIcon className="h-5 w-5" />
              {/* Notification badge */}
              <span className="absolute top-1 right-1 block h-2 w-2 rounded-full bg-red-400"></span>
            </button>

            {/* User Profile Image */}
            <div className="w-8 h-8 bg-indigo-100 rounded-full flex items-center justify-center">
              <span className="text-indigo-600 font-semibold text-sm">
                {user?.fullName?.charAt(0)?.toUpperCase() || 'U'}
              </span>
            </div>
          </div>
        </div>

        {/* Energy & Focus Status */}
        {user?.profile && (
          <div className="mt-4 flex items-center space-x-6">
            <div className="flex items-center space-x-2">
              <span className="text-sm text-gray-500">能量等級:</span>
              <div className="flex space-x-1">
                {[1, 2, 3, 4, 5].map(level => (
                  <div
                    key={level}
                    className={`w-2 h-4 rounded-sm ${
                      level <= 3 ? 'bg-green-400' : 'bg-gray-200'
                    }`}
                  />
                ))}
              </div>
            </div>
            <div className="flex items-center space-x-2">
              <span className="text-sm text-gray-500">專注狀態:</span>
              <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                良好
              </span>
            </div>
            <div className="flex items-center space-x-2">
              <span className="text-sm text-gray-500">連續天數:</span>
              <span className="text-sm font-semibold text-indigo-600">
                {user.profile.streak} 天
              </span>
            </div>
          </div>
        )}
      </div>
    </header>
  )
}

export default Header