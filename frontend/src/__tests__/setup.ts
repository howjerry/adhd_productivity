import '@testing-library/jest-dom'
import { vi } from 'vitest'

// Mock matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation(query => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(), // deprecated
    removeListener: vi.fn(), // deprecated
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
})

// Mock ResizeObserver
global.ResizeObserver = vi.fn().mockImplementation(() => ({
  observe: vi.fn(),
  unobserve: vi.fn(),
  disconnect: vi.fn(),
}))

// Mock IntersectionObserver
global.IntersectionObserver = vi.fn().mockImplementation(() => ({
  observe: vi.fn(),
  unobserve: vi.fn(),
  disconnect: vi.fn(),
}))

// Mock window.scrollTo
Object.defineProperty(window, 'scrollTo', {
  value: vi.fn(),
  writable: true,
})

// Mock Notification API
Object.defineProperty(window, 'Notification', {
  value: vi.fn().mockImplementation((title, options) => ({
    title,
    ...options,
    close: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
  })),
  writable: true,
})

Object.defineProperty(Notification, 'permission', {
  value: 'granted',
  writable: true,
})

Object.defineProperty(Notification, 'requestPermission', {
  value: vi.fn().mockResolvedValue('granted'),
  writable: true,
})

// Mock Audio
Object.defineProperty(window, 'Audio', {
  writable: true,
  value: vi.fn().mockImplementation(() => ({
    play: vi.fn().mockResolvedValue(undefined),
    pause: vi.fn(),
    load: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    currentTime: 0,
    duration: 0,
    volume: 1,
    muted: false,
    paused: true,
    ended: false,
    error: null,
  })),
})

// Mock crypto.randomUUID
Object.defineProperty(global, 'crypto', {
  value: {
    randomUUID: vi.fn(() => 'mocked-uuid'),
  },
})

// Mock localStorage
const localStorageMock = (() => {
  let store: Record<string, string> = {}

  return {
    getItem: vi.fn((key: string) => store[key] || null),
    setItem: vi.fn((key: string, value: string) => {
      store[key] = value
    }),
    removeItem: vi.fn((key: string) => {
      delete store[key]
    }),
    clear: vi.fn(() => {
      store = {}
    }),
    length: 0,
    key: vi.fn(() => null),
  }
})()

Object.defineProperty(window, 'localStorage', {
  value: localStorageMock,
  writable: true,
})

// Mock sessionStorage
Object.defineProperty(window, 'sessionStorage', {
  value: localStorageMock,
  writable: true,
})

// Mock getComputedStyle
Object.defineProperty(window, 'getComputedStyle', {
  value: vi.fn().mockImplementation(() => ({
    getPropertyValue: vi.fn(),
    setProperty: vi.fn(),
  })),
})

// Mock requestAnimationFrame
Object.defineProperty(window, 'requestAnimationFrame', {
  value: vi.fn(cb => setTimeout(cb, 16)),
  writable: true,
})

Object.defineProperty(window, 'cancelAnimationFrame', {
  value: vi.fn(id => clearTimeout(id)),
  writable: true,
})

// Mock Element.scrollIntoView
Element.prototype.scrollIntoView = vi.fn()

// Mock Element.getBoundingClientRect
Element.prototype.getBoundingClientRect = vi.fn(() => ({
  bottom: 0,
  height: 0,
  left: 0,
  right: 0,
  top: 0,
  width: 0,
  x: 0,
  y: 0,
  toJSON: vi.fn(),
}))

// Clean up after each test
afterEach(() => {
  vi.clearAllMocks()
  localStorageMock.clear()
})

// Global test utilities
global.testUtils = {
  // Helper to wait for async operations
  waitFor: async (callback: () => void, timeout = 1000) => {
    const start = Date.now()
    while (Date.now() - start < timeout) {
      try {
        callback()
        return
      } catch (error) {
        await new Promise(resolve => setTimeout(resolve, 10))
      }
    }
    throw new Error('Timeout waiting for condition')
  },
  
  // Helper to create mock user
  createMockUser: () => ({
    id: 'test-user-id',
    email: 'test@example.com',
    firstName: 'Test',
    lastName: 'User',
    adhdType: 'Combined',
    theme: 'Light',
    createdAt: new Date().toISOString(),
  }),
  
  // Helper to create mock tasks
  createMockTask: (overrides = {}) => ({
    id: crypto.randomUUID(),
    title: 'Test Task',
    description: 'Test Description',
    priority: 'Medium',
    status: 'Todo',
    estimatedMinutes: 60,
    dueDate: new Date().toISOString(),
    tags: 'test,mock',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    ...overrides,
  }),
}