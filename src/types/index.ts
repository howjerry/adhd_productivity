// ADHD Productivity System - Type Definitions
// Comprehensive type system for the application

// Base types
export type UUID = string;
export type Timestamp = string; // ISO 8601 timestamp

// Priority levels based on the ADHD system
export enum Priority {
  LOW = 'low',
  MEDIUM = 'medium',
  HIGH = 'high'
}

// Task status progression
export enum TaskStatus {
  CAPTURED = 'captured',
  PROCESSED = 'processed',
  ACTIVE = 'active',
  IN_PROGRESS = 'in_progress',
  COMPLETED = 'completed',
  CANCELLED = 'cancelled',
  SOMEDAY = 'someday'
}

// Energy levels for task matching
export enum EnergyLevel {
  HIGH = 'high',
  MEDIUM = 'medium',
  LOW = 'low',
  DEPLETED = 'depleted'
}

// Time block types for scheduling
export enum TimeBlockType {
  DEEP_WORK = 'deep_work',
  ADMIN = 'admin',
  CREATIVE = 'creative',
  MEETING = 'meeting',
  BREAK = 'break',
  BUFFER = 'buffer'
}

// Capture sources for tracking input methods
export enum CaptureSource {
  QUICK_CAPTURE = 'quick_capture',
  EMAIL = 'email',
  VOICE = 'voice',
  CALENDAR = 'calendar',
  BROWSER = 'browser',
  MOBILE = 'mobile'
}

// Core domain models
export interface User {
  id: UUID;
  email: string;
  username: string;
  displayName: string;
  avatar?: string;
  preferences: UserPreferences;
  createdAt: Timestamp;
  updatedAt: Timestamp;
}

export interface UserPreferences {
  theme: 'light' | 'dark' | 'auto';
  timezone: string;
  workingHours: {
    start: string; // HH:mm format
    end: string;   // HH:mm format
  };
  pomodoroSettings: {
    workDuration: number; // minutes
    shortBreak: number;   // minutes
    longBreak: number;    // minutes
    sessionsUntilLongBreak: number;
  };
  notificationSettings: {
    desktop: boolean;
    email: boolean;
    mobile: boolean;
    quiet_hours: {
      enabled: boolean;
      start: string;
      end: string;
    };
  };
  energyTracking: boolean;
  gamificationEnabled: boolean;
  densityLevel: 'compact' | 'normal' | 'comfortable';
}

export interface CaptureItem {
  id: UUID;
  userId: UUID;
  content: string;
  source: CaptureSource;
  isProcessed: boolean;
  processedAt?: Timestamp;
  processedIntoTaskId?: UUID;
  metadata?: Record<string, unknown>;
  createdAt: Timestamp;
  updatedAt: Timestamp;
}

export interface Task {
  id: UUID;
  userId: UUID;
  title: string;
  description?: string;
  status: TaskStatus;
  priority: Priority;
  energyLevel?: EnergyLevel;
  estimatedMinutes?: number;
  actualMinutes?: number;
  dueDate?: Timestamp;
  scheduledDate?: Timestamp;
  context?: string;
  tags: string[];
  parentTaskId?: UUID;
  subtasks: Task[];
  timeBlocks: TimeBlock[];
  createdAt: Timestamp;
  updatedAt: Timestamp;
  completedAt?: Timestamp;
}

export interface TimeBlock {
  id: UUID;
  userId: UUID;
  taskId?: UUID;
  title: string;
  description?: string;
  type: TimeBlockType;
  startTime: Timestamp;
  endTime: Timestamp;
  isCompleted: boolean;
  actualStartTime?: Timestamp;
  actualEndTime?: Timestamp;
  notes?: string;
  createdAt: Timestamp;
  updatedAt: Timestamp;
}

export interface UserProgress {
  userId: UUID;
  totalPoints: number;
  currentStreak: number;
  longestStreak: number;
  level: number;
  experiencePoints: number;
  pointsToNextLevel: number;
  achievements: Achievement[];
  dailyStats: DailyStats;
  weeklyStats: WeeklyStats;
  monthlyStats: MonthlyStats;
  updatedAt: Timestamp;
}

export interface Achievement {
  id: string;
  name: string;
  description: string;
  iconName: string;
  category: 'tasks' | 'time' | 'streak' | 'focus' | 'milestone';
  points: number;
  unlockedAt: Timestamp;
  rarity: 'common' | 'rare' | 'epic' | 'legendary';
}

export interface DailyStats {
  date: string; // YYYY-MM-DD format
  tasksCreated: number;
  tasksCompleted: number;
  focusMinutes: number;
  breakMinutes: number;
  averageEnergyLevel: number;
  pomodorosSessions: number;
  pointsEarned: number;
}

