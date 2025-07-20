import React, { useEffect, useCallback } from 'react';
import { useAppReady, useGlobalErrors, useNetworkStatus, clearAllErrors } from './index';

interface StoreProviderProps {
  children: React.ReactNode;
  onError?: (error: string) => void;
  fallback?: React.ReactNode;
  errorBoundary?: React.ComponentType<{ error: string; retry: () => void }>;
}

/**
 * Store Provider 元件
 * 負責管理應用狀態的初始化和錯誤處理
 */
export const StoreProvider: React.FC<StoreProviderProps> = ({
  children,
  onError,
  fallback,
  errorBoundary: ErrorBoundary
}) => {
  const { isReady, isLoading, hasError, error } = useAppReady();
  const { hasErrors, firstError } = useGlobalErrors();

  // 處理錯誤
  useEffect(() => {
    if (hasErrors && firstError && onError) {
      onError(firstError.message);
    }
  }, [hasErrors, firstError, onError]);

  // 重試機制
  const handleRetry = useCallback(() => {
    clearAllErrors();
    window.location.reload();
  }, []);

  // 載入中狀態
  if (isLoading) {
    return fallback || (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
          <p className="text-gray-600">正在初始化應用...</p>
        </div>
      </div>
    );
  }

  // 錯誤狀態
  if (hasError && error) {
    if (ErrorBoundary) {
      return <ErrorBoundary error={error} retry={handleRetry} />;
    }

    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center max-w-md">
          <div className="text-red-500 text-6xl mb-4">⚠️</div>
          <h2 className="text-2xl font-bold text-gray-800 mb-2">應用初始化失敗</h2>
          <p className="text-gray-600 mb-6">{error}</p>
          <button
            onClick={handleRetry}
            className="bg-primary text-white px-6 py-2 rounded-lg hover:bg-primary-dark transition-colors"
          >
            重新載入
          </button>
        </div>
      </div>
    );
  }

  // 正常狀態
  if (isReady) {
    return <>{children}</>;
  }

  // 預設載入狀態
  return fallback || (
    <div className="flex items-center justify-center min-h-screen">
      <div className="text-center">
        <div className="animate-pulse h-8 w-48 bg-gray-200 rounded mx-auto mb-4"></div>
        <div className="animate-pulse h-4 w-32 bg-gray-200 rounded mx-auto"></div>
      </div>
    </div>
  );
};

/**
 * 預設錯誤邊界元件
 */
export const DefaultErrorBoundary: React.FC<{ error: string; retry: () => void }> = ({
  error,
  retry
}) => (
  <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
    <div className="bg-white rounded-lg shadow-lg p-8 max-w-md w-full text-center">
      <div className="text-red-500 text-5xl mb-4">💥</div>
      <h1 className="text-2xl font-bold text-gray-800 mb-2">糟糕！出現錯誤了</h1>
      <p className="text-gray-600 mb-6 text-sm leading-relaxed">
        {error}
      </p>
      <div className="space-y-3">
        <button
          onClick={retry}
          className="w-full bg-primary text-white py-2 px-4 rounded-lg hover:bg-primary-dark transition-colors"
        >
          重新載入應用
        </button>
        <button
          onClick={() => window.location.href = '/'}
          className="w-full bg-gray-200 text-gray-700 py-2 px-4 rounded-lg hover:bg-gray-300 transition-colors"
        >
          回到首頁
        </button>
      </div>
    </div>
  </div>
);

/**
 * 載入指示器元件
 */
export const LoadingIndicator: React.FC<{ message?: string }> = ({ 
  message = "載入中..." 
}) => (
  <div className="flex items-center justify-center py-8">
    <div className="text-center">
      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-3"></div>
      <p className="text-gray-600 text-sm">{message}</p>
    </div>
  </div>
);

/**
 * 網路狀態指示器
 */
export const NetworkStatusIndicator: React.FC = () => {
  const { isOnline } = useNetworkStatus();

  if (isOnline) return null;

  return (
    <div className="fixed top-0 left-0 right-0 bg-yellow-500 text-white text-center py-2 text-sm z-50">
      <span className="inline-flex items-center">
        <span className="w-2 h-2 bg-white rounded-full mr-2 animate-pulse"></span>
        網路連線中斷，正在嘗試重新連線...
      </span>
    </div>
  );
};

/**
 * 全域錯誤顯示元件
 */
export const GlobalErrorDisplay: React.FC = () => {
  const { hasErrors, errors } = useGlobalErrors();

  if (!hasErrors) return null;

  return (
    <div className="fixed bottom-4 right-4 z-50 space-y-2">
      {errors.slice(0, 3).map((error, index) => 
        error && typeof error === 'object' && 'source' in error ? (
        <div
          key={index}
          className="bg-red-500 text-white px-4 py-3 rounded-lg shadow-lg max-w-sm"
        >
          <div className="flex items-start justify-between">
            <div>
              <p className="font-medium text-xs opacity-75 uppercase">
                {error.source}
              </p>
              <p className="text-sm">{error.message}</p>
            </div>
            <button
              onClick={clearAllErrors}
              className="ml-2 text-white hover:text-gray-200"
            >
              ✕
            </button>
          </div>
        </div>
        ) : null
      )}
    </div>
  );
};

export default StoreProvider;