import React from 'react';
import { useTaskStore } from '@/stores/useTaskStore';
import { useTimerStore } from '@/stores/useTimerStore';
import { VisualTimer } from '@/components/features/VisualTimer';
import { QuickCapture } from '@/components/features/QuickCapture';
import { PriorityMatrix } from '@/components/features/PriorityMatrix';
import { TaskCard, ProgressCard } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { 
  Plus, 
  Clock, 
  CheckCircle, 
  Target, 
  Zap,
  TrendingUp,
  Calendar,
  Inbox
} from 'lucide-react';

export const DashboardPage: React.FC = () => {
  const { tasks, getTasksForToday, getOverdueTasks } = useTaskStore();
  const { statistics, isRunning } = useTimerStore();

  const todayTasks = getTasksForToday();
  const overdueTasks = getOverdueTasks();
  const activeTasks = tasks.filter(task => task.status === 'active');
  const completedToday = tasks.filter(task => 
    task.status === 'completed' && 
    task.completedAt &&
    new Date(task.completedAt).toDateString() === new Date().toDateString()
  );

  return (
    <div className="p-6 space-y-8">
      {/* Welcome Section */}
      <div className="bg-gradient-to-r from-indigo-500 to-purple-600 rounded-xl p-6 text-white">
        <h1 className="text-2xl font-bold mb-2">Welcome back! 👋</h1>
        <p className="text-indigo-100">
          Ready to tackle your day? You've got {activeTasks.length} active tasks waiting.
        </p>
      </div>

      {/* Quick Stats */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <ProgressCard
          value={completedToday.length}
          label="Tasks Completed Today"
          icon={<CheckCircle className="w-6 h-6 text-green-600" />}
        />
        <ProgressCard
          value={statistics.todayFocusMinutes}
          label="Focus Minutes Today"
          icon={<Clock className="w-6 h-6 text-blue-600" />}
        />
        <ProgressCard
          value={todayTasks.length}
          label="Tasks Scheduled Today"
          icon={<Calendar className="w-6 h-6 text-purple-600" />}
        />
        <ProgressCard
          value={overdueTasks.length}
          label="Overdue Tasks"
          icon={<Target className="w-6 h-6 text-red-600" />}
        />
      </div>

      {/* Main Content Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Left Column */}
        <div className="lg:col-span-2 space-y-8">
          {/* Quick Capture */}
          <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-200">
            <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <Inbox className="w-5 h-5" />
              Quick Capture
            </h2>
            <QuickCapture />
          </div>

          {/* Priority Matrix */}
          <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-200">
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-lg font-semibold text-gray-900 flex items-center gap-2">
                <Target className="w-5 h-5" />
                Priority Matrix
              </h2>
              <Button variant="outline" size="sm">
                View Full Matrix
              </Button>
            </div>
            <PriorityMatrix compact />
          </div>

          {/* Today's Tasks */}
          <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-200">
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-lg font-semibold text-gray-900 flex items-center gap-2">
                <Calendar className="w-5 h-5" />
                Today's Schedule
              </h2>
              <Button variant="outline" size="sm" icon={<Plus className="w-4 h-4" />}>
                Add Task
              </Button>
            </div>
            
            {todayTasks.length > 0 ? (
              <div className="space-y-3">
                {todayTasks.slice(0, 5).map(task => (
                  <TaskCard
                    key={task.id}
                    title={task.title}
                    description={task.description}
                    priority={task.priority}
                    status={task.status as any}
                    energyLevel={task.energyLevel}
                    estimatedTime={task.estimatedMinutes}
                    tags={task.tags}
                  />
                ))}
                {todayTasks.length > 5 && (
                  <div className="text-center pt-4">
                    <Button variant="ghost" size="sm">
                      View {todayTasks.length - 5} more tasks
                    </Button>
                  </div>
                )}
              </div>
            ) : (
              <div className="text-center py-8 text-gray-500">
                <Calendar className="w-12 h-12 mx-auto mb-4 opacity-50" />
                <p>No tasks scheduled for today</p>
                <Button variant="outline" size="sm" className="mt-3">
                  Schedule a task
                </Button>
              </div>
            )}
          </div>
        </div>

        {/* Right Column */}
        <div className="space-y-8">
          {/* Timer Widget */}
          <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-200">
            <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <Zap className="w-5 h-5" />
              Focus Timer
            </h2>
            <VisualTimer showSettings />
          </div>

          {/* Energy Level Tracker */}
          <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-200">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">
              Energy Level
            </h2>
            <div className="space-y-3">
              {['High', 'Medium', 'Low', 'Depleted'].map((level, index) => (
                <button
                  key={level}
                  className="w-full text-left p-3 rounded-lg border border-gray-200 hover:bg-gray-50 transition-colors"
                >
                  <div className="flex items-center justify-between">
                    <span className="font-medium">{level}</span>
                    <div className={`w-3 h-3 rounded-full ${
                      index === 0 ? 'bg-green-500' :
                      index === 1 ? 'bg-yellow-500' :
                      index === 2 ? 'bg-orange-500' : 'bg-red-500'
                    }`} />
                  </div>
                </button>
              ))}
            </div>
          </div>

          {/* Quick Actions */}
          <div className="bg-white rounded-xl p-6 shadow-sm border border-gray-200">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">
              Quick Actions
            </h2>
            <div className="space-y-2">
              <Button variant="outline" size="sm" fullWidth icon={<Plus className="w-4 h-4" />}>
                New Task
              </Button>
              <Button variant="outline" size="sm" fullWidth icon={<Clock className="w-4 h-4" />}>
                Time Block
              </Button>
              <Button variant="outline" size="sm" fullWidth icon={<Target className="w-4 h-4" />}>
                Review Goals
              </Button>
              <Button variant="outline" size="sm" fullWidth icon={<TrendingUp className="w-4 h-4" />}>
                View Progress
              </Button>
            </div>
          </div>

          {/* Overdue Tasks Alert */}
          {overdueTasks.length > 0 && (
            <div className="bg-red-50 border border-red-200 rounded-xl p-6">
              <h3 className="text-red-800 font-semibold mb-2">
                ⚠️ {overdueTasks.length} Overdue Task{overdueTasks.length > 1 ? 's' : ''}
              </h3>
              <p className="text-red-700 text-sm mb-3">
                You have tasks that need immediate attention.
              </p>
              <Button variant="danger" size="sm">
                Review Overdue
              </Button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default DashboardPage;