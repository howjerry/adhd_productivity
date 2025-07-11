import * as signalR from '@microsoft/signalr';
import { TimerSyncEvent, TaskUpdateEvent, ProgressUpdateEvent, UUID } from '@/types';

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 1000; // Start with 1 second
  private isConnecting = false;

  // Event handlers
  private timerSyncHandlers: ((event: TimerSyncEvent) => void)[] = [];
  private taskUpdateHandlers: ((event: TaskUpdateEvent) => void)[] = [];
  private progressUpdateHandlers: ((event: ProgressUpdateEvent) => void)[] = [];

  constructor() {
    // Initialize connection when class is instantiated
    this.initializeConnection();
  }

  private initializeConnection() {
    if (this.isConnecting || this.connection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.isConnecting = true;

    // Build connection with optimized settings
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hub/adhd', {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
        accessTokenFactory: () => {
          // Get token from localStorage or auth store
          return localStorage.getItem('auth-token') || '';
        },
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff with jitter
          const delay = Math.min(
            this.reconnectDelay * Math.pow(2, retryContext.previousRetryCount),
            30000 // Max 30 seconds
          );
          return delay + Math.random() * 1000; // Add jitter
        },
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupEventHandlers();
    this.connect();
  }

  private setupEventHandlers() {
    if (!this.connection) return;

    // Connection state handlers
    this.connection.onclose(() => {
      console.log('SignalR connection closed');
      this.isConnecting = false;
    });

    this.connection.onreconnecting(() => {
      console.log('SignalR reconnecting...');
    });

    this.connection.onreconnected(() => {
      console.log('SignalR reconnected');
      this.reconnectAttempts = 0;
      this.rejoinGroups();
    });

    // Timer synchronization events
    this.connection.on('TimerSync', (event: TimerSyncEvent) => {
      this.timerSyncHandlers.forEach(handler => handler(event));
    });

    // Task update events
    this.connection.on('TaskUpdated', (event: TaskUpdateEvent) => {
      this.taskUpdateHandlers.forEach(handler => handler(event));
    });

    // Progress update events
    this.connection.on('ProgressUpdated', (event: ProgressUpdateEvent) => {
      this.progressUpdateHandlers.forEach(handler => handler(event));
    });

    // System notifications
    this.connection.on('SystemNotification', (message: string, type: 'info' | 'warning' | 'error') => {
      console.log(`System notification [${type}]:`, message);
      // Could integrate with a toast notification system
    });

    // Co-working session events
    this.connection.on('SessionJoined', (_userId: UUID, userName: string) => {
      console.log(`${userName} joined the session`);
    });

    this.connection.on('SessionLeft', (_userId: UUID, userName: string) => {
      console.log(`${userName} left the session`);
    });
  }

  private async connect() {
    if (!this.connection || this.connection.state === signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      await this.connection.start();
      console.log('SignalR connected successfully');
      this.reconnectAttempts = 0;
      this.isConnecting = false;
      
      // Join user's personal channel
      await this.joinPersonalChannel();
    } catch (error) {
      console.error('SignalR connection failed:', error);
      this.isConnecting = false;
      this.handleReconnection();
    }
  }

  private handleReconnection() {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      console.error('Max reconnection attempts reached');
      return;
    }

    this.reconnectAttempts++;
    const delay = this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1);
    
    console.log(`Attempting to reconnect in ${delay}ms (attempt ${this.reconnectAttempts})`);
    
    setTimeout(() => {
      this.connect();
    }, delay);
  }

  private async rejoinGroups() {
    // Rejoin any groups the user was part of
    try {
      await this.joinPersonalChannel();
      // Add other group rejoining logic here
    } catch (error) {
      console.error('Failed to rejoin groups:', error);
    }
  }

  // Public methods for timer synchronization
  async syncTimer(timerState: Omit<TimerSyncEvent, 'timestamp'>) {
    if (!this.isConnected()) return;

    try {
      await this.connection!.invoke('SyncTimer', {
        ...timerState,
        timestamp: new Date().toISOString(),
      });
    } catch (error) {
      console.error('Failed to sync timer:', error);
    }
  }

  // Public methods for task management
  async notifyTaskUpdate(taskId: UUID, changes: Record<string, unknown>) {
    if (!this.isConnected()) return;

    try {
      await this.connection!.invoke('NotifyTaskUpdate', {
        taskId,
        changes,
        timestamp: new Date().toISOString(),
      });
    } catch (error) {
      console.error('Failed to notify task update:', error);
    }
  }

  // Public methods for progress tracking
  async notifyProgressUpdate(progressData: Omit<ProgressUpdateEvent, 'timestamp'>) {
    if (!this.isConnected()) return;

    try {
      await this.connection!.invoke('NotifyProgressUpdate', {
        ...progressData,
        timestamp: new Date().toISOString(),
      });
    } catch (error) {
      console.error('Failed to notify progress update:', error);
    }
  }

  // Co-working session methods
  async joinCoworkingSession(sessionId: string) {
    if (!this.isConnected()) return;

    try {
      await this.connection!.invoke('JoinCoworkingSession', sessionId);
      console.log(`Joined co-working session: ${sessionId}`);
    } catch (error) {
      console.error('Failed to join co-working session:', error);
    }
  }

  async leaveCoworkingSession(sessionId: string) {
    if (!this.isConnected()) return;

    try {
      await this.connection!.invoke('LeaveCoworkingSession', sessionId);
      console.log(`Left co-working session: ${sessionId}`);
    } catch (error) {
      console.error('Failed to leave co-working session:', error);
    }
  }

  // Private channel for user-specific notifications
  private async joinPersonalChannel() {
    if (!this.isConnected()) return;

    try {
      await this.connection!.invoke('JoinPersonalChannel');
      console.log('Joined personal notification channel');
    } catch (error) {
      console.error('Failed to join personal channel:', error);
    }
  }

  // Event subscription methods
  onTimerSync(handler: (event: TimerSyncEvent) => void) {
    this.timerSyncHandlers.push(handler);
    
    // Return unsubscribe function
    return () => {
      const index = this.timerSyncHandlers.indexOf(handler);
      if (index > -1) {
        this.timerSyncHandlers.splice(index, 1);
      }
    };
  }

  onTaskUpdate(handler: (event: TaskUpdateEvent) => void) {
    this.taskUpdateHandlers.push(handler);
    
    return () => {
      const index = this.taskUpdateHandlers.indexOf(handler);
      if (index > -1) {
        this.taskUpdateHandlers.splice(index, 1);
      }
    };
  }

  onProgressUpdate(handler: (event: ProgressUpdateEvent) => void) {
    this.progressUpdateHandlers.push(handler);
    
    return () => {
      const index = this.progressUpdateHandlers.indexOf(handler);
      if (index > -1) {
        this.progressUpdateHandlers.splice(index, 1);
      }
    };
  }

  // Utility methods
  isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }

  getConnectionState(): string {
    return this.connection?.state || 'Disconnected';
  }

  async disconnect() {
    if (this.connection) {
      try {
        await this.connection.stop();
        console.log('SignalR disconnected');
      } catch (error) {
        console.error('Error disconnecting SignalR:', error);
      }
    }
  }

  // Debugging and monitoring
  getConnectionInfo() {
    return {
      state: this.getConnectionState(),
      connectionId: this.connection?.connectionId,
      reconnectAttempts: this.reconnectAttempts,
      isConnecting: this.isConnecting,
    };
  }
}

// Create singleton instance
export const signalRService = new SignalRService();

// React hook for easy integration
export const useSignalR = () => {
  return {
    service: signalRService,
    isConnected: signalRService.isConnected(),
    connectionState: signalRService.getConnectionState(),
    onTimerSync: signalRService.onTimerSync.bind(signalRService),
    onTaskUpdate: signalRService.onTaskUpdate.bind(signalRService),
    onProgressUpdate: signalRService.onProgressUpdate.bind(signalRService),
  };
};

// Types for external use
export type {
  TimerSyncEvent,
  TaskUpdateEvent,
  ProgressUpdateEvent,
};

export default signalRService;