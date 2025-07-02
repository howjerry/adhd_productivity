import React from 'react'
import { useAuthStore } from '../../stores/useAuthStore'
import QuickCapture from '../features/QuickCapture'
import VisualTimer from '../features/VisualTimer'
import TaskSummary from '../features/TaskSummary'
import MoodTracker from '../features/MoodTracker'
import HabitProgress from '../features/HabitProgress'
import AchievementDisplay from '../features/AchievementDisplay'

const Dashboard = () => {
  const { user } = useAuthStore()

  const adhdTypeMessages = {
    1: '混合型 ADHD：建議結合短時間專注與活動休息',
    2: '注意力不足型：專注於減少干擾和提升專注力',
    3: '過動衝動型：利用身體活動來輔助專注'
  }

  return (
    <div className="space-y-6">
      {/* Welcome Section */}
      <div className="bg-gradient-to-r from-indigo-500 to-purple-600 rounded-lg shadow-lg text-white p-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold mb-2">
              歡迎回來，{user?.fullName}！
            </h1>
            <p className="text-indigo-100 mb-4">
              {adhdTypeMessages[user?.adhdType] || '讓我們一起提升您的生產力'}
            </p>
            {user?.profile && (
              <div className="flex items-center space-x-4">
                <div className="bg-white/20 rounded-lg px-3 py-1">
                  <span className="text-sm">等級 {user.profile.level}</span>
                </div>
                <div className="bg-white/20 rounded-lg px-3 py-1">
                  <span className="text-sm">⭐ {user.profile.points} 積分</span>
                </div>
                <div className="bg-white/20 rounded-lg px-3 py-1">
                  <span className="text-sm">🔥 {user.profile.streak} 天連續</span>
                </div>
              </div>
            )}
          </div>
          <div className="text-6xl">🧠</div>
        </div>
      </div>

      {/* Quick Actions */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Quick Capture */}
        <div className="bg-white rounded-lg shadow-md p-6">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">🎯 快速捕捉</h2>
          <QuickCapture />
        </div>

        {/* Visual Timer */}
        <div className="bg-white rounded-lg shadow-md p-6">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">⏰ 番茄計時器</h2>
          <VisualTimer />
        </div>
      </div>

      {/* Main Dashboard Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Task Summary */}
        <div className="bg-white rounded-lg shadow-md p-6">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">📋 任務總覽</h2>
          <TaskSummary />
        </div>

        {/* Mood Tracker */}
        <div className="bg-white rounded-lg shadow-md p-6">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">😊 心情追蹤</h2>
          <MoodTracker />
        </div>

        {/* Habit Progress */}
        <div className="bg-white rounded-lg shadow-md p-6">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">🎯 習慣進度</h2>
          <HabitProgress />
        </div>
      </div>

      {/* Achievements */}
      <div className="bg-white rounded-lg shadow-md p-6">
        <h2 className="text-xl font-semibold text-gray-900 mb-4">🏆 最近成就</h2>
        <AchievementDisplay />
      </div>

      {/* Today's Focus */}
      <div className="bg-white rounded-lg shadow-md p-6">
        <h2 className="text-xl font-semibold text-gray-900 mb-4">💡 今日焦點</h2>
        <div className="space-y-4">
          <div className="flex items-center justify-between p-4 bg-yellow-50 rounded-lg border border-yellow-200">
            <div>
              <h3 className="font-medium text-yellow-800">建議的高能量時段</h3>
              <p className="text-sm text-yellow-600">上午 9:00 - 11:00</p>
            </div>
            <div className="text-2xl">☀️</div>
          </div>
          <div className="flex items-center justify-between p-4 bg-blue-50 rounded-lg border border-blue-200">
            <div>
              <h3 className="font-medium text-blue-800">今日推薦任務</h3>
              <p className="text-sm text-blue-600">完成 3 個高優先級任務</p>
            </div>
            <div className="text-2xl">🎯</div>
          </div>
          <div className="flex items-center justify-between p-4 bg-green-50 rounded-lg border border-green-200">
            <div>
              <h3 className="font-medium text-green-800">休息提醒</h3>
              <p className="text-sm text-green-600">每 25 分鐘休息 5 分鐘</p>
            </div>
            <div className="text-2xl">🌿</div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default Dashboard