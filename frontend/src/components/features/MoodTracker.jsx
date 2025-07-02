import React, { useState } from 'react'
import { toast } from 'react-hot-toast'

const MoodTracker = () => {
  const [selectedMood, setSelectedMood] = useState(null)
  const [selectedEnergy, setSelectedEnergy] = useState(null)
  const [selectedFocus, setSelectedFocus] = useState(null)

  const moodOptions = [
    { value: 1, emoji: '😢', label: '很低落' },
    { value: 2, emoji: '😟', label: '低落' },
    { value: 3, emoji: '😐', label: '一般' },
    { value: 4, emoji: '😊', label: '不錯' },
    { value: 5, emoji: '😄', label: '很好' }
  ]

  const energyOptions = [
    { value: 1, emoji: '🔋', label: '很疲倦', color: 'text-red-500' },
    { value: 2, emoji: '🔋', label: '疲倦', color: 'text-orange-500' },
    { value: 3, emoji: '🔋', label: '普通', color: 'text-yellow-500' },
    { value: 4, emoji: '🔋', label: '有活力', color: 'text-green-500' },
    { value: 5, emoji: '⚡', label: '充滿活力', color: 'text-green-600' }
  ]

  const focusOptions = [
    { value: 1, emoji: '😵', label: '無法專注' },
    { value: 2, emoji: '😮‍💨', label: '難以專注' },
    { value: 3, emoji: '😐', label: '普通' },
    { value: 4, emoji: '🎯', label: '能夠專注' },
    { value: 5, emoji: '🔥', label: '高度專注' }
  ]

  const handleSubmit = async () => {
    if (!selectedMood || !selectedEnergy || !selectedFocus) {
      toast.error('請填寫所有項目')
      return
    }

    try {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 500))
      toast.success('心情記錄已保存！')
      
      // Reset form
      setSelectedMood(null)
      setSelectedEnergy(null)
      setSelectedFocus(null)
    } catch (error) {
      toast.error('保存失敗，請重試')
    }
  }

  const recentEntries = [
    { date: '今天 14:30', mood: 4, energy: 3, focus: 4 },
    { date: '今天 10:15', mood: 3, energy: 4, focus: 3 },
    { date: '昨天 16:45', mood: 5, energy: 5, focus: 5 },
  ]

  return (
    <div className="space-y-4">
      {/* Quick Mood Log */}
      <div className="space-y-3">
        <h4 className="text-sm font-medium text-gray-700">現在的狀態</h4>
        
        {/* Mood */}
        <div>
          <label className="block text-xs text-gray-600 mb-2">心情</label>
          <div className="flex space-x-2">
            {moodOptions.map((option) => (
              <button
                key={option.value}
                onClick={() => setSelectedMood(option.value)}
                className={`p-2 rounded-lg text-xl transition-all ${
                  selectedMood === option.value
                    ? 'bg-blue-100 ring-2 ring-blue-500'
                    : 'hover:bg-gray-100'
                }`}
                title={option.label}
              >
                {option.emoji}
              </button>
            ))}
          </div>
        </div>

        {/* Energy */}
        <div>
          <label className="block text-xs text-gray-600 mb-2">能量</label>
          <div className="flex space-x-2">
            {energyOptions.map((option) => (
              <button
                key={option.value}
                onClick={() => setSelectedEnergy(option.value)}
                className={`p-2 rounded-lg text-xl transition-all ${
                  selectedEnergy === option.value
                    ? 'bg-green-100 ring-2 ring-green-500'
                    : 'hover:bg-gray-100'
                }`}
                title={option.label}
              >
                <span className={option.color}>{option.emoji}</span>
              </button>
            ))}
          </div>
        </div>

        {/* Focus */}
        <div>
          <label className="block text-xs text-gray-600 mb-2">專注力</label>
          <div className="flex space-x-2">
            {focusOptions.map((option) => (
              <button
                key={option.value}
                onClick={() => setSelectedFocus(option.value)}
                className={`p-2 rounded-lg text-xl transition-all ${
                  selectedFocus === option.value
                    ? 'bg-purple-100 ring-2 ring-purple-500'
                    : 'hover:bg-gray-100'
                }`}
                title={option.label}
              >
                {option.emoji}
              </button>
            ))}
          </div>
        </div>

        {/* Submit Button */}
        <button
          onClick={handleSubmit}
          disabled={!selectedMood || !selectedEnergy || !selectedFocus}
          className="w-full py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed text-sm"
        >
          記錄狀態
        </button>
      </div>

      {/* Recent Entries */}
      <div className="border-t pt-4">
        <h4 className="text-sm font-medium text-gray-700 mb-2">最近記錄</h4>
        <div className="space-y-2">
          {recentEntries.map((entry, index) => (
            <div key={index} className="flex items-center justify-between p-2 bg-gray-50 rounded">
              <span className="text-xs text-gray-500">{entry.date}</span>
              <div className="flex items-center space-x-1">
                <span title="心情">{moodOptions.find(m => m.value === entry.mood)?.emoji}</span>
                <span title="能量" className="text-green-500">
                  {energyOptions.find(e => e.value === entry.energy)?.emoji}
                </span>
                <span title="專注力">{focusOptions.find(f => f.value === entry.focus)?.emoji}</span>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Today's Average */}
      <div className="bg-indigo-50 p-3 rounded-lg">
        <h4 className="text-sm font-medium text-indigo-800 mb-1">今日平均</h4>
        <div className="flex items-center justify-between text-sm">
          <span className="text-indigo-600">心情 😊 4/5</span>
          <span className="text-indigo-600">能量 🔋 3.5/5</span>
          <span className="text-indigo-600">專注 🎯 3.5/5</span>
        </div>
      </div>
    </div>
  )
}

export default MoodTracker