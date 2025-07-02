import React from 'react';

export const CalendarPage: React.FC = () => {
  return (
    <div className="p-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Calendar</h1>
        <p className="text-gray-600">Schedule your time blocks and manage your calendar.</p>
      </div>
      
      <div className="bg-white rounded-xl p-8 shadow-sm border border-gray-200 text-center">
        <p className="text-gray-500">Calendar view coming soon...</p>
      </div>
    </div>
  );
};

export default CalendarPage;