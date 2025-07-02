import React from 'react';
import { PriorityMatrix } from '@/components/features/PriorityMatrix';

export const TasksPage: React.FC = () => {
  return (
    <div className="p-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Tasks</h1>
        <p className="text-gray-600">Manage and prioritize your tasks using the ADHD-optimized matrix system.</p>
      </div>
      
      <PriorityMatrix showFilters />
    </div>
  );
};

export default TasksPage;