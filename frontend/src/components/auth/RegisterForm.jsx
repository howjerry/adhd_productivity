import React, { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { toast } from 'react-hot-toast'
import { useAuthStore } from '../../stores/useAuthStore'

const RegisterForm = () => {
  const navigate = useNavigate()
  const { register, isLoading } = useAuthStore()
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    fullName: '',
    adhdType: '1'
  })

  const handleInputChange = (e) => {
    const { name, value } = e.target
    setFormData(prev => ({
      ...prev,
      [name]: value
    }))
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    
    if (!formData.email || !formData.password || !formData.fullName) {
      toast.error('請填寫所有欄位')
      return
    }

    if (formData.password !== formData.confirmPassword) {
      toast.error('密碼確認不一致')
      return
    }

    if (formData.password.length < 6) {
      toast.error('密碼長度至少需要6個字符')
      return
    }

    try {
      await register(formData.email, formData.password, formData.fullName, formData.adhdType)
      toast.success('註冊成功！歡迎加入 ADHD 生產力系統')
      navigate('/dashboard')
    } catch (error) {
      toast.error(error.message || '註冊失敗')
    }
  }

  const adhdTypes = [
    { value: '1', label: '混合型 (Combined)', description: '注意力不集中 + 過動衝動' },
    { value: '2', label: '注意力不足型 (Inattentive)', description: '主要為注意力不集中' },
    { value: '3', label: '過動衝動型 (Hyperactive)', description: '主要為過動衝動' }
  ]

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100 px-4">
      <div className="max-w-md w-full space-y-8">
        {/* Header */}
        <div className="text-center">
          <h1 className="text-4xl font-bold text-gray-900 mb-2">🧠</h1>
          <h2 className="text-3xl font-extrabold text-gray-900">
            加入 ADHD 生產力系統
          </h2>
          <p className="mt-2 text-sm text-gray-600">
            開始您的個人化生產力管理之旅
          </p>
        </div>

        {/* Register Form */}
        <div className="bg-white shadow-xl rounded-lg p-8">
          <form className="space-y-6" onSubmit={handleSubmit}>
            <div>
              <label htmlFor="fullName" className="block text-sm font-medium text-gray-700 mb-2">
                全名
              </label>
              <input
                id="fullName"
                name="fullName"
                type="text"
                required
                value={formData.fullName}
                onChange={handleInputChange}
                className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                placeholder="請輸入您的全名"
              />
            </div>

            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-2">
                電子郵件
              </label>
              <input
                id="email"
                name="email"
                type="email"
                autoComplete="email"
                required
                value={formData.email}
                onChange={handleInputChange}
                className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                placeholder="請輸入您的電子郵件"
              />
            </div>

            <div>
              <label htmlFor="adhdType" className="block text-sm font-medium text-gray-700 mb-2">
                ADHD 類型
              </label>
              <select
                id="adhdType"
                name="adhdType"
                required
                value={formData.adhdType}
                onChange={handleInputChange}
                className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
              >
                {adhdTypes.map(type => (
                  <option key={type.value} value={type.value}>
                    {type.label}
                  </option>
                ))}
              </select>
              <p className="mt-1 text-xs text-gray-500">
                {adhdTypes.find(type => type.value === formData.adhdType)?.description}
              </p>
            </div>

            <div>
              <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-2">
                密碼
              </label>
              <input
                id="password"
                name="password"
                type="password"
                autoComplete="new-password"
                required
                value={formData.password}
                onChange={handleInputChange}
                className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                placeholder="請輸入密碼 (至少6個字符)"
              />
            </div>

            <div>
              <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700 mb-2">
                確認密碼
              </label>
              <input
                id="confirmPassword"
                name="confirmPassword"
                type="password"
                autoComplete="new-password"
                required
                value={formData.confirmPassword}
                onChange={handleInputChange}
                className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                placeholder="請再次輸入密碼"
              />
            </div>

            <div>
              <button
                type="submit"
                disabled={isLoading}
                className="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isLoading ? '註冊中...' : '註冊'}
              </button>
            </div>
          </form>

          {/* Login Link */}
          <div className="mt-6 text-center">
            <p className="text-sm text-gray-600">
              已經有帳號了？{' '}
              <Link 
                to="/login" 
                className="font-medium text-indigo-600 hover:text-indigo-500"
              >
                立即登入
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}

export default RegisterForm