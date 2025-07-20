import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { Sidebar } from '../Sidebar';
import { useTaskStore } from '@/stores/useTaskStore';
import { useTimerStore } from '@/stores/useTimerStore';
import type { Task, TaskStatus } from '@/types';

// Mock stores
vi.mock('@/stores/useTaskStore', () => ({
  useTaskStore: vi.fn(),
}));

vi.mock('@/stores/useTimerStore', () => ({
  useTimerStore: vi.fn(),
}));

// Mock react-router-dom location
const mockLocation = { pathname: '/dashboard' };
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useLocation: () => mockLocation,
  };
});

// Helper component to wrap Sidebar with Router
const SidebarWrapper = (props: any) => (
  <BrowserRouter>
    <Sidebar {...props} />
  </BrowserRouter>
);

describe('Sidebar', () => {
  const mockTasks: Task[] = [
    {
      id: 'task-1',
      userId: 'user-1',
      title: 'Active Task 1',
      status: 'active' as TaskStatus,
      priority: 'high' as any,
      tags: [],
      subtasks: [],
      timeBlocks: [],
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    },
    {
      id: 'task-2',
      userId: 'user-1',
      title: 'Active Task 2',
      status: 'active' as TaskStatus,
      priority: 'medium' as any,
      tags: [],
      subtasks: [],
      timeBlocks: [],
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    },
    {
      id: 'task-3',
      userId: 'user-1',
      title: 'Today Task',
      status: 'active' as TaskStatus,
      priority: 'low' as any,
      scheduledDate: new Date().toISOString(),
      tags: [],
      subtasks: [],
      timeBlocks: [],
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    },
    {
      id: 'task-4',
      userId: 'user-1',
      title: 'Completed Task',
      status: 'completed' as TaskStatus,
      priority: 'medium' as any,
      tags: [],
      subtasks: [],
      timeBlocks: [],
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    },
  ];

  const mockOnToggle = vi.fn();
  const mockOnCollapse = vi.fn();
  const mockOnClose = vi.fn();

  const defaultProps = {
    isOpen: true,
    isCollapsed: false,
    onToggle: mockOnToggle,
    onCollapse: mockOnCollapse,
    onClose: mockOnClose,
  };

  beforeEach(() => {
    vi.mocked(useTaskStore).mockReturnValue({
      tasks: mockTasks,
    });

    vi.mocked(useTimerStore).mockReturnValue({
      isRunning: false,
      sessionCount: 0,
    });

    // Mock window.innerWidth for mobile detection
    Object.defineProperty(window, 'innerWidth', {
      writable: true,
      configurable: true,
      value: 1024,
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('基本渲染', () => {
    it('應該正確渲染側邊欄', () => {
      render(<SidebarWrapper {...defaultProps} />);
      
      expect(screen.getByText('ADHD Focus')).toBeInTheDocument();
      expect(screen.getByText('Productivity System')).toBeInTheDocument();
    });

    it('應該渲染所有導航項目', () => {
      render(<SidebarWrapper {...defaultProps} />);
      
      expect(screen.getByText('Dashboard')).toBeInTheDocument();
      expect(screen.getByText('Tasks')).toBeInTheDocument();
      expect(screen.getByText('Calendar')).toBeInTheDocument();
      expect(screen.getByText('Capture')).toBeInTheDocument();
      expect(screen.getByText('Statistics')).toBeInTheDocument();
      expect(screen.getByText('Settings')).toBeInTheDocument();
    });

    it('應該顯示導航項目的描述', () => {
      render(<SidebarWrapper {...defaultProps} />);
      
      expect(screen.getByText('Overview and quick actions')).toBeInTheDocument();
      expect(screen.getByText('Manage your tasks')).toBeInTheDocument();
      expect(screen.getByText('Schedule and time blocks')).toBeInTheDocument();
    });

    it('應該顯示 Quick Focus 區塊', () => {
      render(<SidebarWrapper {...defaultProps} />);
      
      expect(screen.getByText('Quick Focus')).toBeInTheDocument();
      expect(screen.getByText('Priority matrix view')).toBeInTheDocument();
    });

    it('應該顯示版本資訊', () => {
      render(<SidebarWrapper {...defaultProps} />);
      
      expect(screen.getByText('v1.0.0 • Built for ADHD minds')).toBeInTheDocument();
    });
  });

  describe('摺疊狀態', () => {
    it('應該在摺疊時隱藏文字內容', () => {
      render(<SidebarWrapper {...defaultProps} isCollapsed={true} />);
      
      expect(screen.queryByText('ADHD Focus')).not.toBeInTheDocument();
      expect(screen.queryByText('Productivity System')).not.toBeInTheDocument();
      expect(screen.queryByText('Overview and quick actions')).not.toBeInTheDocument();
    });

    it('應該在摺疊時顯示狀態指示器', () => {
      render(<SidebarWrapper {...defaultProps} isCollapsed={true} />);
      
      const statusIndicator = screen.getByTitle('System online');
      expect(statusIndicator).toBeInTheDocument();
    });

    it('應該在摺疊時隱藏摺疊按鈕', () => {
      render(<SidebarWrapper {...defaultProps} isCollapsed={true} />);
      
      const collapseButton = screen.getByLabelText('Expand sidebar');
      expect(collapseButton).toHaveClass('hidden');
    });

    it('應該在點擊摺疊按鈕時呼叫 onCollapse', async () => {
      const user = userEvent.setup();
      
      render(<SidebarWrapper {...defaultProps} />);
      
      await user.click(screen.getByLabelText('Collapse sidebar'));
      
      expect(mockOnCollapse).toHaveBeenCalledTimes(1);
    });
  });

  describe('任務徽章顯示', () => {
    it('應該顯示活躍任務數量徽章', () => {
      render(<SidebarWrapper {...defaultProps} />);
      
      // 檢查 Tasks 項目有徽章 (2個活躍任務)
      const taskBadges = screen.getAllByText('2');
      expect(taskBadges.length).toBeGreaterThan(0);
    });

    it('應該顯示今日任務數量徽章', () => {
      render(<SidebarWrapper {...defaultProps} />);
      
      // 檢查 Calendar 項目有徽章 (1個今日任務)
      const calendarBadges = screen.getAllByText('1');
      expect(calendarBadges.length).toBeGreaterThan(0);
    });

    it('應該在沒有任務時不顯示徽章', () => {
      vi.mocked(useTaskStore).mockReturnValue({
        tasks: [],
      });

      render(<SidebarWrapper {...defaultProps} />);
      
      // 不應該有任何數字徽章
      expect(screen.queryByText('2')).not.toBeInTheDocument();
      expect(screen.queryByText('1')).not.toBeInTheDocument();
    });

    it('應該在摺疊狀態下顯示工具提示徽章', () => {
      render(<SidebarWrapper {...defaultProps} isCollapsed={true} />);
      
      // 檢查是否有工具提示內容
      const tasksLink = screen.getByRole('link', { name: /tasks/i });
      expect(tasksLink).toHaveAttribute('title', 'Tasks');
    });
  });

  describe('計時器狀態顯示', () => {
    it('應該在計時器運行時顯示計時器狀態', () => {
      vi.mocked(useTimerStore).mockReturnValue({
        isRunning: true,
        sessionCount: 2,
      });

      render(<SidebarWrapper {...defaultProps} />);
      
      expect(screen.getByText('Focus Session')).toBeInTheDocument();
      expect(screen.getByText('Session 3 running')).toBeInTheDocument();
    });

    it('應該在計時器未運行時不顯示計時器狀態', () => {
      render(<SidebarWrapper {...defaultProps} />);
      
      expect(screen.queryByText('Focus Session')).not.toBeInTheDocument();
    });

    it('應該在摺疊狀態下只顯示計時器圖示', () => {
      vi.mocked(useTimerStore).mockReturnValue({
        isRunning: true,
        sessionCount: 2,
      });

      render(<SidebarWrapper {...defaultProps} isCollapsed={true} />);
      
      expect(screen.queryByText('Focus Session')).not.toBeInTheDocument();
      expect(screen.queryByText('Session 3 running')).not.toBeInTheDocument();
    });
  });

  describe('導航活躍狀態', () => {
    it('應該高亮顯示當前頁面', () => {
      mockLocation.pathname = '/dashboard';
      
      render(<SidebarWrapper {...defaultProps} />);
      
      const dashboardLink = screen.getByRole('link', { name: /dashboard/i });
      expect(dashboardLink).toHaveClass('bg-indigo-50', 'text-indigo-700');
    });

    it('應該在非當前頁面使用一般樣式', () => {
      mockLocation.pathname = '/dashboard';
      
      render(<SidebarWrapper {...defaultProps} />);
      
      const tasksLink = screen.getByRole('link', { name: /tasks/i });
      expect(tasksLink).toHaveClass('text-gray-700', 'hover:bg-gray-100');
      expect(tasksLink).not.toHaveClass('bg-indigo-50', 'text-indigo-700');
    });
  });

  describe('行動裝置行為', () => {
    beforeEach(() => {
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 768, // 模擬行動裝置寬度
      });
    });

    it('應該在行動裝置上點擊導航項目時關閉側邊欄', async () => {
      const user = userEvent.setup();
      
      render(<SidebarWrapper {...defaultProps} />);
      
      await user.click(screen.getByRole('link', { name: /tasks/i }));
      
      expect(mockOnClose).toHaveBeenCalledTimes(1);
    });

    it('應該在桌面版點擊導航項目時不關閉側邊欄', async () => {
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 1200, // 桌面版寬度
      });

      const user = userEvent.setup();
      
      render(<SidebarWrapper {...defaultProps} />);
      
      await user.click(screen.getByRole('link', { name: /tasks/i }));
      
      expect(mockOnClose).not.toHaveBeenCalled();
    });
  });

  describe('響應式設計', () => {
    it('應該在關閉狀態下隱藏側邊欄', () => {
      const { container } = render(<SidebarWrapper {...defaultProps} isOpen={false} />);
      
      const sidebar = container.firstChild?.firstChild;
      expect(sidebar).toHaveClass('-translate-x-full', 'lg:translate-x-0');
    });

    it('應該在開啟狀態下顯示側邊欄', () => {
      const { container } = render(<SidebarWrapper {...defaultProps} isOpen={true} />);
      
      const sidebar = container.firstChild?.firstChild;
      expect(sidebar).toHaveClass('translate-x-0');
    });

    it('應該根據摺疊狀態調整寬度', () => {
      const { container, rerender } = render(<SidebarWrapper {...defaultProps} isCollapsed={false} />);
      
      let sidebar = container.firstChild?.firstChild;
      expect(sidebar).toHaveClass('w-64');
      
      rerender(<SidebarWrapper {...defaultProps} isCollapsed={true} />);
      
      sidebar = container.firstChild?.firstChild;
      expect(sidebar).toHaveClass('w-16');
    });
  });

  describe('無障礙功能', () => {
    it('應該有正確的 aria-label', () => {
      render(<SidebarWrapper {...defaultProps} />);
      
      expect(screen.getByLabelText('Collapse sidebar')).toBeInTheDocument();
    });

    it('應該為摺疊狀態的導航項目提供 title 屬性', () => {
      render(<SidebarWrapper {...defaultProps} isCollapsed={true} />);
      
      const tasksLink = screen.getByRole('link', { name: /tasks/i });
      expect(tasksLink).toHaveAttribute('title', 'Tasks');
    });

    it('應該使用語意化的導航結構', () => {
      render(<SidebarWrapper {...defaultProps} />);
      
      expect(screen.getByRole('navigation')).toBeInTheDocument();
    });
  });

  describe('邊界情況', () => {
    it('應該處理大量任務的徽章顯示', () => {
      const manyTasks = Array.from({ length: 99 }, (_, i) => ({
        ...mockTasks[0],
        id: `task-${i}`,
        status: 'active' as TaskStatus,
      }));

      vi.mocked(useTaskStore).mockReturnValue({
        tasks: manyTasks,
      });

      render(<SidebarWrapper {...defaultProps} />);
      
      expect(screen.getByText('99')).toBeInTheDocument();
    });

    it('應該處理沒有 scheduledDate 的任務', () => {
      const tasksWithoutSchedule = mockTasks.map(task => ({
        ...task,
        scheduledDate: undefined,
      }));

      vi.mocked(useTaskStore).mockReturnValue({
        tasks: tasksWithoutSchedule,
      });

      render(<SidebarWrapper {...defaultProps} />);
      
      // 不應該有今日任務徽章
      expect(screen.queryByText('1')).not.toBeInTheDocument();
    });

    it('應該處理高會話數的計時器', () => {
      vi.mocked(useTimerStore).mockReturnValue({
        isRunning: true,
        sessionCount: 99,
      });

      render(<SidebarWrapper {...defaultProps} />);
      
      expect(screen.getByText('Session 100 running')).toBeInTheDocument();
    });
  });

  describe('工具提示功能', () => {
    it('應該在摺疊狀態下顯示工具提示', () => {
      render(<SidebarWrapper {...defaultProps} isCollapsed={true} />);
      
      // 檢查工具提示容器是否存在
      const tooltips = screen.getAllByText('Dashboard');
      expect(tooltips.length).toBeGreaterThan(0);
    });

    it('應該在工具提示中包含徽章資訊', () => {
      render(<SidebarWrapper {...defaultProps} isCollapsed={true} />);
      
      // 在摺疊狀態下，工具提示應該包含徽章資訊
      const tooltipElements = document.querySelectorAll('.absolute.left-full');
      expect(tooltipElements.length).toBeGreaterThan(0);
    });
  });
});