export interface WeeklyStats {
  weekStart: string; // YYYY-MM-DD format
  tasksCompleted: number;
  focusHours: number;
  averageCompletionRate: number;
  mostProductiveDay: string;
  pointsEarned: number;
}

export interface MonthlyStats {
  month: string; // YYYY-MM format
  tasksCompleted: number;
  focusHours: number;
  achievementsUnlocked: number;
  averageWeeklyScore: number;
  pointsEarned: number;
}

// UI State types
export interface TimerState {
  isRunning: boolean;
  isPaused: boolean;
  mode: 'work' | 'short_break' | 'long_break';
  timeRemaining: number; // seconds
  totalDuration: number; // seconds
  sessionCount: number;
  currentTaskId?: UUID;
  startedAt?: Timestamp;
}

export interface AppState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  theme: 'light' | 'dark';
  sidebarOpen: boolean;
  currentView: 'dashboard' | 'tasks' | 'calendar' | 'capture' | 'stats' | 'settings';
}

export interface TaskFilters {
  status?: TaskStatus[];
  priority?: Priority[];
  energyLevel?: EnergyLevel[];
  tags?: string[];
  context?: string;
  dueDate?: {
    start?: string;
    end?: string;
  };
  search?: string;
}

export interface PaginationParams {
  page: number;
  limit: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

export interface ApiResponse<T> {
  data: T;
  message?: string;
  errors?: string[];
}

export interface PaginatedResponse<T> extends ApiResponse<T[]> {
  pagination: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
    hasNext: boolean;
    hasPrev: boolean;
  };
}

// Form types
export interface TaskFormData {
  title: string;
  description?: string;
  priority: Priority;
  energyLevel?: EnergyLevel;
  estimatedMinutes?: number;
  dueDate?: string;
  scheduledDate?: string;
  context?: string;
  tags: string[];
}

export interface CaptureFormData {
  content: string;
  source: CaptureSource;
}

export interface TimeBlockFormData {
  title: string;
  description?: string;
  type: TimeBlockType;
  startTime: string;
  endTime: string;
  taskId?: UUID;
}

// Component prop types
export interface BaseComponentProps {
  className?: string;
  children?: React.ReactNode;
}

export interface ButtonProps extends BaseComponentProps {
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger' | 'success';
  size?: 'sm' | 'md' | 'lg';
  disabled?: boolean;
  loading?: boolean;
  onClick?: () => void;
  type?: 'button' | 'submit' | 'reset';
}

export interface CardProps extends BaseComponentProps {
  elevation?: 'sm' | 'md' | 'lg';
  padding?: 'sm' | 'md' | 'lg';
  hover?: boolean;
}

export interface ModalProps extends BaseComponentProps {
  isOpen: boolean;
  onClose: () => void;
  title?: string;
  size?: 'sm' | 'md' | 'lg' | 'xl';
}

// Event types for SignalR
export interface TimerSyncEvent {
  userId: UUID;
  timerState: TimerState;
  timestamp: Timestamp;
}

export interface TaskUpdateEvent {
  taskId: UUID;
  userId: UUID;
  changes: Partial<Task>;
  timestamp: Timestamp;
}

export interface ProgressUpdateEvent {
  userId: UUID;
  pointsEarned: number;
  newAchievements: Achievement[];
  updatedStats: DailyStats;
  timestamp: Timestamp;
}

// Hook return types
export interface UseTimerReturn {
  state: TimerState;
  start: (taskId?: UUID) => void;
  pause: () => void;
  resume: () => void;
  stop: () => void;
  reset: () => void;
  switchMode: (mode: TimerState['mode']) => void;
}

export interface UseTasksReturn {
  tasks: Task[];
  loading: boolean;
  error: string | null;
  createTask: (data: TaskFormData) => Promise<Task>;
  updateTask: (id: UUID, data: Partial<TaskFormData>) => Promise<Task>;
  deleteTask: (id: UUID) => Promise<void>;
  completeTask: (id: UUID) => Promise<Task>;
  fetchTasks: (filters?: TaskFilters, pagination?: PaginationParams) => Promise<void>;
  refresh: () => Promise<void>;
}

// Utility types
export type DeepPartial<T> = {
  [P in keyof T]?: T[P] extends object ? DeepPartial<T[P]> : T[P];
};

export type Optional<T, K extends keyof T> = Omit<T, K> & Partial<Pick<T, K>>;

export type CreateTaskData = Omit<Task, 'id' | 'userId' | 'status' | 'subtasks' | 'timeBlocks' | 'createdAt' | 'updatedAt' | 'completedAt'>;

export type UpdateTaskData = Partial<Omit<Task, 'id' | 'userId' | 'createdAt' | 'subtasks' | 'timeBlocks'>>;