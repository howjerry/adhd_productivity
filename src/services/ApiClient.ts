import { 
  ApiResponse, 
  PaginatedResponse, 
  TaskFormData, 
  Task, 
  CaptureFormData, 
  CaptureItem, 
  TimeBlockFormData, 
  TimeBlock, 
  UserProgress, 
  User, 
  UserPreferences, 
  UUID,
  TaskFilters,
  PaginationParams
} from '@/types';

// API 配置
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api';
const API_TIMEOUT = 30000; // 30 秒超時

// 錯誤類型定義
export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
    public statusText: string,
    public response?: Response
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export class NetworkError extends Error {
  constructor(message: string, public originalError: Error) {
    super(message);
    this.name = 'NetworkError';
  }
}

export class TimeoutError extends Error {
  constructor(message: string = '請求超時') {
    super(message);
    this.name = 'TimeoutError';
  }
}

// 重試策略配置
interface RetryConfig {
  maxRetries: number;
  baseDelay: number;
  maxDelay: number;
  retryCondition?: (error: Error) => boolean;
}

const DEFAULT_RETRY_CONFIG: RetryConfig = {
  maxRetries: 3,
  baseDelay: 1000,
  maxDelay: 10000,
  retryCondition: (error) => {
    if (error instanceof ApiError) {
      // 對於某些狀態碼不重試
      return ![400, 401, 403, 404, 422].includes(error.status);
    }
    return error instanceof NetworkError || error instanceof TimeoutError;
  }
};

// 請求攔截器配置
interface RequestInterceptor {
  onRequest?: (config: RequestConfig) => RequestConfig | Promise<RequestConfig>;
  onRequestError?: (error: Error) => Promise<never>;
}

// 回應攔截器配置
interface ResponseInterceptor {
  onResponse?: <T>(response: ApiResponse<T>) => ApiResponse<T> | Promise<ApiResponse<T>>;
  onResponseError?: (error: Error) => Promise<never>;
}

// 請求配置
interface RequestConfig {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  headers?: Record<string, string>;
  body?: unknown;
  timeout?: number;
  retryConfig?: Partial<RetryConfig>;
  skipAuthToken?: boolean;
}

// 記憶體中快取
interface CacheEntry<T> {
  data: T;
  timestamp: number;
  ttl: number;
}

class ApiCache {
  private cache = new Map<string, CacheEntry<any>>();

  set<T>(key: string, data: T, ttl: number = 300000): void { // 預設 5 分鐘
    this.cache.set(key, {
      data,
      timestamp: Date.now(),
      ttl
    });
  }

  get<T>(key: string): T | null {
    const entry = this.cache.get(key);
    if (!entry) return null;

    if (Date.now() - entry.timestamp > entry.ttl) {
      this.cache.delete(key);
      return null;
    }

    return entry.data;
  }

  delete(key: string): void {
    this.cache.delete(key);
  }

  clear(): void {
    this.cache.clear();
  }

  // 根據 pattern 清除快取
  clearByPattern(pattern: string): void {
    const regex = new RegExp(pattern);
    for (const key of this.cache.keys()) {
      if (regex.test(key)) {
        this.cache.delete(key);
      }
    }
  }
}

export class ApiClient {
  private baseURL: string;
  private defaultHeaders: Record<string, string>;
  private authTokenProvider: (() => string | null) | null = null;
  private requestInterceptors: RequestInterceptor[] = [];
  private responseInterceptors: ResponseInterceptor[] = [];
  private cache = new ApiCache();

