import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MatrixQuadrant } from '../MatrixQuadrant';
import type { Task, Priority, TaskStatus } from '@/types';
import { Clock } from 'lucide-react';

// Mock MatrixTaskCard
vi.mock('../MatrixTaskCard', () => ({
  MatrixTaskCard: ({ task, compact }: { task: Task; compact: boolean }) => (
    <div data-testid={`task-card-${task.id}`} data-compact={compact}>
      {task.title}
    </div>
  ),
}));

// Mock Button component
vi.mock('@/components/ui/Button', () => ({
  Button: ({ icon, onClick, variant, size, ...props }: any) => (
    <button
      onClick={onClick}
      data-variant={variant}
      data-size={size}
      {...props}
    >
      {icon}
    </button>
  ),
}));

describe('MatrixQuadrant', () => {
  const mockQuadrant = {
    id: 'urgent-important',
    title: 'Do First',
    description: 'Urgent and Important',
    icon: <Clock className="w-4 h-4" />,
    urgent: true,
    important: true,
    color: 'red',
  };

  const mockTasks: Task[] = [
    {
      id: 'task-1',
      userId: 'user-1',
      title: 'Urgent Task 1',
      status: 'active' as TaskStatus,
      priority: 'high' as Priority,
      tags: [],
      subtasks: [],
      timeBlocks: [],
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    },
    {
      id: 'task-2',
      userId: 'user-1',
      title: 'Urgent Task 2',
      status: 'active' as TaskStatus,
      priority: 'high' as Priority,
      tags: [],
      subtasks: [],
      timeBlocks: [],
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    },
  ];

  const mockOnTaskMove = vi.fn();
  const mockOnSelect = vi.fn();

  const defaultProps = {
    quadrant: mockQuadrant,
    tasks: mockTasks,
    compact: false,
    onTaskMove: mockOnTaskMove,
    isSelected: false,
    onSelect: mockOnSelect,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('基本渲染', () => {
    it('應該正確渲染象限標題', () => {
      render(<MatrixQuadrant {...defaultProps} />);
      
      expect(screen.getByText('Do First')).toBeInTheDocument();
    });

    it('應該顯示任務數量', () => {
      render(<MatrixQuadrant {...defaultProps} />);
      
      expect(screen.getByText('2')).toBeInTheDocument();
    });

    it('應該渲染動作按鈕', () => {
      render(<MatrixQuadrant {...defaultProps} />);
      
      const buttons = screen.getAllByRole('button');
      expect(buttons).toHaveLength(2); // Plus 和 Filter 按鈕
    });

    it('應該渲染所有任務卡片', () => {
      render(<MatrixQuadrant {...defaultProps} />);
      
      expect(screen.getByTestId('task-card-task-1')).toBeInTheDocument();
      expect(screen.getByTestId('task-card-task-2')).toBeInTheDocument();
      expect(screen.getByText('Urgent Task 1')).toBeInTheDocument();
      expect(screen.getByText('Urgent Task 2')).toBeInTheDocument();
    });

    it('應該應用正確的 CSS 類別', () => {
      const { container } = render(<MatrixQuadrant {...defaultProps} />);
      
      const quadrantElement = container.firstChild;
      expect(quadrantElement).toHaveClass('matrix-quadrant');
      expect(quadrantElement).toHaveClass('quadrant-urgent-important');
    });
  });

  describe('空狀態', () => {
    it('應該在沒有任務時顯示空狀態', () => {
      render(<MatrixQuadrant {...defaultProps} tasks={[]} />);
      
      expect(screen.getByText('No tasks in this quadrant')).toBeInTheDocument();
      expect(screen.getByText('Drag tasks here to categorize')).toBeInTheDocument();
    });

    it('應該在空狀態下顯示象限圖示', () => {
      render(<MatrixQuadrant {...defaultProps} tasks={[]} />);
      
      const emptyIcon = document.querySelector('.empty-icon');
      expect(emptyIcon).toBeInTheDocument();
    });

    it('應該在空狀態下顯示任務數量為 0', () => {
      render(<MatrixQuadrant {...defaultProps} tasks={[]} />);
      
      expect(screen.getByText('0')).toBeInTheDocument();
    });
  });

  describe('拖放功能', () => {
    it('應該在拖放時設置 drag-over 類別', () => {
      const { container } = render(<MatrixQuadrant {...defaultProps} />);
      
      const quadrantElement = container.firstChild as HTMLElement;
      
      fireEvent.dragOver(quadrantElement, {
        preventDefault: vi.fn(),
      });
      
      expect(quadrantElement).toHaveClass('drag-over');
    });

    it('應該在離開拖放區域時移除 drag-over 類別', () => {
      const { container } = render(<MatrixQuadrant {...defaultProps} />);
      
      const quadrantElement = container.firstChild as HTMLElement;
      
      fireEvent.dragOver(quadrantElement, {
        preventDefault: vi.fn(),
      });
      
      expect(quadrantElement).toHaveClass('drag-over');
      
      fireEvent.dragLeave(quadrantElement);
      
      expect(quadrantElement).not.toHaveClass('drag-over');
    });

    it('應該在放下時呼叫 onTaskMove', () => {
      const { container } = render(<MatrixQuadrant {...defaultProps} />);
      
      const quadrantElement = container.firstChild as HTMLElement;
      
      const mockDataTransfer = {
        getData: vi.fn().mockReturnValue('task-123'),
      };
      
      fireEvent.drop(quadrantElement, {
        preventDefault: vi.fn(),
        dataTransfer: mockDataTransfer,
      });
      
      expect(mockOnTaskMove).toHaveBeenCalledWith('task-123', 'urgent-important');
    });

    it('應該在放下時移除 drag-over 類別', () => {
      const { container } = render(<MatrixQuadrant {...defaultProps} />);
      
      const quadrantElement = container.firstChild as HTMLElement;
      
      fireEvent.dragOver(quadrantElement, {
        preventDefault: vi.fn(),
      });
      
      expect(quadrantElement).toHaveClass('drag-over');
      
      const mockDataTransfer = {
        getData: vi.fn().mockReturnValue('task-123'),
      };
      
      fireEvent.drop(quadrantElement, {
        preventDefault: vi.fn(),
        dataTransfer: mockDataTransfer,
      });
      
      expect(quadrantElement).not.toHaveClass('drag-over');
    });

    it('應該在沒有任務 ID 時不呼叫 onTaskMove', () => {
      const { container } = render(<MatrixQuadrant {...defaultProps} />);
      
      const quadrantElement = container.firstChild as HTMLElement;
      
      const mockDataTransfer = {
        getData: vi.fn().mockReturnValue(''),
      };
      
      fireEvent.drop(quadrantElement, {
        preventDefault: vi.fn(),
        dataTransfer: mockDataTransfer,
      });
      
      expect(mockOnTaskMove).not.toHaveBeenCalled();
    });
  });

  describe('選擇功能', () => {
    it('應該在點擊時呼叫 onSelect', async () => {
      const user = userEvent.setup();
      const { container } = render(<MatrixQuadrant {...defaultProps} />);
      
      const quadrantElement = container.firstChild as HTMLElement;
      await user.click(quadrantElement);
      
      expect(mockOnSelect).toHaveBeenCalledTimes(1);
    });

    it('應該在選中時顯示選中樣式', () => {
      const { container } = render(<MatrixQuadrant {...defaultProps} isSelected={true} />);
      
      const quadrantElement = container.firstChild;
      expect(quadrantElement).toHaveClass('selected');
    });

    it('應該在未選中時不顯示選中樣式', () => {
      const { container } = render(<MatrixQuadrant {...defaultProps} isSelected={false} />);
      
      const quadrantElement = container.firstChild;
      expect(quadrantElement).not.toHaveClass('selected');
    });
  });

  describe('緊湊模式', () => {
    it('應該將緊湊模式傳遞給任務卡片', () => {
      render(<MatrixQuadrant {...defaultProps} compact={true} />);
      
      const taskCard = screen.getByTestId('task-card-task-1');
      expect(taskCard).toHaveAttribute('data-compact', 'true');
    });

    it('應該在非緊湊模式下不設置緊湊標記', () => {
      render(<MatrixQuadrant {...defaultProps} compact={false} />);
      
      const taskCard = screen.getByTestId('task-card-task-1');
      expect(taskCard).toHaveAttribute('data-compact', 'false');
    });
  });

  describe('無障礙功能', () => {
    it('應該支援鍵盤導航', async () => {
      const user = userEvent.setup();
      render(<MatrixQuadrant {...defaultProps} />);
      
      const buttons = screen.getAllByRole('button');
      
      await user.tab();
      expect(buttons[0]).toHaveFocus();
      
      await user.tab();
      expect(buttons[1]).toHaveFocus();
    });

    it('應該有適當的角色和標籤', () => {
      render(<MatrixQuadrant {...defaultProps} />);
      
      // 檢查按鈕元素
      const buttons = screen.getAllByRole('button');
      expect(buttons).toHaveLength(2);
    });
  });

  describe('邊界情況', () => {
    it('應該處理極大數量的任務', () => {
      const manyTasks = Array.from({ length: 100 }, (_, i) => ({
        ...mockTasks[0],
        id: `task-${i}`,
        title: `Task ${i}`,
      }));
      
      render(<MatrixQuadrant {...defaultProps} tasks={manyTasks} />);
      
      expect(screen.getByText('100')).toBeInTheDocument();
      expect(screen.getByTestId('task-card-task-0')).toBeInTheDocument();
      expect(screen.getByTestId('task-card-task-99')).toBeInTheDocument();
    });

    it('應該處理沒有圖示的象限', () => {
      const quadrantWithoutIcon = { ...mockQuadrant, icon: null };
      
      render(<MatrixQuadrant {...defaultProps} quadrant={quadrantWithoutIcon} />);
      
      expect(screen.getByText('Do First')).toBeInTheDocument();
    });

    it('應該處理極長的標題', () => {
      const longTitleQuadrant = {
        ...mockQuadrant,
        title: 'This is a very long quadrant title that might overflow the container',
      };
      
      render(<MatrixQuadrant {...defaultProps} quadrant={longTitleQuadrant} />);
      
      expect(screen.getByText(longTitleQuadrant.title)).toBeInTheDocument();
    });
  });

  describe('拖放事件處理', () => {
    it('應該防止預設的 dragOver 行為', () => {
      const { container } = render(<MatrixQuadrant {...defaultProps} />);
      
      const quadrantElement = container.firstChild as HTMLElement;
      
      // 使用 createEvent 並手動模擬事件
      const event = new Event('dragover', { bubbles: true, cancelable: true });
      const preventDefault = vi.spyOn(event, 'preventDefault');
      
      quadrantElement.dispatchEvent(event);
      
      expect(preventDefault).toHaveBeenCalled();
    });

    it('應該防止預設的 drop 行為', () => {
      const { container } = render(<MatrixQuadrant {...defaultProps} />);
      
      const quadrantElement = container.firstChild as HTMLElement;
      
      // 創建拖放事件
      const event = new Event('drop', { bubbles: true, cancelable: true });
      const preventDefault = vi.spyOn(event, 'preventDefault');
      
      // 模擬 dataTransfer
      Object.defineProperty(event, 'dataTransfer', {
        value: {
          getData: vi.fn().mockReturnValue('task-123'),
        },
      });
      
      quadrantElement.dispatchEvent(event);
      
      expect(preventDefault).toHaveBeenCalled();
    });
  });

  describe('效能測試', () => {
    it('應該高效渲染大量任務', () => {
      const manyTasks = Array.from({ length: 50 }, (_, i) => ({
        ...mockTasks[0],
        id: `task-${i}`,
        title: `Task ${i}`,
      }));
      
      const startTime = performance.now();
      
      render(<MatrixQuadrant {...defaultProps} tasks={manyTasks} />);
      
      const endTime = performance.now();
      const renderTime = endTime - startTime;
      
      // 渲染時間應該少於100ms
      expect(renderTime).toBeLessThan(100);
      
      // 所有任務都應該被渲染
      expect(screen.getByText('50')).toBeInTheDocument();
    });
  });
});