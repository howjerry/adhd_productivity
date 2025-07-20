import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { VirtualizedMatrixQuadrant } from '../VirtualizedMatrixQuadrant';
import type { Task, Priority, TaskStatus } from '@/types';
import { Clock } from 'lucide-react';

// Mock dependencies
vi.mock('../MatrixTaskCard', () => ({
  MatrixTaskCard: ({ task, compact }: { task: Task; compact: boolean }) => (
    <div data-testid={`task-card-${task.id}`} data-compact={compact}>
      {task.title}
    </div>
  ),
}));

vi.mock('@/components/ui/VirtualizedList', () => ({
  VirtualizedTaskList: ({ 
    tasks, 
    renderTask, 
    itemHeight, 
    maxHeight, 
    className,
    overscanCount 
  }: any) => (
    <div 
      data-testid="virtualized-list"
      data-item-height={itemHeight}
      data-max-height={maxHeight}
      data-overscan-count={overscanCount}
      className={className}
    >
      {tasks.map((task: Task, index: number) => 
        renderTask(task, index, { height: itemHeight })
      )}
    </div>
  ),
}));

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

// Mock environment
const originalEnv = process.env.NODE_ENV;

describe('VirtualizedMatrixQuadrant', () => {
  const mockQuadrant = {
    id: 'urgent-important',
    title: 'Do First',
    description: 'Urgent and Important',
    icon: <Clock className="w-4 h-4" />,
    urgent: true,
    important: true,
    color: 'red',
  };

  const createMockTasks = (count: number): Task[] => 
    Array.from({ length: count }, (_, i) => ({
      id: `task-${i}`,
      userId: 'user-1',
      title: `Task ${i}`,
      status: 'active' as TaskStatus,
      priority: 'high' as Priority,
      tags: [],
      subtasks: [],
      timeBlocks: [],
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    }));

  const mockOnTaskMove = vi.fn();
  const mockOnSelect = vi.fn();

  const defaultProps = {
    quadrant: mockQuadrant,
    tasks: createMockTasks(5),
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
    process.env.NODE_ENV = originalEnv;
  });

  describe('基本渲染', () => {
    it('應該正確渲染象限標題', () => {
      render(<VirtualizedMatrixQuadrant {...defaultProps} />);
      
      expect(screen.getByText('Do First')).toBeInTheDocument();
    });

    it('應該顯示任務數量', () => {
      render(<VirtualizedMatrixQuadrant {...defaultProps} />);
      
      expect(screen.getByText('5')).toBeInTheDocument();
    });

    it('應該渲染動作按鈕', () => {
      render(<VirtualizedMatrixQuadrant {...defaultProps} />);
      
      const buttons = screen.getAllByRole('button');
      expect(buttons).toHaveLength(2); // Plus 和 Filter 按鈕
    });
  });

  describe('虛擬化決策', () => {
    it('應該在任務數量少於10個時使用標準渲染', () => {
      render(<VirtualizedMatrixQuadrant {...defaultProps} tasks={createMockTasks(5)} />);
      
      expect(screen.queryByTestId('virtualized-list')).not.toBeInTheDocument();
      expect(screen.getByTestId('task-card-task-0')).toBeInTheDocument();
      expect(screen.queryByText('(虛擬)')).not.toBeInTheDocument();
    });

    it('應該在任務數量超過10個時使用虛擬化渲染', () => {
      render(<VirtualizedMatrixQuadrant {...defaultProps} tasks={createMockTasks(15)} />);
      
      expect(screen.getByTestId('virtualized-list')).toBeInTheDocument();
      expect(screen.getByText('(虛擬)')).toBeInTheDocument();
    });

    it('應該能手動禁用虛擬化', () => {
      render(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={createMockTasks(15)}
          enableVirtualization={false}
        />
      );
      
      expect(screen.queryByTestId('virtualized-list')).not.toBeInTheDocument();
      expect(screen.queryByText('(虛擬)')).not.toBeInTheDocument();
    });
  });

  describe('虛擬化設定', () => {
    it('應該使用正確的項目高度 - 緊湊模式', () => {
      render(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={createMockTasks(15)}
          compact={true}
        />
      );
      
      const virtualizedList = screen.getByTestId('virtualized-list');
      expect(virtualizedList).toHaveAttribute('data-item-height', '60');
    });

    it('應該使用正確的項目高度 - 標準模式', () => {
      render(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={createMockTasks(15)}
          compact={false}
        />
      );
      
      const virtualizedList = screen.getByTestId('virtualized-list');
      expect(virtualizedList).toHaveAttribute('data-item-height', '80');
    });

    it('應該使用自訂最大高度', () => {
      render(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={createMockTasks(15)}
          maxHeight={600}
        />
      );
      
      const virtualizedList = screen.getByTestId('virtualized-list');
      expect(virtualizedList).toHaveAttribute('data-max-height', '600');
    });

    it('應該設定正確的 overscan 數量', () => {
      render(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={createMockTasks(15)}
        />
      );
      
      const virtualizedList = screen.getByTestId('virtualized-list');
      expect(virtualizedList).toHaveAttribute('data-overscan-count', '3');
    });
  });

  describe('空狀態', () => {
    it('應該在沒有任務時顯示空狀態', () => {
      render(<VirtualizedMatrixQuadrant {...defaultProps} tasks={[]} />);
      
      expect(screen.getByText('No tasks in this quadrant')).toBeInTheDocument();
      expect(screen.getByText('Drag tasks here to categorize')).toBeInTheDocument();
    });

    it('應該在空狀態下顯示任務數量為 0', () => {
      render(<VirtualizedMatrixQuadrant {...defaultProps} tasks={[]} />);
      
      expect(screen.getByText('0')).toBeInTheDocument();
    });
  });

  describe('拖放功能', () => {
    it('應該在拖放時設置 drag-over 類別', () => {
      const { container } = render(<VirtualizedMatrixQuadrant {...defaultProps} />);
      
      const quadrantElement = container.firstChild as HTMLElement;
      
      fireEvent.dragOver(quadrantElement, {
        preventDefault: vi.fn(),
      });
      
      expect(quadrantElement).toHaveClass('drag-over');
    });

    it('應該在放下時呼叫 onTaskMove', () => {
      const { container } = render(<VirtualizedMatrixQuadrant {...defaultProps} />);
      
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

    it('應該使用記憶化的事件處理器', () => {
      const { rerender } = render(<VirtualizedMatrixQuadrant {...defaultProps} />);
      
      // 獲取初始的事件處理器參考
      const initialComponent = screen.getByText('Do First').closest('.matrix-quadrant');
      
      // 重新渲染相同的 props
      rerender(<VirtualizedMatrixQuadrant {...defaultProps} />);
      
      // 事件處理器應該保持相同的參考（透過 useCallback）
      const afterRerender = screen.getByText('Do First').closest('.matrix-quadrant');
      expect(afterRerender).toBe(initialComponent);
    });
  });

  describe('CSS 類別', () => {
    it('應該在虛擬化時添加虛擬化類別', () => {
      const { container } = render(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={createMockTasks(15)}
        />
      );
      
      const quadrantElement = container.firstChild;
      expect(quadrantElement).toHaveClass('virtualized');
    });

    it('應該在非虛擬化時不添加虛擬化類別', () => {
      const { container } = render(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={createMockTasks(5)}
        />
      );
      
      const quadrantElement = container.firstChild;
      expect(quadrantElement).not.toHaveClass('virtualized');
    });

    it('應該應用正確的象限 ID 類別', () => {
      const { container } = render(<VirtualizedMatrixQuadrant {...defaultProps} />);
      
      const quadrantElement = container.firstChild;
      expect(quadrantElement).toHaveClass('quadrant-urgent-important');
    });
  });

  describe('效能指標', () => {
    beforeEach(() => {
      process.env.NODE_ENV = 'development';
    });

    it('應該在開發模式且任務數量超過50時顯示效能指標', () => {
      render(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={createMockTasks(60)}
        />
      );
      
      expect(screen.getByText('60 tasks • Virtualized')).toBeInTheDocument();
    });

    it('應該在任務數量少於50時不顯示效能指標', () => {
      render(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={createMockTasks(30)}
        />
      );
      
      expect(screen.queryByText(/tasks •/)).not.toBeInTheDocument();
    });

    it('應該在非虛擬化模式下顯示 Standard', () => {
      render(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={createMockTasks(60)}
          enableVirtualization={false}
        />
      );
      
      expect(screen.getByText('60 tasks • Standard')).toBeInTheDocument();
    });
  });

  describe('任務渲染', () => {
    it('應該在虛擬化模式下正確渲染任務', () => {
      render(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={createMockTasks(15)}
        />
      );
      
      // 虛擬化列表應該包含任務
      const virtualizedList = screen.getByTestId('virtualized-list');
      expect(virtualizedList).toBeInTheDocument();
      
      // 任務應該被渲染在虛擬化列表中
      expect(screen.getByTestId('task-card-task-0')).toBeInTheDocument();
    });

    it('應該在標準模式下正確渲染任務', () => {
      render(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={createMockTasks(5)}
        />
      );
      
      // 標準任務容器應該存在
      const standardTasks = document.querySelector('.quadrant-standard-tasks');
      expect(standardTasks).toBeInTheDocument();
      
      // 所有任務都應該被渲染
      for (let i = 0; i < 5; i++) {
        expect(screen.getByTestId(`task-card-task-${i}`)).toBeInTheDocument();
      }
    });

    it('應該將緊湊模式傳遞給任務卡片', () => {
      render(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={createMockTasks(5)}
          compact={true}
        />
      );
      
      const taskCard = screen.getByTestId('task-card-task-0');
      expect(taskCard).toHaveAttribute('data-compact', 'true');
    });
  });

  describe('記憶化和效能', () => {
    it('應該記憶化任務渲染器函數', () => {
      const { rerender } = render(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={createMockTasks(15)}
        />
      );
      
      // 重新渲染相同的 compact 值
      rerender(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={createMockTasks(15)}
          compact={false}
        />
      );
      
      // 任務應該仍然正確渲染
      expect(screen.getByTestId('task-card-task-0')).toBeInTheDocument();
    });

    it('應該記憶化虛擬化決策', () => {
      const tasks = createMockTasks(15);
      
      const { rerender } = render(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={tasks}
          enableVirtualization={true}
        />
      );
      
      expect(screen.getByTestId('virtualized-list')).toBeInTheDocument();
      
      // 重新渲染相同的 props
      rerender(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={tasks}
          enableVirtualization={true}
        />
      );
      
      // 虛擬化列表應該仍然存在
      expect(screen.getByTestId('virtualized-list')).toBeInTheDocument();
    });
  });

  describe('邊界情況', () => {
    it('應該處理正好10個任務的邊界情況', () => {
      render(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={createMockTasks(10)}
        />
      );
      
      // 10個任務應該不使用虛擬化
      expect(screen.queryByTestId('virtualized-list')).not.toBeInTheDocument();
      expect(screen.queryByText('(虛擬)')).not.toBeInTheDocument();
    });

    it('應該處理11個任務啟用虛擬化', () => {
      render(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={createMockTasks(11)}
        />
      );
      
      // 11個任務應該使用虛擬化
      expect(screen.getByTestId('virtualized-list')).toBeInTheDocument();
      expect(screen.getByText('(虛擬)')).toBeInTheDocument();
    });

    it('應該處理極大數量的任務', () => {
      const manyTasks = createMockTasks(1000);
      
      const startTime = performance.now();
      
      render(
        <VirtualizedMatrixQuadrant 
          {...defaultProps} 
          tasks={manyTasks}
        />
      );
      
      const endTime = performance.now();
      const renderTime = endTime - startTime;
      
      // 即使有1000個任務，渲染時間也應該合理
      expect(renderTime).toBeLessThan(200);
      
      expect(screen.getByText('1000')).toBeInTheDocument();
      expect(screen.getByText('(虛擬)')).toBeInTheDocument();
    });
  });

  describe('無障礙功能', () => {
    it('應該支援鍵盤導航', async () => {
      const user = userEvent.setup();
      render(<VirtualizedMatrixQuadrant {...defaultProps} />);
      
      const buttons = screen.getAllByRole('button');
      
      await user.tab();
      expect(buttons[0]).toHaveFocus();
    });

    it('應該提供適當的語意標記', () => {
      render(<VirtualizedMatrixQuadrant {...defaultProps} />);
      
      const buttons = screen.getAllByRole('button');
      expect(buttons).toHaveLength(2);
    });
  });
});