  constructor(baseURL: string = API_BASE_URL) {
    this.baseURL = baseURL;
    this.defaultHeaders = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };
  }

  // 設置認證 token 提供者
  setAuthTokenProvider(provider: () => string | null): void {
    this.authTokenProvider = provider;
  }

  // 添加請求攔截器
  addRequestInterceptor(interceptor: RequestInterceptor): void {
    this.requestInterceptors.push(interceptor);
  }

  // 添加回應攔截器
  addResponseInterceptor(interceptor: ResponseInterceptor): void {
    this.responseInterceptors.push(interceptor);
  }

  // 清除所有攔截器
  clearInterceptors(): void {
    this.requestInterceptors = [];
    this.responseInterceptors = [];
  }

  // 獲取完整 URL
  private getFullUrl(endpoint: string): string {
    if (endpoint.startsWith('http')) {
      return endpoint;
    }
    return `${this.baseURL}${endpoint.startsWith('/') ? endpoint : '/' + endpoint}`;
  }

  // 準備請求標頭
  private prepareHeaders(config: RequestConfig): Record<string, string> {
    const headers = { ...this.defaultHeaders, ...config.headers };

    // 添加認證標頭
    if (!config.skipAuthToken && this.authTokenProvider) {
      const token = this.authTokenProvider();
      if (token) {
        headers.Authorization = `Bearer ${token}`;
      }
    }

    return headers;
  }

  // 執行請求攔截器
  private async runRequestInterceptors(config: RequestConfig): Promise<RequestConfig> {
    let processedConfig = config;
    
    for (const interceptor of this.requestInterceptors) {
      if (interceptor.onRequest) {
        try {
          processedConfig = await interceptor.onRequest(processedConfig);
        } catch (error) {
          if (interceptor.onRequestError) {
            await interceptor.onRequestError(error as Error);
          }
          throw error;
        }
      }
    }

    return processedConfig;
  }

  // 執行回應攔截器
  private async runResponseInterceptors<T>(response: ApiResponse<T>): Promise<ApiResponse<T>> {
    let processedResponse = response;

    for (const interceptor of this.responseInterceptors) {
      if (interceptor.onResponse) {
        try {
          processedResponse = await interceptor.onResponse(processedResponse);
        } catch (error) {
          if (interceptor.onResponseError) {
            await interceptor.onResponseError(error as Error);
          }
          throw error;
        }
      }
    }

    return processedResponse;
  }

  // 帶超時的 fetch
  private async fetchWithTimeout(url: string, options: RequestInit, timeout: number): Promise<Response> {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), timeout);

    try {
      const response = await fetch(url, {
        ...options,
        signal: controller.signal,
      });
      clearTimeout(timeoutId);
      return response;
    } catch (error) {
      clearTimeout(timeoutId);
      if (error instanceof Error && error.name === 'AbortError') {
        throw new TimeoutError();
      }
      throw new NetworkError('網路連線失敗', error as Error);
    }
  }

  // 指數退避重試
  private async sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  private async retryRequest<T>(
    fn: () => Promise<T>,
    retryConfig: RetryConfig
  ): Promise<T> {
    let lastError: Error;

    for (let attempt = 0; attempt <= retryConfig.maxRetries; attempt++) {
      try {
        return await fn();
      } catch (error) {
        lastError = error as Error;

        if (attempt === retryConfig.maxRetries) {
          break;
        }

        if (retryConfig.retryCondition && !retryConfig.retryCondition(lastError)) {
          break;
        }

        const delay = Math.min(
          retryConfig.baseDelay * Math.pow(2, attempt),
          retryConfig.maxDelay
        );

        console.warn(`API 請求失敗，第 ${attempt + 1} 次重試，${delay}ms 後重新嘗試:`, lastError.message);
        await this.sleep(delay);
      }
    }

    throw lastError!;
  }

  // 主要請求方法
  async request<T>(endpoint: string, config: RequestConfig = {}): Promise<ApiResponse<T>> {
    // 執行請求攔截器
    const processedConfig = await this.runRequestInterceptors(config);

    const url = this.getFullUrl(endpoint);
    const headers = this.prepareHeaders(processedConfig);
    const timeout = processedConfig.timeout || API_TIMEOUT;
    const retryConfig = { ...DEFAULT_RETRY_CONFIG, ...processedConfig.retryConfig };

    const makeRequest = async (): Promise<ApiResponse<T>> => {
      const requestOptions: RequestInit = {
        method: processedConfig.method || 'GET',
        headers,
      };

      if (processedConfig.body && processedConfig.method !== 'GET') {
        requestOptions.body = JSON.stringify(processedConfig.body);
      }

      const response = await this.fetchWithTimeout(url, requestOptions, timeout);

      if (!response.ok) {
        const errorMessage = await response.text().catch(() => '未知錯誤');
        throw new ApiError(errorMessage, response.status, response.statusText, response);
      }

      const data = await response.json();
      const apiResponse: ApiResponse<T> = {
        data: data.data || data,
        message: data.message,
        errors: data.errors,
      };

      return this.runResponseInterceptors(apiResponse);
    };

    return this.retryRequest(makeRequest, retryConfig);
  }

  // 便利方法
  async get<T>(endpoint: string, config?: Omit<RequestConfig, 'method'>): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, { ...config, method: 'GET' });
  }

  async post<T>(endpoint: string, body?: unknown, config?: Omit<RequestConfig, 'method' | 'body'>): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, { ...config, method: 'POST', body });
  }

  async put<T>(endpoint: string, body?: unknown, config?: Omit<RequestConfig, 'method' | 'body'>): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, { ...config, method: 'PUT', body });
  }

  async patch<T>(endpoint: string, body?: unknown, config?: Omit<RequestConfig, 'method' | 'body'>): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, { ...config, method: 'PATCH', body });
  }

  async delete<T>(endpoint: string, config?: Omit<RequestConfig, 'method'>): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, { ...config, method: 'DELETE' });
  }

  // 帶快取的 GET 請求
  async getCached<T>(endpoint: string, ttl?: number, config?: Omit<RequestConfig, 'method'>): Promise<ApiResponse<T>> {
    const cacheKey = `${endpoint}:${JSON.stringify(config || {})}`;
    const cached = this.cache.get<ApiResponse<T>>(cacheKey);
    
    if (cached) {
      return cached;
    }

    const response = await this.get<T>(endpoint, config);
    this.cache.set(cacheKey, response, ttl);
    
    return response;
  }

  // 清除快取
  clearCache(pattern?: string): void {
    if (pattern) {
      this.cache.clearByPattern(pattern);
    } else {
      this.cache.clear();
    }
  }

  // === 具體 API 方法 ===

  // 認證相關 API
  async login(email: string, password: string): Promise<ApiResponse<{ user: User; token: string }>> {
    return this.post<{ user: User; token: string }>('/auth/login', { email, password });
  }

  async register(email: string, password: string, username: string): Promise<ApiResponse<{ user: User; token: string }>> {
    return this.post<{ user: User; token: string }>('/auth/register', { email, password, username });
  }

  async refreshToken(): Promise<ApiResponse<{ token: string }>> {
    return this.post<{ token: string }>('/auth/refresh');
  }

  async logout(): Promise<ApiResponse<void>> {
    return this.post<void>('/auth/logout');
  }

  // 使用者相關 API
  async getCurrentUser(): Promise<ApiResponse<User>> {
    return this.getCached<User>('/user/profile', 60000); // 快取 1 分鐘
  }

  async updateUser(updates: Partial<User>): Promise<ApiResponse<User>> {
    const response = await this.patch<User>('/user/profile', updates);
    this.clearCache('user');
    return response;
  }

  async updateUserPreferences(preferences: Partial<UserPreferences>): Promise<ApiResponse<UserPreferences>> {
    const response = await this.patch<UserPreferences>('/user/preferences', preferences);
    this.clearCache('user');
    return response;
  }

  // 任務相關 API
  async getTasks(filters?: TaskFilters, pagination?: PaginationParams): Promise<PaginatedResponse<Task>> {
    const params = new URLSearchParams();
    
    if (filters) {
      Object.entries(filters).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          if (Array.isArray(value)) {
            value.forEach(v => params.append(key, v.toString()));
          } else if (typeof value === 'object') {
            params.append(key, JSON.stringify(value));
          } else {
            params.append(key, value.toString());
          }
        }
      });
    }

    if (pagination) {
      Object.entries(pagination).forEach(([key, value]) => {
        if (value !== undefined) {
          params.append(key, value.toString());
        }
      });
    }

    const queryString = params.toString();
    const endpoint = `/tasks${queryString ? `?${queryString}` : ''}`;
    
    return this.get<Task[]>(endpoint) as Promise<PaginatedResponse<Task>>;
  }

  async getTask(id: UUID): Promise<ApiResponse<Task>> {
    return this.getCached<Task>(`/tasks/${id}`, 30000); // 快取 30 秒
  }

  async createTask(data: TaskFormData): Promise<ApiResponse<Task>> {
    const response = await this.post<Task>('/tasks', data);
    this.clearCache('tasks');
    return response;
  }

  async updateTask(id: UUID, updates: Partial<Task>): Promise<ApiResponse<Task>> {
    const response = await this.patch<Task>(`/tasks/${id}`, updates);
    this.clearCache('tasks');
    this.cache.delete(`/tasks/${id}`);
    return response;
  }

  async deleteTask(id: UUID): Promise<ApiResponse<void>> {
    const response = await this.delete<void>(`/tasks/${id}`);
    this.clearCache('tasks');
    this.cache.delete(`/tasks/${id}`);
    return response;
  }

  async completeTask(id: UUID): Promise<ApiResponse<Task>> {
    const response = await this.patch<Task>(`/tasks/${id}/complete`);
    this.clearCache('tasks');
    this.cache.delete(`/tasks/${id}`);
    return response;
  }

  // 快速捕獲相關 API
  async getCaptureItems(): Promise<ApiResponse<CaptureItem[]>> {
    return this.get<CaptureItem[]>('/capture');
  }

  async createCaptureItem(data: CaptureFormData): Promise<ApiResponse<CaptureItem>> {
    const response = await this.post<CaptureItem>('/capture', data);
    this.clearCache('capture');
    return response;
  }

  async processCaptureItem(id: UUID, taskData?: TaskFormData): Promise<ApiResponse<Task | void>> {
    const response = await this.post<Task | void>(`/capture/${id}/process`, taskData);
    this.clearCache('capture');
    this.clearCache('tasks');
    return response;
  }

  async deleteCaptureItem(id: UUID): Promise<ApiResponse<void>> {
    const response = await this.delete<void>(`/capture/${id}`);
    this.clearCache('capture');
    return response;
  }

  // 時間區塊相關 API
  async getTimeBlocks(startDate?: string, endDate?: string): Promise<ApiResponse<TimeBlock[]>> {
    const params = new URLSearchParams();
    if (startDate) params.append('start', startDate);
    if (endDate) params.append('end', endDate);
    
    const queryString = params.toString();
    const endpoint = `/timeblocks${queryString ? `?${queryString}` : ''}`;
    
    return this.getCached<TimeBlock[]>(endpoint, 60000); // 快取 1 分鐘
  }

  async createTimeBlock(data: TimeBlockFormData): Promise<ApiResponse<TimeBlock>> {
    const response = await this.post<TimeBlock>('/timeblocks', data);
    this.clearCache('timeblocks');
    return response;
  }

  async updateTimeBlock(id: UUID, updates: Partial<TimeBlock>): Promise<ApiResponse<TimeBlock>> {
    const response = await this.patch<TimeBlock>(`/timeblocks/${id}`, updates);
    this.clearCache('timeblocks');
    return response;
  }

  async deleteTimeBlock(id: UUID): Promise<ApiResponse<void>> {
    const response = await this.delete<void>(`/timeblocks/${id}`);
    this.clearCache('timeblocks');
    return response;
  }

  // 進度統計相關 API
  async getUserProgress(): Promise<ApiResponse<UserProgress>> {
    return this.getCached<UserProgress>('/progress', 120000); // 快取 2 分鐘
  }

  async recordFocusSession(minutes: number, taskId?: UUID): Promise<ApiResponse<UserProgress>> {
    const response = await this.post<UserProgress>('/progress/focus-session', { minutes, taskId });
    this.clearCache('progress');
    return response;
  }

  async recordTaskCompletion(taskId: UUID): Promise<ApiResponse<UserProgress>> {
    const response = await this.post<UserProgress>('/progress/task-completion', { taskId });
    this.clearCache('progress');
    return response;
  }
}

// 創建單例實例
export const apiClient = new ApiClient();

// 設置預設攔截器
apiClient.addRequestInterceptor({
  onRequest: (config) => {
    console.log(`發送 API 請求: ${config.method || 'GET'}`, config);
    return config;
  },
  onRequestError: async (error) => {
    console.error('請求攔截器錯誤:', error);
    throw error;
  }
});

apiClient.addResponseInterceptor({
  onResponse: (response) => {
    console.log('收到 API 回應:', response);
    return response;
  },
  onResponseError: async (error) => {
    console.error('回應攔截器錯誤:', error);
    
    // 處理 401 錯誤 (未授權)
    if (error instanceof ApiError && error.status === 401) {
      console.warn('認證失效，需要重新登入');
      // 這裡可以觸發登出流程
      // 例如：清除 token、重定向到登入頁面等
    }
    
    throw error;
  }
});

export default apiClient;