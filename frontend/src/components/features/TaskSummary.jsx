import React from 'react'
import { CheckIcon, ClockIcon, AlertTriangleIcon, PlusIcon } from 'lucide-react'
import { Link } from 'react-router-dom'

const TaskSummary = () => {
  // Mock data - replace with actual API data
  const taskStats = {
    total: 12,
    completed: 5,
    inProgress: 3,
    overdue: 2,
    upcoming: 2
  }

  const recentTasks = [
    { id: 1, title: '完成專案提案', priority: 'high', status: 'completed', energy: 'high' },
    { id: 2, title: '回覆客戶郵件', priority: 'medium', status: 'inProgress', energy: 'medium' },
    { id: 3, title: '準備會議資料', priority: 'high', status: 'overdue', energy: 'high' },
    { id: 4, title: '整理桌面', priority: 'low', status: 'todo', energy: 'low' }
  ]

  const priorityColors = {
    high: 'text-red-600 bg-red-100',
    medium: 'text-yellow-600 bg-yellow-100',
    low: 'text-green-600 bg-green-100'
  }

  const statusIcons = {
    completed: <CheckIcon className="h-4 w-4 text-green-600" />,
    inProgress: <ClockIcon className="h-4 w-4 text-blue-600" />,
    overdue: <AlertTriangleIcon className="h-4 w-4 text-red-600" />,
    todo: <PlusIcon className="h-4 w-4 text-gray-400" />
  }

  const energyLabels = {
    high: '⚡ 高能量',
    medium: '💡 中等',
    low: '🌱 低能量'
  }

  return (
    <div className="space-y-4">
      {/* Stats Grid */}
      <div className="grid grid-cols-2 gap-3">
        <div className="bg-green-50 p-3 rounded-lg">
          <div className="text-2xl font-bold text-green-600">{taskStats.completed}</div>
          <div className="text-sm text-green-600">已完成</div>
        </div>
        <div className="bg-blue-50 p-3 rounded-lg">
          <div className="text-2xl font-bold text-blue-600">{taskStats.inProgress}</div>
          <div className="text-sm text-blue-600">進行中</div>
        </div>
        <div className="bg-red-50 p-3 rounded-lg">
          <div className="text-2xl font-bold text-red-600">{taskStats.overdue}</div>
          <div className="text-sm text-red-600">已逾期</div>
        </div>
        <div className="bg-gray-50 p-3 rounded-lg">
          <div className="text-2xl font-bold text-gray-600">{taskStats.upcoming}</div>
          <div className="text-sm text-gray-600">即將到期</div>
        </div>
      </div>

      {/* Progress Bar */}
      <div className="space-y-2">
        <div className="flex justify-between text-sm">
          <span>今日進度</span>
          <span>{Math.round((taskStats.completed / taskStats.total) * 100)}%</span>
        </div>
        <div className="w-full bg-gray-200 rounded-full h-2">
          <div
            className="bg-green-500 h-2 rounded-full transition-all duration-300"
            style={{ width: `${(taskStats.completed / taskStats.total) * 100}%` }}
          />
        </div>
      </div>

      {/* Recent Tasks */}
      <div className="space-y-2">
        <h4 className="text-sm font-medium text-gray-700">最近任務</h4>
        <div className="space-y-1">
          {recentTasks.map((task) => (
            <div key={task.id} className="flex items-center space-x-2 p-2 hover:bg-gray-50 rounded">
              {statusIcons[task.status]}
              <div className="flex-1 min-w-0">
                <p className={`text-sm truncate ${
                  task.status === 'completed' ? 'line-through text-gray-500' : 'text-gray-900'
                }`}>
                  {task.title}
                </p>
                <div className="flex items-center space-x-2 mt-1">
                  <span className={`text-xs px-2 py-0.5 rounded-full ${priorityColors[task.priority]}`}>
                    {task.priority === 'high' ? '高' : task.priority === 'medium' ? '中' : '低'}
                  </span>
                  <span className="text-xs text-gray-500">
                    {energyLabels[task.energy]}
                  </span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Action Button */}
      <Link
        to="/tasks"
        className="block w-full text-center py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors text-sm"
      >
        查看所有任務
      </Link>
    </div>
  )
}

export default TaskSummary