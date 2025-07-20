import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Activity, HardDrive, Zap, Clock, ChevronDown, ChevronUp } from 'lucide-react';
import { usePerformanceMonitor, useMemoryMonitor, useViewportSize } from '@/hooks/usePerformanceOptimization';

interface PerformancePanelProps {
  componentName?: string;
  showDetails?: boolean;
}

export const PerformancePanel: React.FC<PerformancePanelProps> = ({
  componentName = 'App',
  showDetails = false,
}) => {
  const [isExpanded, setIsExpanded] = useState(showDetails);
  const metrics = usePerformanceMonitor(componentName);
  const memoryInfo = useMemoryMonitor();
  const { width, height } = useViewportSize();

  // 只在開發環境顯示
  if (process.env.NODE_ENV !== 'development') {
    return null;
  }

  const formatBytes = (bytes: number) => {
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    if (bytes === 0) return '0 Bytes';
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round((bytes / Math.pow(1024, i)) * 100) / 100 + ' ' + sizes[i];
  };

  const getPerformanceColor = (renderTime: number) => {
    if (renderTime < 8) return 'text-green-600';
    if (renderTime < 16) return 'text-yellow-600';
    return 'text-red-600';
  };

  const getMemoryColor = (percentage: number) => {
    if (percentage < 60) return 'text-green-600';
    if (percentage < 80) return 'text-yellow-600';
    return 'text-red-600';
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      className="fixed bottom-4 right-4 bg-white border border-gray-200 rounded-lg shadow-lg z-50 min-w-64"
    >
      {/* Header */}
      <div
        className="flex items-center justify-between p-3 cursor-pointer hover:bg-gray-50"
        onClick={() => setIsExpanded(!isExpanded)}
      >
        <div className="flex items-center space-x-2">
          <Activity className="w-4 h-4 text-blue-600" />
          <span className="text-sm font-medium text-gray-900">效能監控</span>
        </div>
        <div className="flex items-center space-x-2">
          <div className={`w-2 h-2 rounded-full ${
            metrics.lastRenderTime < 16 ? 'bg-green-500' : 
            metrics.lastRenderTime < 32 ? 'bg-yellow-500' : 'bg-red-500'
          }`} />
          {isExpanded ? (
            <ChevronUp className="w-4 h-4 text-gray-400" />
          ) : (
            <ChevronDown className="w-4 h-4 text-gray-400" />
          )}
        </div>
      </div>

      {/* Expanded Content */}
      <AnimatePresence>
        {isExpanded && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.2 }}
            className="border-t border-gray-200"
          >
            <div className="p-3 space-y-3">
              {/* Render Performance */}
              <div className="space-y-2">
                <div className="flex items-center space-x-2">
                  <Clock className="w-3 h-3 text-gray-500" />
                  <span className="text-xs font-medium text-gray-700">渲染效能</span>
                </div>
                <div className="space-y-1">
                  <div className="flex justify-between text-xs">
                    <span className="text-gray-600">上次渲染</span>
                    <span className={getPerformanceColor(metrics.lastRenderTime)}>
                      {metrics.lastRenderTime.toFixed(2)}ms
                    </span>
                  </div>
                  <div className="flex justify-between text-xs">
                    <span className="text-gray-600">平均渲染</span>
                    <span className={getPerformanceColor(metrics.averageRenderTime)}>
                      {metrics.averageRenderTime.toFixed(2)}ms
                    </span>
                  </div>
                  <div className="flex justify-between text-xs">
                    <span className="text-gray-600">渲染次數</span>
                    <span className="text-gray-900">{metrics.renderCount}</span>
                  </div>
                </div>
              </div>

              {/* Memory Usage */}
              {memoryInfo && (
                <div className="space-y-2">
                  <div className="flex items-center space-x-2">
                    <HardDrive className="w-3 h-3 text-gray-500" />
                    <span className="text-xs font-medium text-gray-700">記憶體使用</span>
                  </div>
                  <div className="space-y-1">
                    <div className="flex justify-between text-xs">
                      <span className="text-gray-600">已使用</span>
                      <span className="text-gray-900">
                        {formatBytes(memoryInfo.used)}
                      </span>
                    </div>
                    <div className="flex justify-between text-xs">
                      <span className="text-gray-600">使用率</span>
                      <span className={getMemoryColor(memoryInfo.percentage)}>
                        {memoryInfo.percentage.toFixed(1)}%
                      </span>
                    </div>
                    {/* Memory Usage Bar */}
                    <div className="w-full bg-gray-200 rounded-full h-1">
                      <div
                        className={`h-1 rounded-full ${
                          memoryInfo.percentage < 60 ? 'bg-green-500' :
                          memoryInfo.percentage < 80 ? 'bg-yellow-500' : 'bg-red-500'
                        }`}
                        style={{ width: `${Math.min(memoryInfo.percentage, 100)}%` }}
                      />
                    </div>
                  </div>
                </div>
              )}

              {/* Viewport */}
              <div className="space-y-2">
                <div className="flex items-center space-x-2">
                  <Zap className="w-3 h-3 text-gray-500" />
                  <span className="text-xs font-medium text-gray-700">視窗資訊</span>
                </div>
                <div className="flex justify-between text-xs">
                  <span className="text-gray-600">尺寸</span>
                  <span className="text-gray-900">{width} × {height}</span>
                </div>
              </div>

              {/* Performance Recommendations */}
              {(metrics.averageRenderTime > 16 || (memoryInfo && memoryInfo.percentage > 80)) && (
                <div className="p-2 bg-yellow-50 border border-yellow-200 rounded text-xs">
                  <div className="font-medium text-yellow-800 mb-1">效能建議</div>
                  <div className="text-yellow-700 space-y-1">
                    {metrics.averageRenderTime > 16 && (
                      <div>• 考慮使用虛擬滾動或分頁</div>
                    )}
                    {memoryInfo && memoryInfo.percentage > 80 && (
                      <div>• 記憶體使用過高，建議釋放未使用資源</div>
                    )}
                  </div>
                </div>
              )}
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.div>
  );
};

export default PerformancePanel;