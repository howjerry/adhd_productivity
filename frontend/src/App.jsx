import React, { useEffect } from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { Toaster } from 'react-hot-toast'
import { useAuthStore } from './stores/useAuthStore'

// Import components
import Layout from './components/layout/Layout'
import Dashboard from './components/dashboard/Dashboard'
import LoginForm from './components/auth/LoginForm'
import RegisterForm from './components/auth/RegisterForm'

// Placeholder components for incomplete features
const PlaceholderPage = ({ title, description }) => (
  <div className="bg-white rounded-lg shadow-md p-8 text-center">
    <h1 className="text-3xl font-bold text-gray-900 mb-4">{title}</h1>
    <p className="text-gray-600 mb-6">{description}</p>
    <div className="text-6xl mb-4">🚧</div>
    <p className="text-sm text-gray-500">此功能正在開發中，敬請期待！</p>
  </div>
)

function App() {
  const { isAuthenticated, checkAuthStatus } = useAuthStore()

  useEffect(() => {
    // Check if user is still authenticated on app load
    checkAuthStatus()
  }, [checkAuthStatus])

  return (
    <div className="App">
      <Toaster 
        position="top-right"
        toastOptions={{
          duration: 4000,
          style: {
            background: '#363636',
            color: '#fff',
          },
        }}
      />
      
      <Routes>
        {/* Auth Routes */}
        <Route 
          path="/login" 
          element={isAuthenticated ? <Navigate to="/dashboard" /> : <LoginForm />} 
        />
        <Route 
          path="/register" 
          element={isAuthenticated ? <Navigate to="/dashboard" /> : <RegisterForm />} 
        />
        
        {/* Protected Routes */}
        <Route 
          path="/" 
          element={isAuthenticated ? <Layout /> : <Navigate to="/login" />}
        >
          <Route index element={<Navigate to="/dashboard" />} />
          <Route path="dashboard" element={<Dashboard />} />
          <Route 
            path="tasks" 
            element={
              <PlaceholderPage 
                title="任務管理" 
                description="智能任務管理系統，支援優先級排序和能量分配"
              />
            } 
          />
          <Route 
            path="pomodoro" 
            element={
              <PlaceholderPage 
                title="番茄計時器" 
                description="專為 ADHD 設計的專注時間管理工具"
              />
            } 
          />
          <Route 
            path="habits" 
            element={
              <PlaceholderPage 
                title="習慣追蹤" 
                description="建立和維持良好習慣的追蹤系統"
              />
            } 
          />
          <Route 
            path="mood" 
            element={
              <PlaceholderPage 
                title="情緒記錄" 
                description="追蹤心情、能量和專注力的變化"
              />
            } 
          />
          <Route 
            path="body-doubling" 
            element={
              <PlaceholderPage 
                title="Body Doubling" 
                description="與他人一起專注工作的虛擬共同空間"
              />
            } 
          />
          <Route 
            path="achievements" 
            element={
              <PlaceholderPage 
                title="成就系統" 
                description="遊戲化的成就和獎勵系統"
              />
            } 
          />
          <Route 
            path="analytics" 
            element={
              <PlaceholderPage 
                title="數據分析" 
                description="深入分析您的生產力模式和趨勢"
              />
            } 
          />
          <Route 
            path="settings" 
            element={
              <PlaceholderPage 
                title="設定" 
                description="個人化您的 ADHD 生產力系統"
              />
            } 
          />
        </Route>
        
        {/* 404 Route */}
        <Route 
          path="*" 
          element={
            <div className="min-h-screen flex items-center justify-center bg-gray-50">
              <div className="text-center">
                <h1 className="text-4xl font-bold text-gray-900">404</h1>
                <p className="text-gray-600 mt-2">找不到頁面</p>
                <button 
                  onClick={() => window.history.back()}
                  className="mt-4 px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700"
                >
                  返回上一頁
                </button>
              </div>
            </div>
          } 
        />
      </Routes>
    </div>
  )
}

export default App