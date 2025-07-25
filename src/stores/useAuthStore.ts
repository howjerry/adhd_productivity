import { create } from 'zustand';
import { immer } from 'zustand/middleware/immer';
import { persist } from 'zustand/middleware';
import { User, UserPreferences } from '@/types';

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  token: string | null;
}

interface AuthActions {
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, username: string) => Promise<void>;
  logout: () => void;
  updateUser: (updates: Partial<User>) => void;
  updatePreferences: (preferences: Partial<UserPreferences>) => void;
  clearError: () => void;
  setLoading: (loading: boolean) => void;
}

type AuthStore = AuthState & AuthActions;

const initialState: AuthState = {
  user: null,
  isAuthenticated: false,
  isLoading: false,
  error: null,
  token: null,
};

export const useAuthStore = create<AuthStore>()(
  persist(
    immer((set, _get) => ({
      ...initialState,

      login: async (email: string, password: string) => {
        set((state) => {
          state.isLoading = true;
          state.error = null;
        });

        try {
          // TODO: Replace with actual API call
          const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify({ email, password }),
          });

          if (!response.ok) {
            throw new Error('Invalid credentials');
          }

          const { user, token } = await response.json();

          set((state) => {
            state.user = user;
            state.token = token;
            state.isAuthenticated = true;
            state.isLoading = false;
            state.error = null;
          });
        } catch (error) {
          set((state) => {
            state.error = error instanceof Error ? error.message : 'Login failed';
            state.isLoading = false;
          });
          throw error;
        }
      },

      register: async (email: string, password: string, username: string) => {
        set((state) => {
          state.isLoading = true;
          state.error = null;
        });

        try {
          // TODO: Replace with actual API call
          const response = await fetch('/api/auth/register', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify({ email, password, username }),
          });

          if (!response.ok) {
            throw new Error('Registration failed');
          }

          const { user, token } = await response.json();

          set((state) => {
            state.user = user;
            state.token = token;
            state.isAuthenticated = true;
            state.isLoading = false;
            state.error = null;
          });
        } catch (error) {
          set((state) => {
            state.error = error instanceof Error ? error.message : 'Registration failed';
            state.isLoading = false;
          });
          throw error;
        }
      },

      logout: () => {
        set((state) => {
          state.user = null;
          state.token = null;
          state.isAuthenticated = false;
          state.error = null;
        });
      },

      updateUser: (updates: Partial<User>) => {
        set((state) => {
          if (state.user) {
            state.user = { ...state.user, ...updates };
          }
        });
      },

      updatePreferences: (preferences: Partial<UserPreferences>) => {
        set((state) => {
          if (state.user) {
            state.user.preferences = { ...state.user.preferences, ...preferences };
          }
        });
      },

      clearError: () => {
        set((state) => {
          state.error = null;
        });
      },

      setLoading: (loading: boolean) => {
        set((state) => {
          state.isLoading = loading;
        });
      },
    })),
    {
      name: 'auth-store',
      partialize: (state) => ({
        user: state.user,
        token: state.token,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
);