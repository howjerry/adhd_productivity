import React from 'react';
import { QuickCapture } from '@/components/features/QuickCapture';

export const CapturePage: React.FC = () => {
  return (
    <div className="p-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Capture</h1>
        <p className="text-gray-600">Quickly capture thoughts, ideas, and tasks without friction.</p>
      </div>
      
      <div className="max-w-2xl mx-auto">
        <QuickCapture />
      </div>
    </div>
  );
};

export default CapturePage;