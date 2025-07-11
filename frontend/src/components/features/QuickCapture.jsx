import React, { useState } from 'react'
import { SendIcon } from 'lucide-react'
import { toast } from 'react-hot-toast'

const QuickCapture = () => {
  const [input, setInput] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    if (!input.trim()) return

    setIsSubmitting(true)
    
    try {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 500))
      
      toast.success('已成功捕捉您的想法！')
      setInput('')
    } catch (error) {
      toast.error('捕捉失敗，請再試一次')
    } finally {
      setIsSubmitting(false)
    }
  }

  const quickActions = [
    { label: '📋 新增任務', action: () => setInput('任務: ') },
    { label: '💡 記錄想法', action: () => setInput('想法: ') },
    { label: '⚡ 緊急事項', action: () => setInput('緊急: ') },
    { label: '📅 稍後處理', action: () => setInput('稍後: ') }
  ]

  return (
    <div className="space-y-4">
      {/* Input Form */}
      <form onSubmit={handleSubmit} className="space-y-3">
        <textarea
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="快速記錄任何想法、任務或提醒事項..."
          className="w-full p-3 border border-gray-300 rounded-lg resize-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
          rows={3}
        />
        <div className="flex justify-between items-center">
          <span className="text-sm text-gray-500">
            {input.length}/500 字符
          </span>
          <button
            type="submit"
            disabled={!input.trim() || isSubmitting}
            className="flex items-center space-x-2 px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isSubmitting ? (
              <span className="text-sm">處理中...</span>
            ) : (
              <>
                <SendIcon className="h-4 w-4" />
                <span className="text-sm">捕捉</span>
              </>
            )}
          </button>
        </div>
      </form>

      {/* Quick Actions */}
      <div className="border-t pt-4">
        <p className="text-sm text-gray-600 mb-2">快速操作:</p>
        <div className="grid grid-cols-2 gap-2">
          {quickActions.map((action, index) => (
            <button
              key={index}
              onClick={action.action}
              className="text-left p-2 text-sm border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
            >
              {action.label}
            </button>
          ))}
        </div>
      </div>

      {/* Recent Captures */}
      <div className="border-t pt-4">
        <p className="text-sm text-gray-600 mb-2">最近捕捉:</p>
        <div className="space-y-2">
          <div className="p-2 bg-gray-50 rounded text-sm">
            💡 研究 ADHD 專注技巧
          </div>
          <div className="p-2 bg-gray-50 rounded text-sm">
            📋 完成專案報告
          </div>
          <div className="p-2 bg-gray-50 rounded text-sm">
            ⚡ 回覆重要郵件
          </div>
        </div>
      </div>
    </div>
  )
}

export default QuickCapture