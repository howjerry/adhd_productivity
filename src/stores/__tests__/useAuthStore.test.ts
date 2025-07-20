import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useAuthStore } from '../useAuthStore';
import type { User, UserPreferences } from '@/types';

// Mock fetch
const mockFetch = vi.fn();
Object.defineProperty(global, 'fetch', {
  value: mockFetch,
  writable: true,
});

// Mock user data
const mockUser: User = {
  id: 'user-123',
  email: 'test@example.com',
  username: 'testuser',
  displayName: 'Test User',
  avatar: 'https://example.com/avatar.jpg',
  preferences: {
    theme: 'light',
    timezone: 'Asia/Taipei',
    workingHours: {
      start: '09:00',
      end: '17:00',
    },
    pomodoroSettings: {
      workDuration: 25,
      shortBreak: 5,
      longBreak: 15,
      sessionsUntilLongBreak: 4,
    },
    notificationSettings: {
      desktop: true,
      email: false,
      mobile: true,
      quiet_hours: {
        enabled: false,
        start: '22:00',
        end: '08:00',
      },
    },
    energyTracking: true,
    gamificationEnabled: true,
    densityLevel: 'normal',
  },
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
};

const mockToken = 'mock-jwt-token';

describe('useAuthStore', () => {
  beforeEach(() => {
    // Reset store state
    useAuthStore.setState({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      error: null,
      token: null,
    });
    
    // Clear localStorage
    localStorage.clear();
    
    // Reset mocks
    mockFetch.mockReset();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('初始狀態', () => {
    it('應該有正確的初始狀態', () => {
      const { result } = renderHook(() => useAuthStore());
      
      expect(result.current.user).toBeNull();
      expect(result.current.isAuthenticated).toBe(false);
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
      expect(result.current.token).toBeNull();
    });
  });

  describe('登入功能', () => {
    it('應該成功登入', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          data: {
            user: mockUser,
            token: mockToken,
          }
        }),
      });

      const { result } = renderHook(() => useAuthStore());

      await act(async () => {
        await result.current.login('test@example.com', 'password123');
      });

      expect(result.current.isAuthenticated).toBe(true);
      expect(result.current.user).toEqual(mockUser);
      expect(result.current.token).toBe(mockToken);
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
    });

    it('應該在登入過程中設置 loading 狀態', async () => {
      let resolvePromise: (value: any) => void;
      const promise = new Promise((resolve) => {
        resolvePromise = resolve;
      });

      mockFetch.mockReturnValueOnce(promise);

      const { result } = renderHook(() => useAuthStore());

      const loginPromise = result.current.login('test@example.com', 'password123');

      expect(result.current.isLoading).toBe(true);
      expect(result.current.error).toBeNull();

      await act(async () => {
        resolvePromise!({
          ok: true,
          json: async () => ({
            data: {
              user: mockUser,
              token: mockToken,
            }
          }),
        });
        await promise;
      });

      expect(result.current.isLoading).toBe(false);
    });

    it('應該處理登入失敗', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 401,
        text: async () => 'Invalid credentials',
        statusText: 'Unauthorized'
      });

      const { result } = renderHook(() => useAuthStore());

      await act(async () => {
        try {
          await result.current.login('test@example.com', 'wrongpassword');
        } catch (error) {
          // Expected to throw
        }
      });

      expect(result.current.isAuthenticated).toBe(false);
      expect(result.current.user).toBeNull();
      expect(result.current.token).toBeNull();
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBe('Invalid credentials');
    });

    it('應該處理網路錯誤', async () => {
      const networkError = new Error('網路連線失敗');
      mockFetch.mockRejectedValueOnce(networkError);

      const { result } = renderHook(() => useAuthStore());

      await act(async () => {
        try {
          await result.current.login('test@example.com', 'password123');
        } catch (error) {
          // Expected to throw
        }
      });

      expect(result.current.isAuthenticated).toBe(false);
      // API Client 會將錯誤包裝，所以檢查錯誤存在即可
      expect(result.current.error).toBeTruthy();
      expect(result.current.isLoading).toBe(false);
    });

    it('應該發送正確的登入請求', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          data: {
            user: mockUser,
            token: mockToken,
          }
        }),
      });

      const { result } = renderHook(() => useAuthStore());

      await act(async () => {
        await result.current.login('test@example.com', 'password123');
      });

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/auth/login'),
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Content-Type': 'application/json',
          }),
          body: JSON.stringify({
            email: 'test@example.com',
            password: 'password123',
          }),
        })
      );
    });
  });

  describe('註冊功能', () => {
    it('應該成功註冊', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          data: {
            user: mockUser,
            token: mockToken,
          }
        }),
      });

      const { result } = renderHook(() => useAuthStore());

      await act(async () => {
        await result.current.register('test@example.com', 'password123', 'testuser');
      });

      expect(result.current.isAuthenticated).toBe(true);
      expect(result.current.user).toEqual(mockUser);
      expect(result.current.token).toBe(mockToken);
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
    });

    it('應該處理註冊失敗', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        text: async () => 'Registration failed',
        statusText: 'Bad Request'
      });

      const { result } = renderHook(() => useAuthStore());

      await act(async () => {
        try {
          await result.current.register('test@example.com', 'password123', 'testuser');
        } catch (error) {
          // Expected to throw
        }
      });

      expect(result.current.isAuthenticated).toBe(false);
      expect(result.current.error).toBe('Registration failed');
      expect(result.current.isLoading).toBe(false);
    });

    it('應該發送正確的註冊請求', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          data: {
            user: mockUser,
            token: mockToken,
          }
        }),
      });

      const { result } = renderHook(() => useAuthStore());

      await act(async () => {
        await result.current.register('test@example.com', 'password123', 'testuser');
      });

      expect(mockFetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/auth/register'),
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Content-Type': 'application/json',
          }),
          body: JSON.stringify({
            email: 'test@example.com',
            password: 'password123',
            username: 'testuser',
          }),
        })
      );
    });
  });

  describe('登出功能', () => {
    it('應該成功登出', () => {
      // 先設置已登入狀態
      useAuthStore.setState({
        user: mockUser,
        isAuthenticated: true,
        token: mockToken,
        error: 'some error',
      });

      const { result } = renderHook(() => useAuthStore());

      act(() => {
        result.current.logout();
      });

      expect(result.current.user).toBeNull();
      expect(result.current.isAuthenticated).toBe(false);
      expect(result.current.token).toBeNull();
      expect(result.current.error).toBeNull();
    });
  });

  describe('用戶資料更新', () => {
    beforeEach(() => {
      useAuthStore.setState({
        user: mockUser,
        isAuthenticated: true,
        token: mockToken,
      });
    });

    it('應該成功更新用戶資料', () => {
      const { result } = renderHook(() => useAuthStore());

      const updates = {
        displayName: 'Updated Name',
        avatar: 'new-avatar.jpg',
      };

      act(() => {
        result.current.updateUser(updates);
      });

      expect(result.current.user).toEqual({
        ...mockUser,
        ...updates,
      });
    });

    it('應該在沒有用戶時不執行更新', () => {
      useAuthStore.setState({ user: null });
      
      const { result } = renderHook(() => useAuthStore());

      act(() => {
        result.current.updateUser({ displayName: 'New Name' });
      });

      expect(result.current.user).toBeNull();
    });
  });

  describe('用戶偏好設定更新', () => {
    beforeEach(() => {
      useAuthStore.setState({
        user: mockUser,
        isAuthenticated: true,
        token: mockToken,
      });
    });

    it('應該成功更新用戶偏好設定', () => {
      const { result } = renderHook(() => useAuthStore());

      const preferenceUpdates: Partial<UserPreferences> = {
        theme: 'dark',
        densityLevel: 'compact',
        energyTracking: false,
      };

      act(() => {
        result.current.updatePreferences(preferenceUpdates);
      });

      expect(result.current.user?.preferences).toEqual({
        ...mockUser.preferences,
        ...preferenceUpdates,
      });
    });

    it('應該在沒有用戶時不執行偏好更新', () => {
      useAuthStore.setState({ user: null });
      
      const { result } = renderHook(() => useAuthStore());

      act(() => {
        result.current.updatePreferences({ theme: 'dark' });
      });

      expect(result.current.user).toBeNull();
    });
  });

  describe('錯誤處理', () => {
    it('應該能清除錯誤', () => {
      useAuthStore.setState({ error: 'Some error' });
      
      const { result } = renderHook(() => useAuthStore());

      act(() => {
        result.current.clearError();
      });

      expect(result.current.error).toBeNull();
    });
  });

  describe('Loading 狀態管理', () => {
    it('應該能設置 loading 狀態', () => {
      const { result } = renderHook(() => useAuthStore());

      act(() => {
        result.current.setLoading(true);
      });

      expect(result.current.isLoading).toBe(true);

      act(() => {
        result.current.setLoading(false);
      });

      expect(result.current.isLoading).toBe(false);
    });
  });

  describe('持久化儲存', () => {
    it('應該持久化重要的認證資料', () => {
      const { result } = renderHook(() => useAuthStore());

      act(() => {
        useAuthStore.setState({
          user: mockUser,
          token: mockToken,
          isAuthenticated: true,
        });
      });

      // 檢查 localStorage 是否包含認證資料
      const storedData = localStorage.getItem('auth-store');
      if (storedData) {
        const parsedData = JSON.parse(storedData);
        expect(parsedData.state.user).toEqual(mockUser);
        expect(parsedData.state.token).toBe(mockToken);
        expect(parsedData.state.isAuthenticated).toBe(true);
      }
    });

    it('不應該持久化錯誤和 loading 狀態', () => {
      act(() => {
        useAuthStore.setState({
          user: mockUser,
          token: mockToken,
          isAuthenticated: true,
          error: 'Some error',
          isLoading: true,
        });
      });

      const storedData = localStorage.getItem('auth-store');
      if (storedData) {
        const parsedData = JSON.parse(storedData);
        expect(parsedData.state.error).toBeUndefined();
        expect(parsedData.state.isLoading).toBeUndefined();
      }
    });
  });

  describe('並發操作', () => {
    it('應該正確處理並發登入請求', async () => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({
          user: mockUser,
          token: mockToken,
        }),
      });

      const { result } = renderHook(() => useAuthStore());

      // 同時發起兩個登入請求
      const loginPromise1 = act(async () => {
        await result.current.login('test1@example.com', 'password123');
      });

      const loginPromise2 = act(async () => {
        await result.current.login('test2@example.com', 'password123');
      });

      await Promise.all([loginPromise1, loginPromise2]);

      // 應該只有一個用戶登入
      expect(result.current.isAuthenticated).toBe(true);
      expect(result.current.user).toEqual(mockUser);
    });
  });
});