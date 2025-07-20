import { useState, useEffect, useCallback, useMemo } from 'react';

export interface PerformanceMetrics {
  renderCount: number;
  lastRenderTime: number;
  averageRenderTime: number;
  memoryUsage?: number;
}

// 效能監控 Hook
export const usePerformanceMonitor = (componentName: string) => {
  const [metrics, setMetrics] = useState<PerformanceMetrics>({
    renderCount: 0,
    lastRenderTime: 0,
    averageRenderTime: 0,
  });

  const startTime = useMemo(() => performance.now(), []);

  useEffect(() => {
    const endTime = performance.now();
    const renderTime = endTime - startTime;

    setMetrics(prev => {
      const newRenderCount = prev.renderCount + 1;
      const newAverageRenderTime = 
        (prev.averageRenderTime * prev.renderCount + renderTime) / newRenderCount;

      return {
        renderCount: newRenderCount,
        lastRenderTime: renderTime,
        averageRenderTime: newAverageRenderTime,
        memoryUsage: (performance as any).memory?.usedJSHeapSize,
      };
    });

    // 開發模式下記錄效能
    if (process.env.NODE_ENV === 'development') {
      console.log(`[Performance] ${componentName} - Render time: ${renderTime.toFixed(2)}ms`);
      
      // 警告慢渲染
      if (renderTime > 16) { // 60fps threshold
        console.warn(`[Performance Warning] ${componentName} slow render: ${renderTime.toFixed(2)}ms`);
      }
    }
  });

  return metrics;
};

// 虛擬化閾值檢測
export const useVirtualizationThreshold = (itemCount: number, itemHeight: number = 50) => {
  return useMemo(() => {
    const estimatedHeight = itemCount * itemHeight;
    const viewportHeight = window.innerHeight;
    
    // 如果預估高度超過 3 倍視窗高度，建議虛擬化
    const shouldVirtualize = estimatedHeight > viewportHeight * 3 || itemCount > 100;
    
    return {
      shouldVirtualize,
      estimatedHeight,
      itemCount,
      recommendation: shouldVirtualize ? 'virtualize' : 'standard',
    };
  }, [itemCount, itemHeight]);
};

// Debounced 搜尋 Hook
export const useDebouncedSearch = <T>(
  items: T[],
  searchTerm: string,
  searchFn: (item: T, term: string) => boolean,
  delay: number = 300
) => {
  const [debouncedTerm, setDebouncedTerm] = useState(searchTerm);
  
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedTerm(searchTerm);
    }, delay);
    
    return () => clearTimeout(timer);
  }, [searchTerm, delay]);
  
  const filteredItems = useMemo(() => {
    if (!debouncedTerm.trim()) return items;
    return items.filter(item => searchFn(item, debouncedTerm));
  }, [items, debouncedTerm, searchFn]);
  
  return {
    filteredItems,
    isSearching: searchTerm !== debouncedTerm,
    searchTerm: debouncedTerm,
  };
};

// 記憶化回調 Hook
export const useMemoizedCallback = <T extends (...args: any[]) => any>(
  callback: T,
  deps: React.DependencyList
): T => {
  return useCallback(callback, deps);
};

// 批量更新 Hook
export const useBatchedUpdates = () => {
  const [pendingUpdates, setPendingUpdates] = useState<(() => void)[]>([]);
  
  const addUpdate = useCallback((updateFn: () => void) => {
    setPendingUpdates(prev => [...prev, updateFn]);
  }, []);
  
  const executeBatch = useCallback(() => {
    pendingUpdates.forEach(update => update());
    setPendingUpdates([]);
  }, [pendingUpdates]);
  
  // 在下一個微任務中執行批量更新
  useEffect(() => {
    if (pendingUpdates.length > 0) {
      const timer = setTimeout(executeBatch, 0);
      return () => clearTimeout(timer);
    }
  }, [pendingUpdates, executeBatch]);
  
  return { addUpdate, executeBatch, pendingCount: pendingUpdates.length };
};

// 視窗大小監控 Hook
export const useViewportSize = () => {
  const [size, setSize] = useState({
    width: typeof window !== 'undefined' ? window.innerWidth : 1024,
    height: typeof window !== 'undefined' ? window.innerHeight : 768,
  });
  
  useEffect(() => {
    if (typeof window === 'undefined') return;
    
    const handleResize = () => {
      setSize({
        width: window.innerWidth,
        height: window.innerHeight,
      });
    };
    
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);
  
  return size;
};

// 記憶體使用監控
export const useMemoryMonitor = () => {
  const [memoryInfo, setMemoryInfo] = useState<{
    used: number;
    total: number;
    percentage: number;
  } | null>(null);
  
  useEffect(() => {
    const updateMemoryInfo = () => {
      if ('memory' in performance) {
        const memory = (performance as any).memory;
        const used = memory.usedJSHeapSize;
        const total = memory.totalJSHeapSize;
        const percentage = (used / total) * 100;
        
        setMemoryInfo({ used, total, percentage });
        
        // 記憶體使用警告
        if (percentage > 80) {
          console.warn(`[Memory Warning] High memory usage: ${percentage.toFixed(1)}%`);
        }
      }
    };
    
    updateMemoryInfo();
    const interval = setInterval(updateMemoryInfo, 5000); // 每 5 秒檢查一次
    
    return () => clearInterval(interval);
  }, []);
  
  return memoryInfo;
};

export default {
  usePerformanceMonitor,
  useVirtualizationThreshold,
  useDebouncedSearch,
  useMemoizedCallback,
  useBatchedUpdates,
  useViewportSize,
  useMemoryMonitor,
};