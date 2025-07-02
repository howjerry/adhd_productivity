import React, { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { toast } from 'react-hot-toast'
import { useAuthStore } from '../../stores/useAuthStore'

const LoginForm = () => {
  const navigate = useNavigate()
  const { login, isLoading } = useAuthStore()
  const [formData, setFormData] = useState({
    email: '',
    password: ''
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
    
    if (!formData.email || !formData.password) {
      toast.error('請填寫所有欄位')
      return
    }

    try {
      await login(formData.email, formData.password)
      toast.success('登入成功！')
      navigate('/dashboard')
    } catch (error) {
      toast.error(error.message || '登入失敗')
    }
  }

  // 快速填入測試帳號
  const fillTestAccount = (type) => {
    const accounts = {
      demo: { email: 'demo@adhd.dev', password: 'demo123' },
      test: { email: 'test@adhd.dev', password: 'test123' },
      admin: { email: 'admin@adhd.dev', password: 'admin123' }
    }
    setFormData(accounts[type])
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100 px-4">
      <div className="max-w-md w-full space-y-8">
        {/* Header */}
        <div className="text-center">
          <h1 className="text-4xl font-bold text-gray-900 mb-2">🧠</h1>
          <h2 className="text-3xl font-extrabold text-gray-900">
            ADHD 生產力系統
          </h2>
          <p className="mt-2 text-sm text-gray-600">
            專為 ADHD 使用者設計的生產力管理工具
          </p>
        </div>

        {/* Login Form */}
        <div className="bg-white shadow-xl rounded-lg p-8">
          <form className="space-y-6" onSubmit={handleSubmit}>
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
              <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-2">
                密碼
              </label>
              <input
                id="password"
                name="password"
                type="password"
                autoComplete="current-password"
                required
                value={formData.password}
                onChange={handleInputChange}
                className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                placeholder="請輸入您的密碼"
              />
            </div>

            <div>
              <button
                type="submit"
                disabled={isLoading}
                className="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isLoading ? '登入中...' : '登入'}
              </button>
            </div>
          </form>

          {/* Test Accounts */}
          <div className="mt-6 border-t pt-6">
            <p className="text-center text-sm text-gray-600 mb-4">測試帳號快速登入</p>
            <div className="space-y-2">
              <button
                type="button"
                onClick={() => fillTestAccount('demo')}
                className="w-full px-3 py-2 text-sm border border-gray-300 rounded-md hover:bg-gray-50 text-left"
              >
                <span className="font-medium">Demo 帳號</span> - demo@adhd.dev (混合型 ADHD)
              </button>
              <button
                type="button"
                onClick={() => fillTestAccount('test')}
                className="w-full px-3 py-2 text-sm border border-gray-300 rounded-md hover:bg-gray-50 text-left"
              >
                <span className="font-medium">Test 帳號</span> - test@adhd.dev (注意力不足型)
              </button>
              <button
                type="button"
                onClick={() => fillTestAccount('admin')}
                className="w-full px-3 py-2 text-sm border border-gray-300 rounded-md hover:bg-gray-50 text-left"
              >
                <span className="font-medium">Admin 帳號</span> - admin@adhd.dev (管理員)
              </button>
            </div>
          </div>

          {/* Register Link */}
          <div className="mt-6 text-center">
            <p className="text-sm text-gray-600">
              還沒有帳號？{' '}
              <Link 
                to="/register" 
                className="font-medium text-indigo-600 hover:text-indigo-500"
              >
                立即註冊
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}

export default LoginForm