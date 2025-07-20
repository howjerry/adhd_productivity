import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QuickCapture } from '../QuickCapture';
import { useTaskStore } from '@/stores/useTaskStore';
import { Priority, EnergyLevel } from '@/types';

// Mock the task store
vi.mock('@/stores/useTaskStore', () => ({
  useTaskStore: vi.fn(),
}));

// Mock UI components
vi.mock('@/components/ui/Button', () => ({
  Button: ({ children, onClick, disabled, loading, type, ...props }: any) => (
    <button
      onClick={onClick}
      disabled={disabled || loading}
      type={type}
      data-loading={loading}
      {...props}
    >
      {children}
    </button>
  ),
}));

describe('QuickCapture', () => {
  const mockCreateTask = vi.fn();
  const mockOnCapture = vi.fn();
  const mockOnToggleMinimized = vi.fn();

  beforeEach(() => {
    vi.mocked(useTaskStore).mockReturnValue({
      createTask: mockCreateTask,
    });

    mockCreateTask.mockResolvedValue({
      id: 'task-123',
      title: 'Test task',
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('基本渲染', () => {
    it('應該正確渲染元件', () => {
      render(<QuickCapture />);
      
      expect(screen.getByPlaceholderText(/What's on your mind/)).toBeInTheDocument();
      expect(screen.getByText('Capture')).toBeInTheDocument();
    });

    it('應該渲染優先級選擇器', () => {
      render(<QuickCapture />);
      
      expect(screen.getByTitle('High Priority (Ctrl+1)')).toBeInTheDocument();
      expect(screen.getByTitle('Medium Priority (Ctrl+2)')).toBeInTheDocument();
      expect(screen.getByTitle('Low Priority (Ctrl+3)')).toBeInTheDocument();
    });

    it('應該渲染能量級別選擇器', () => {
      render(<QuickCapture />);
      
      Object.values(EnergyLevel).forEach(level => {
        expect(screen.getByTitle(`${level} energy required`)).toBeInTheDocument();
      });
    });

    it('應該渲染語音輸入按鈕', () => {
      render(<QuickCapture />);
      
      expect(screen.getByTitle('Voice input')).toBeInTheDocument();
    });
  });

  describe('浮動模式', () => {
    it('應該在最小化時顯示簡化介面', () => {
      render(
        <QuickCapture 
          floating 
          minimized 
          onToggleMinimized={mockOnToggleMinimized}
        />
      );
      
      expect(screen.getByRole('button', { name: 'Open quick capture' })).toBeInTheDocument();
      expect(screen.queryByPlaceholderText(/What's on your mind/)).not.toBeInTheDocument();
    });

    it('應該在點擊最小化按鈕時呼叫 onToggleMinimized', async () => {
      const user = userEvent.setup();
      
      render(
        <QuickCapture 
          floating 
          minimized 
          onToggleMinimized={mockOnToggleMinimized}
        />
      );
      
      await user.click(screen.getByRole('button', { name: 'Open quick capture' }));
      
      expect(mockOnToggleMinimized).toHaveBeenCalledTimes(1);
    });

    it('應該在浮動模式下顯示最小化按鈕', () => {
      render(
        <QuickCapture 
          floating 
          onToggleMinimized={mockOnToggleMinimized}
        />
      );
      
      expect(screen.getByLabelText('Minimize')).toBeInTheDocument();
    });
  });

  describe('文字輸入功能', () => {
    it('應該更新輸入值', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.type(textarea, 'Test task content');
      
      expect(textarea).toHaveValue('Test task content');
    });

    it('應該顯示字數統計', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.type(textarea, 'Test content');
      
      expect(screen.getByText('12/300')).toBeInTheDocument();
    });

    it('應該在字數過多時顯示警告樣式', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      const longText = 'a'.repeat(250);
      await user.type(textarea, longText);
      
      const counter = screen.getByText('250/300');
      expect(counter).toHaveClass('counter-warning');
    });

    it('應該在達到字數限制時顯示限制樣式', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      const longText = 'a'.repeat(350);
      await user.type(textarea, longText);
      
      const counter = screen.getByText('350/300');
      expect(counter).toHaveClass('counter-limit');
    });
  });

  describe('優先級選擇', () => {
    it('應該預設選擇中等優先級', () => {
      render(<QuickCapture />);
      
      const mediumButton = screen.getByTitle('Medium Priority (Ctrl+2)');
      expect(mediumButton).toHaveClass('selected');
    });

    it('應該能選擇不同優先級', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const highButton = screen.getByTitle('High Priority (Ctrl+1)');
      await user.click(highButton);
      
      expect(highButton).toHaveClass('selected');
    });

    it('應該支援鍵盤快速鍵設定優先級', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.click(textarea);
      await user.keyboard('{Control>}1{/Control}');
      
      const highButton = screen.getByTitle('High Priority (Ctrl+1)');
      expect(highButton).toHaveClass('selected');
    });
  });

  describe('能量級別選擇', () => {
    it('應該能選擇能量級別', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const highEnergyButton = screen.getByTitle(`${EnergyLevel.HIGH} energy required`);
      await user.click(highEnergyButton);
      
      expect(highEnergyButton).toHaveClass('selected');
    });

    it('應該能取消選擇能量級別', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const highEnergyButton = screen.getByTitle(`${EnergyLevel.HIGH} energy required`);
      
      // 先選擇
      await user.click(highEnergyButton);
      expect(highEnergyButton).toHaveClass('selected');
      
      // 再點擊取消選擇
      await user.click(highEnergyButton);
      expect(highEnergyButton).not.toHaveClass('selected');
    });
  });

  describe('標籤功能', () => {
    it('應該在獲得焦點時顯示標籤建議', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.click(textarea);
      
      expect(screen.getByText('work')).toBeInTheDocument();
      expect(screen.getByText('personal')).toBeInTheDocument();
      expect(screen.getByText('urgent')).toBeInTheDocument();
    });

    it('應該能選擇和取消選擇標籤', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.click(textarea);
      
      const workTag = screen.getByText('work');
      
      // 選擇標籤
      await user.click(workTag);
      expect(workTag).toHaveClass('tag-selected');
      
      // 取消選擇標籤
      await user.click(workTag);
      expect(workTag).not.toHaveClass('tag-selected');
    });
  });

  describe('智能建議功能', () => {
    it('應該檢測電子郵件相關內容並提供建議', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.type(textarea, 'Send email to client');
      
      expect(screen.getByText('Add "email" tag')).toBeInTheDocument();
    });

    it('應該檢測會議相關內容並提供時間區塊建議', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.type(textarea, 'Schedule meeting with team');
      
      expect(screen.getByText('Schedule time block')).toBeInTheDocument();
    });

    it('應該檢測緊急內容並提供高優先級建議', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.type(textarea, 'Urgent task needs to be done asap');
      
      expect(screen.getByText('Set high priority')).toBeInTheDocument();
    });

    it('應該能點擊智能建議執行動作', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.type(textarea, 'Urgent task');
      
      const prioritySuggestion = screen.getByText('Set high priority');
      await user.click(prioritySuggestion);
      
      const highButton = screen.getByTitle('High Priority (Ctrl+1)');
      expect(highButton).toHaveClass('selected');
    });
  });

  describe('語音錄音功能', () => {
    it('應該能切換錄音狀態', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const voiceButton = screen.getByTitle('Voice input');
      await user.click(voiceButton);
      
      expect(screen.getByText('Recording...')).toBeInTheDocument();
      
      await user.click(voiceButton);
      expect(screen.queryByText('Recording...')).not.toBeInTheDocument();
    });
  });

  describe('表單提交功能', () => {
    it('應該成功提交任務', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture onCapture={mockOnCapture} />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.type(textarea, 'Test task');
      
      const submitButton = screen.getByText('Capture');
      await user.click(submitButton);
      
      await waitFor(() => {
        expect(mockCreateTask).toHaveBeenCalledWith({
          title: 'Test task',
          priority: Priority.MEDIUM,
          energyLevel: undefined,
          tags: [],
        });
      });
      
      expect(mockOnCapture).toHaveBeenCalledWith('Test task');
    });

    it('應該在提交後清除表單', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.type(textarea, 'Test task');
      
      // 選擇標籤和設定
      await user.click(textarea); // 獲得焦點以顯示標籤
      await user.click(screen.getByText('work'));
      await user.click(screen.getByTitle('High Priority (Ctrl+1)'));
      await user.click(screen.getByTitle(`${EnergyLevel.HIGH} energy required`));
      
      const submitButton = screen.getByText('Capture');
      await user.click(submitButton);
      
      await waitFor(() => {
        expect(textarea).toHaveValue('');
      });
      
      // 檢查設定是否重置
      expect(screen.getByTitle('Medium Priority (Ctrl+2)')).toHaveClass('selected');
      expect(screen.getByText('work')).not.toHaveClass('tag-selected');
      expect(screen.getByTitle(`${EnergyLevel.HIGH} energy required`)).not.toHaveClass('selected');
    });

    it('應該在空內容時禁用提交按鈕', () => {
      render(<QuickCapture />);
      
      const submitButton = screen.getByText('Capture');
      expect(submitButton).toBeDisabled();
    });

    it('應該在處理中時顯示載入狀態', async () => {
      const user = userEvent.setup();
      
      // 模擬較慢的 API 呼叫
      mockCreateTask.mockImplementation(() => new Promise(resolve => setTimeout(resolve, 100)));
      
      render(<QuickCapture />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.type(textarea, 'Test task');
      
      const submitButton = screen.getByText('Capture');
      await user.click(submitButton);
      
      expect(submitButton).toHaveAttribute('data-loading', 'true');
      expect(submitButton).toBeDisabled();
    });

    it('應該支援 Ctrl+Enter 快速提交', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.type(textarea, 'Test task');
      await user.keyboard('{Control>}{Enter}{/Control}');
      
      await waitFor(() => {
        expect(mockCreateTask).toHaveBeenCalled();
      });
    });
  });

  describe('鍵盤快速鍵', () => {
    it('應該支援 Escape 清除內容', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.type(textarea, 'Test content');
      expect(textarea).toHaveValue('Test content');
      
      await user.keyboard('{Escape}');
      expect(textarea).toHaveValue('');
    });

    it('應該在浮動模式下支援 Escape 最小化', async () => {
      const user = userEvent.setup();
      
      render(
        <QuickCapture 
          floating 
          onToggleMinimized={mockOnToggleMinimized}
        />
      );
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.click(textarea);
      await user.keyboard('{Escape}');
      
      expect(mockOnToggleMinimized).toHaveBeenCalled();
    });

    it('應該在獲得焦點時顯示鍵盤快速鍵提示', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.click(textarea);
      
      expect(screen.getByText('Keyboard shortcuts:')).toBeInTheDocument();
      expect(screen.getByText('Save')).toBeInTheDocument();
      expect(screen.getByText('Priority')).toBeInTheDocument();
      expect(screen.getByText('Clear/Close')).toBeInTheDocument();
    });
  });

  describe('錯誤處理', () => {
    it('應該處理任務創建失敗', async () => {
      const user = userEvent.setup();
      const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      
      mockCreateTask.mockRejectedValue(new Error('API Error'));
      
      render(<QuickCapture />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.type(textarea, 'Test task');
      
      const submitButton = screen.getByText('Capture');
      await user.click(submitButton);
      
      await waitFor(() => {
        expect(consoleErrorSpy).toHaveBeenCalledWith('Failed to create task:', expect.any(Error));
      });
      
      consoleErrorSpy.mockRestore();
    });
  });

  describe('自動調整文字區域高度', () => {
    it('應該根據內容調整 textarea 高度', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/) as HTMLTextAreaElement;
      
      // 模擬多行內容
      const multilineContent = 'Line 1\nLine 2\nLine 3\nLine 4';
      await user.type(textarea, multilineContent);
      
      // 驗證 scrollHeight 被設置為 height
      expect(textarea.style.height).toBe(`${textarea.scrollHeight}px`);
    });
  });

  describe('浮動模式特殊行為', () => {
    it('應該在浮動模式下提交後自動最小化', async () => {
      const user = userEvent.setup();
      
      render(
        <QuickCapture 
          floating 
          onToggleMinimized={mockOnToggleMinimized}
        />
      );
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.type(textarea, 'Test task');
      
      const submitButton = screen.getByText('Capture');
      await user.click(submitButton);
      
      await waitFor(() => {
        expect(mockOnToggleMinimized).toHaveBeenCalled();
      });
    });
  });

  describe('客製化屬性', () => {
    it('應該應用客製化 className', () => {
      const { container } = render(<QuickCapture className="custom-class" />);
      
      expect(container.firstChild).toHaveClass('custom-class');
    });

    it('應該支援客製化 onCapture 回調', async () => {
      const user = userEvent.setup();
      
      render(<QuickCapture onCapture={mockOnCapture} />);
      
      const textarea = screen.getByPlaceholderText(/What's on your mind/);
      await user.type(textarea, 'Test task');
      
      const submitButton = screen.getByText('Capture');
      await user.click(submitButton);
      
      await waitFor(() => {
        expect(mockOnCapture).toHaveBeenCalledWith('Test task');
      });
    });
  });
});