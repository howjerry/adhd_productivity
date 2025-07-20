import React from 'react';
import { TimerStats } from '@/components/features/VisualTimer/TimerStats';

export const StatsPage: React.FC = () => {
  return (
    <div className="p-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Statistics</h1>
        <p className="text-gray-600">Track your productivity and progress over time.</p>
      </div>
      
      <div className="space-y-6">
        <TimerStats />
        
        <div className="bg-white rounded-xl p-8 shadow-sm border border-gray-200 text-center">
          <p className="text-gray-500">Detailed analytics coming soon...</p>
        </div>
      </div>
    </div>
  );
};

export default StatsPage;