import React, { useState, useEffect } from 'react'
import { PlayIcon, PauseIcon, RotateCcwIcon, SettingsIcon } from 'lucide-react'

const VisualTimer = () => {
  const [timeLeft, setTimeLeft] = useState(25 * 60) // 25 minutes in seconds
  const [isRunning, setIsRunning] = useState(false)
  const [mode, setMode] = useState('work') // 'work', 'shortBreak', 'longBreak'
  const [sessionCount, setSessionCount] = useState(0)

  const modes = {
    work: { duration: 25 * 60, label: '專注時間', color: 'text-red-600', bg: 'bg-red-100' },
    shortBreak: { duration: 5 * 60, label: '短休息', color: 'text-green-600', bg: 'bg-green-100' },
    longBreak: { duration: 15 * 60, label: '長休息', color: 'text-blue-600', bg: 'bg-blue-100' }
  }

  useEffect(() => {
    let interval = null
    if (isRunning && timeLeft > 0) {
      interval = setInterval(() => {
        setTimeLeft(timeLeft => timeLeft - 1)
      }, 1000)
    } else if (timeLeft === 0) {
      setIsRunning(false)
      handleSessionComplete()
    }
    return () => clearInterval(interval)
  }, [isRunning, timeLeft])

  const handleSessionComplete = () => {
    if (mode === 'work') {
      setSessionCount(prev => prev + 1)
      const nextMode = sessionCount % 4 === 3 ? 'longBreak' : 'shortBreak'
      setMode(nextMode)
      setTimeLeft(modes[nextMode].duration)
    } else {
      setMode('work')
      setTimeLeft(modes.work.duration)
    }
  }

  const toggleTimer = () => {
    setIsRunning(!isRunning)
  }

  const resetTimer = () => {
    setIsRunning(false)
    setTimeLeft(modes[mode].duration)
  }

  const switchMode = (newMode) => {
    setIsRunning(false)
    setMode(newMode)
    setTimeLeft(modes[newMode].duration)
  }

  const formatTime = (seconds) => {
    const mins = Math.floor(seconds / 60)
    const secs = seconds % 60
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`
  }

  const progress = ((modes[mode].duration - timeLeft) / modes[mode].duration) * 100

  return (
    <div className="text-center space-y-4">
      {/* Mode Tabs */}
      <div className="flex space-x-1 bg-gray-100 rounded-lg p-1">
        {Object.entries(modes).map(([key, modeInfo]) => (
          <button
            key={key}
            onClick={() => switchMode(key)}
            className={`flex-1 py-2 px-3 rounded-md text-sm font-medium transition-colors ${
              mode === key
                ? `${modeInfo.bg} ${modeInfo.color}`
                : 'text-gray-600 hover:text-gray-900'
            }`}
          >
            {modeInfo.label}
          </button>
        ))}
      </div>

      {/* Timer Display */}
      <div className="relative">
        {/* Circular Progress */}
        <div className="relative w-48 h-48 mx-auto">
          <svg className="w-full h-full transform -rotate-90" viewBox="0 0 100 100">
            {/* Background circle */}
            <circle
              cx="50"
              cy="50"
              r="45"
              fill="none"
              stroke="#e5e7eb"
              strokeWidth="8"
            />
            {/* Progress circle */}
            <circle
              cx="50"
              cy="50"
              r="45"
              fill="none"
              stroke={mode === 'work' ? '#dc2626' : mode === 'shortBreak' ? '#16a34a' : '#2563eb'}
              strokeWidth="8"
              strokeLinecap="round"
              strokeDasharray={`${2 * Math.PI * 45}`}
              strokeDashoffset={`${2 * Math.PI * 45 * (1 - progress / 100)}`}
              className="transition-all duration-1000 ease-in-out"
            />
          </svg>
          
          {/* Time Display */}
          <div className="absolute inset-0 flex items-center justify-center">
            <div>
              <div className={`text-4xl font-bold ${modes[mode].color}`}>
                {formatTime(timeLeft)}
              </div>
              <div className="text-sm text-gray-500 mt-1">
                {modes[mode].label}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Controls */}
      <div className="flex items-center justify-center space-x-4">
        <button
          onClick={toggleTimer}
          className={`flex items-center justify-center w-12 h-12 rounded-full ${
            isRunning ? 'bg-red-100 text-red-600' : 'bg-green-100 text-green-600'
          } hover:opacity-80 transition-opacity`}
        >
          {isRunning ? <PauseIcon className="h-6 w-6" /> : <PlayIcon className="h-6 w-6 ml-1" />}
        </button>
        
        <button
          onClick={resetTimer}
          className="flex items-center justify-center w-12 h-12 rounded-full bg-gray-100 text-gray-600 hover:opacity-80 transition-opacity"
        >
          <RotateCcwIcon className="h-5 w-5" />
        </button>
      </div>

      {/* Session Counter */}
      <div className="flex items-center justify-center space-x-4 text-sm text-gray-600">
        <span>今日完成: {sessionCount} 個番茄鐘</span>
        <div className="flex space-x-1">
          {[...Array(4)].map((_, i) => (
            <div
              key={i}
              className={`w-3 h-3 rounded-full ${
                i < sessionCount % 4 ? 'bg-red-400' : 'bg-gray-200'
              }`}
            />
          ))}
        </div>
      </div>

      {/* Tips */}
      <div className={`p-3 rounded-lg ${modes[mode].bg} ${modes[mode].color}`}>
        <p className="text-sm">
          {mode === 'work' 
            ? '💡 專注於當前任務，避免多工處理' 
            : mode === 'shortBreak'
            ? '🌿 短暫休息，起身活動一下'
            : '🚶‍♂️ 長休息時間，散個步或做些輕鬆的事'
          }
        </p>
      </div>
    </div>
  )
}

export default VisualTimer