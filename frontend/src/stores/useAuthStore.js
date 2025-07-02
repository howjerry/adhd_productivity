import { create } from 'zustand'

const API_BASE_URL = 'http://localhost:5001/api'

const useAuthStore = create((set, get) => ({
      // State
      user: null,
      token: null,
      isLoading: false,
      isAuthenticated: false,

      // Actions
      login: async (email, password) => {
        set({ isLoading: true })
        
        try {
          const response = await fetch(`${API_BASE_URL}/auth/login`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify({ email, password }),
          })

          const data = await response.json()

          if (!response.ok) {
            throw new Error(data.message || '登入失敗')
          }

          if (data.success) {
            set({
              user: data.user,
              token: data.token,
              isAuthenticated: true,
              isLoading: false,
            })
          } else {
            throw new Error(data.message || '登入失敗')
          }
        } catch (error) {
          set({ isLoading: false })
          throw error
        }
      },

      register: async (email, password, fullName, adhdType) => {
        set({ isLoading: true })
        
        try {
          const response = await fetch(`${API_BASE_URL}/auth/register`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify({ 
              email, 
              password, 
              fullName, 
              adhdType: parseInt(adhdType) 
            }),
          })

          const data = await response.json()

          if (!response.ok) {
            throw new Error(data.message || '註冊失敗')
          }

          if (data.success) {
            set({
              user: data.user,
              token: data.token,
              isAuthenticated: true,
              isLoading: false,
            })
          } else {
            throw new Error(data.message || '註冊失敗')
          }
        } catch (error) {
          set({ isLoading: false })
          throw error
        }
      },

      logout: () => {
        set({
          user: null,
          token: null,
          isAuthenticated: false,
          isLoading: false,
        })
      },

      // Get auth headers for API calls
      getAuthHeaders: () => {
        const { token } = get()
        return token ? { Authorization: `Bearer ${token}` } : {}
      },

      // Check if token is still valid
      checkAuthStatus: async () => {
        const { token } = get()
        if (!token) return false

        try {
          const response = await fetch(`${API_BASE_URL}/auth/me`, {
            headers: {
              'Authorization': `Bearer ${token}`,
            },
          })

          if (response.ok) {
            const userData = await response.json()
            set({ user: userData, isAuthenticated: true })
            return true
          } else {
            // Token invalid, logout
            get().logout()
            return false
          }
        } catch (error) {
          get().logout()
          return false
        }
      },
    }))

export { useAuthStore }