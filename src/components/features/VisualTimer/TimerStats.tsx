import React from 'react';
import clsx from 'clsx';
import { useTimerStore } from '@/stores/useTimerStore';

interface TimerStatsProps {
  className?: string;
}

export const TimerStats: React.FC<TimerStatsProps> = React.memo(({ className }) => {
  const { statistics } = useTimerStore();

  return (
    <div className={clsx('grid grid-cols-2 gap-4', className)}>
      <div className="text-center p-4 bg-white rounded-lg shadow">
        <div className="text-2xl font-bold text-blue-600">
          {statistics.todayCompletedSessions}
        </div>
        <div className="text-sm text-gray-600">Sessions Today</div>
      </div>
      
      <div className="text-center p-4 bg-white rounded-lg shadow">
        <div className="text-2xl font-bold text-green-600">
          {statistics.todayFocusMinutes}
        </div>
        <div className="text-sm text-gray-600">Focus Minutes</div>
      </div>
      
      <div className="text-center p-4 bg-white rounded-lg shadow">
        <div className="text-2xl font-bold text-purple-600">
          {Math.round(statistics.weeklyFocusMinutes / 60)}
        </div>
        <div className="text-sm text-gray-600">Hours This Week</div>
      </div>
      
      <div className="text-center p-4 bg-white rounded-lg shadow">
        <div className="text-2xl font-bold text-orange-600">
          {statistics.totalSessions}
        </div>
        <div className="text-sm text-gray-600">Total Sessions</div>
      </div>
    </div>
  );
});