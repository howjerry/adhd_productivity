import React from 'react'
import { CheckIcon, XIcon } from 'lucide-react'

const HabitProgress = () => {
  const habits = [
    {
      id: 1,
      name: '晨間運動',
      target: 1,
      completed: 1,
      streak: 5,
      lastWeek: [true, true, false, true, true, true, true]
    },
    {
      id: 2,
      name: '冥想練習',
      target: 1,
      completed: 0,
      streak: 3,
      lastWeek: [true, true, true, false, false, true, false]
    },
    {
      id: 3,
      name: '喝水 8 杯',
      target: 8,
      completed: 5,
      streak: 2,
      lastWeek: [true, false, true, true, true, false, true]
    },
    {
      id: 4,
      name: '閱讀 30 分鐘',
      target: 1,
      completed: 1,
      streak: 7,
      lastWeek: [true, true, true, true, true, true, true]
    }
  ]

  const getProgressPercentage = (completed, target) => {
    return Math.min((completed / target) * 100, 100)
  }

  const getStreakEmoji = (streak) => {
    if (streak >= 7) return '🔥'
    if (streak >= 3) return '⭐'
    if (streak >= 1) return '✨'
    return '💤'
  }

  return (
    <div className="space-y-4">
      {/* Today's Habits */}
      <div className="space-y-3">
        {habits.map((habit) => (
          <div key={habit.id} className="border border-gray-200 rounded-lg p-3">
            <div className="flex items-center justify-between mb-2">
              <h4 className="text-sm font-medium text-gray-900">{habit.name}</h4>
              <div className="flex items-center space-x-1">
                <span className="text-lg">{getStreakEmoji(habit.streak)}</span>
                <span className="text-xs text-gray-500">{habit.streak}天</span>
              </div>
            </div>
            
            {/* Progress */}
            <div className="space-y-2">
              <div className="flex items-center justify-between text-sm">
                <span className="text-gray-600">
                  {habit.completed}/{habit.target} {habit.target > 1 ? '次' : ''}
                </span>
                <span className="text-gray-600">
                  {Math.round(getProgressPercentage(habit.completed, habit.target))}%
                </span>
              </div>
              
              <div className="w-full bg-gray-200 rounded-full h-2">
                <div
                  className={`h-2 rounded-full transition-all duration-300 ${
                    habit.completed >= habit.target ? 'bg-green-500' : 'bg-blue-500'
                  }`}
                  style={{ width: `${getProgressPercentage(habit.completed, habit.target)}%` }}
                />
              </div>

              {/* Weekly Progress */}
              <div className="flex items-center justify-between">
                <span className="text-xs text-gray-500">過去 7 天</span>
                <div className="flex space-x-1">
                  {habit.lastWeek.map((completed, index) => (
                    <div
                      key={index}
                      className={`w-3 h-3 rounded-full ${
                        completed ? 'bg-green-400' : 'bg-gray-200'
                      }`}
                      title={`第 ${index + 1} 天`}
                    />
                  ))}
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Quick Stats */}
      <div className="bg-gray-50 rounded-lg p-3">
        <h4 className="text-sm font-medium text-gray-700 mb-2">今日統計</h4>
        <div className="grid grid-cols-2 gap-4 text-center">
          <div>
            <div className="text-2xl font-bold text-green-600">
              {habits.filter(h => h.completed >= h.target).length}
            </div>
            <div className="text-xs text-gray-600">已完成</div>
          </div>
          <div>
            <div className="text-2xl font-bold text-blue-600">
              {Math.round(
                habits.reduce((acc, h) => acc + getProgressPercentage(h.completed, h.target), 0) / habits.length
              )}%
            </div>
            <div className="text-xs text-gray-600">整體進度</div>
          </div>
        </div>
      </div>

      {/* Motivational Message */}
      <div className="bg-indigo-50 p-3 rounded-lg border border-indigo-200">
        <p className="text-sm text-indigo-800 text-center">
          💪 持續的小步伐會帶來巨大的改變！
        </p>
      </div>
    </div>
  )
}

export default HabitProgress