import { vi } from 'vitest';

const createMockConnection = () => ({
  start: vi.fn().mockResolvedValue(null),
  stop: vi.fn().mockResolvedValue(null),
  on: vi.fn(),
  off: vi.fn(),
  invoke: vi.fn().mockResolvedValue(null),
  onclose: vi.fn(),
  onreconnecting: vi.fn(),
  onreconnected: vi.fn(),
  state: 'Disconnected',
  connectionId: 'mock-connection-id',
});

export const signalRService = {
  initializeConnection: vi.fn(),
  connect: vi.fn(),
  disconnect: vi.fn(),
  syncTimer: vi.fn(),
  notifyTaskUpdate: vi.fn(),
  notifyProgressUpdate: vi.fn(),
  joinCoworkingSession: vi.fn(),
  leaveCoworkingSession: vi.fn(),
  joinPersonalChannel: vi.fn(),
  onTimerSync: vi.fn(() => () => {}),
  onTaskUpdate: vi.fn(() => () => {}),
  onProgressUpdate: vi.fn(() => () => {}),
  isConnected: vi.fn(() => false),
  getConnectionState: vi.fn(() => 'Disconnected'),
  getConnectionInfo: vi.fn(() => ({
    state: 'Disconnected',
    connectionId: null,
    reconnectAttempts: 0,
    isConnecting: false,
  })),
  connection: createMockConnection(),
};

export const useSignalR = () => ({
  service: signalRService,
  isConnected: signalRService.isConnected(),
  connectionState: signalRService.getConnectionState(),
  onTimerSync: signalRService.onTimerSync,
  onTaskUpdate: signalRService.onTaskUpdate,
  onProgressUpdate: signalRService.onProgressUpdate,
});

export default signalRService;
