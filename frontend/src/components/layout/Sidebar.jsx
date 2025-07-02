import React from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { useAuthStore } from '../../stores/useAuthStore'
import {
  HomeIcon,
  CheckSquareIcon,
  ClockIcon,
  TargetIcon,
  HeartIcon,
  UsersIcon,
  TrophyIcon,
  BarChartIcon,
  SettingsIcon,
  LogOutIcon
} from 'lucide-react'

const Sidebar = () => {
  const navigate = useNavigate()
  const { user, logout } = useAuthStore()

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  const menuItems = [
    {
      name: '儀表板',
      path: '/dashboard',
      icon: HomeIcon,
      description: '總覽與快速存取'
    },
    {
      name: '任務管理',
      path: '/tasks',
      icon: CheckSquareIcon,
      description: '任務列表與優先級'
    },
    {
      name: '番茄計時器',
      path: '/pomodoro',
      icon: ClockIcon,
      description: '專注時間管理'
    },
    {
      name: '習慣追蹤',
      path: '/habits',
      icon: TargetIcon,
      description: '建立良好習慣'
    },
    {
      name: '情緒記錄',
      path: '/mood',
      icon: HeartIcon,
      description: '心情與能量追蹤'
    },
    {
      name: 'Body Doubling',
      path: '/body-doubling',
      icon: UsersIcon,
      description: '與他人一起專注'
    },
    {
      name: '成就系統',
      path: '/achievements',
      icon: TrophyIcon,
      description: '成就與獎勵'
    },
    {
      name: '數據分析',
      path: '/analytics',
      icon: BarChartIcon,
      description: '生產力統計'
    },
    {
      name: '設定',
      path: '/settings',
      icon: SettingsIcon,
      description: '個人化設定'
    }
  ]

  const adhdTypeLabels = {
    1: '混合型',
    2: '注意力不足型',
    3: '過動衝動型'
  }

  return (
    <div className="fixed inset-y-0 left-0 w-64 bg-white shadow-lg z-50">
      {/* Logo */}
      <div className="flex items-center justify-center h-16 px-6 bg-indigo-600">
        <h1 className="text-xl font-bold text-white">🧠 ADHD 系統</h1>
      </div>

      {/* User Info */}
      <div className="p-4 border-b border-gray-200">
        <div className="flex items-center space-x-3">
          <div className="w-10 h-10 bg-indigo-100 rounded-full flex items-center justify-center">
            <span className="text-indigo-600 font-semibold text-lg">
              {user?.fullName?.charAt(0)?.toUpperCase() || 'U'}
            </span>
          </div>
          <div className="flex-1">
            <p className="text-sm font-medium text-gray-900">
              {user?.fullName || '使用者'}
            </p>
            <p className="text-xs text-gray-500">
              {adhdTypeLabels[user?.adhdType] || 'ADHD 類型'}
            </p>
            {user?.profile && (
              <div className="flex items-center mt-1">
                <span className="text-xs text-indigo-600 font-medium">
                  Lv.{user.profile.level}
                </span>
                <span className="ml-2 text-xs text-yellow-600">
                  ⭐ {user.profile.points}
                </span>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 px-2 py-4 space-y-1">
        {menuItems.map((item) => {
          const Icon = item.icon
          return (
            <NavLink
              key={item.path}
              to={item.path}
              className={({ isActive }) =>
                `group flex items-center px-3 py-2 text-sm font-medium rounded-md transition-colors duration-200 ${
                  isActive
                    ? 'bg-indigo-100 text-indigo-900 border-r-2 border-indigo-600'
                    : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
                }`
              }
              title={item.description}
            >
              <Icon className="mr-3 h-5 w-5 flex-shrink-0" />
              {item.name}
            </NavLink>
          )
        })}
      </nav>

      {/* Logout */}
      <div className="border-t border-gray-200 p-4">
        <button
          onClick={handleLogout}
          className="group flex w-full items-center px-3 py-2 text-sm font-medium text-gray-600 rounded-md hover:bg-red-50 hover:text-red-600 transition-colors duration-200"
        >
          <LogOutIcon className="mr-3 h-5 w-5 flex-shrink-0" />
          登出
        </button>
      </div>
    </div>
  )
}

export default Sidebar