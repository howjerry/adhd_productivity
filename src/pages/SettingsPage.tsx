import React from 'react';

export const SettingsPage: React.FC = () => {
  return (
    <div className="p-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Settings</h1>
        <p className="text-gray-600">Customize your ADHD productivity system.</p>
      </div>
      
      <div className="bg-white rounded-xl p-8 shadow-sm border border-gray-200 text-center">
        <p className="text-gray-500">Settings panel coming soon...</p>
      </div>
    </div>
  );
};

export default SettingsPage;