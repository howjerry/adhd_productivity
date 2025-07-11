import React from 'react'

const AchievementDisplay = () => {
  const recentAchievements = [
    {
      id: 1,
      name: '專注大師',
      description: '完成 10 個番茄鐘',
      icon: '🍅',
      points: 50,
      unlockedAt: '2 小時前',
      rarity: 'common'
    },
    {
      id: 2,
      name: '早起鳥兒',
      description: '連續 7 天早上 8 點前開始工作',
      icon: '🌅',
      points: 100,
      unlockedAt: '1 天前',
      rarity: 'rare'
    },
    {
      id: 3,
      name: '任務清理專家',
      description: '單日完成 5 個任務',
      icon: '🧹',
      points: 75,
      unlockedAt: '3 天前',
      rarity: 'uncommon'
    }
  ]

  const upcomingAchievements = [
    {
      name: '習慣養成者',
      description: '連續 30 天保持所有習慣',
      icon: '🏆',
      progress: 15,
      target: 30,
      points: 200
    },
    {
      name: '心情記錄家',
      description: '連續記錄心情 14 天',
      icon: '📖',
      progress: 8,
      target: 14,
      points: 150
    },
    {
      name: '效率王',
      description: '單週完成 50 個任務',
      icon: '⚡',
      progress: 32,
      target: 50,
      points: 300
    }
  ]

  const rarityColors = {
    common: 'border-gray-300 bg-gray-50',
    uncommon: 'border-green-300 bg-green-50',
    rare: 'border-blue-300 bg-blue-50',
    epic: 'border-purple-300 bg-purple-50',
    legendary: 'border-yellow-300 bg-yellow-50'
  }

  const rarityTextColors = {
    common: 'text-gray-600',
    uncommon: 'text-green-600',
    rare: 'text-blue-600',
    epic: 'text-purple-600',
    legendary: 'text-yellow-600'
  }

  return (
    <div className="space-y-6">
      {/* Recent Achievements */}
      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-3">🎉 最新解鎖</h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {recentAchievements.map((achievement) => (
            <div
              key={achievement.id}
              className={`p-4 rounded-lg border-2 ${rarityColors[achievement.rarity]} transition-all hover:scale-105`}
            >
              <div className="text-center">
                <div className="text-4xl mb-2">{achievement.icon}</div>
                <h4 className="font-semibold text-gray-900 mb-1">{achievement.name}</h4>
                <p className="text-sm text-gray-600 mb-2">{achievement.description}</p>
                <div className="flex items-center justify-center space-x-2">
                  <span className={`text-sm font-medium ${rarityTextColors[achievement.rarity]}`}>
                    +{achievement.points} 積分
                  </span>
                  <span className="text-xs text-gray-500">{achievement.unlockedAt}</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Progress Towards Next Achievements */}
      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-3">🎯 即將解鎖</h3>
        <div className="space-y-3">
          {upcomingAchievements.map((achievement, index) => (
            <div key={index} className="bg-white border border-gray-200 rounded-lg p-4">
              <div className="flex items-center space-x-4">
                <div className="text-3xl">{achievement.icon}</div>
                <div className="flex-1">
                  <div className="flex items-center justify-between mb-1">
                    <h4 className="font-medium text-gray-900">{achievement.name}</h4>
                    <span className="text-sm text-indigo-600 font-medium">
                      +{achievement.points} 積分
                    </span>
                  </div>
                  <p className="text-sm text-gray-600 mb-2">{achievement.description}</p>
                  
                  {/* Progress Bar */}
                  <div className="space-y-1">
                    <div className="flex justify-between text-sm">
                      <span className="text-gray-600">
                        {achievement.progress}/{achievement.target}
                      </span>
                      <span className="text-gray-600">
                        {Math.round((achievement.progress / achievement.target) * 100)}%
                      </span>
                    </div>
                    <div className="w-full bg-gray-200 rounded-full h-2">
                      <div
                        className="bg-indigo-500 h-2 rounded-full transition-all duration-300"
                        style={{ width: `${(achievement.progress / achievement.target) * 100}%` }}
                      />
                    </div>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Achievement Stats */}
      <div className="bg-gradient-to-r from-indigo-500 to-purple-600 rounded-lg p-4 text-white">
        <h3 className="text-lg font-semibold mb-3">🏆 成就統計</h3>
        <div className="grid grid-cols-3 gap-4 text-center">
          <div>
            <div className="text-2xl font-bold">12</div>
            <div className="text-sm opacity-90">已解鎖</div>
          </div>
          <div>
            <div className="text-2xl font-bold">1,250</div>
            <div className="text-sm opacity-90">總積分</div>
          </div>
          <div>
            <div className="text-2xl font-bold">85%</div>
            <div className="text-sm opacity-90">完成度</div>
          </div>
        </div>
      </div>

      {/* Motivational Quote */}
      <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
        <div className="flex items-center space-x-2">
          <span className="text-2xl">💡</span>
          <p className="text-sm text-yellow-800 italic">
            "每個小成就都是通往更大目標的一步。繼續努力，你正在做得很好！"
          </p>
        </div>
      </div>
    </div>
  )
}

export default AchievementDisplay