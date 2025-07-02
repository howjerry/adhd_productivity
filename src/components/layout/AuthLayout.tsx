import React from 'react';
import { Brain } from 'lucide-react';

interface AuthLayoutProps {
  children: React.ReactNode;
}

export const AuthLayout: React.FC<AuthLayoutProps> = ({ children }) => {
  return (
    <div className="min-h-screen bg-gradient-to-br from-indigo-50 via-white to-purple-50 flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        {/* Header */}
        <div className="text-center mb-8">
          <div className="flex items-center justify-center w-16 h-16 bg-indigo-600 rounded-xl mx-auto mb-4">
            <Brain className="w-8 h-8 text-white" />
          </div>
          <h1 className="text-3xl font-bold text-gray-900 mb-2">
            ADHD Productivity System
          </h1>
          <p className="text-gray-600">
            A productivity system designed specifically for ADHD minds
          </p>
        </div>

        {/* Content */}
        <div className="bg-white rounded-xl shadow-lg border border-gray-200 p-8">
          {children}
        </div>

        {/* Footer */}
        <div className="text-center mt-8">
          <p className="text-sm text-gray-500">
            Built with ❤️ for neurodivergent productivity
          </p>
        </div>
      </div>
    </div>
  );
};

export default AuthLayout;