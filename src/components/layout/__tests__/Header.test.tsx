import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Header } from '../Header';
import { useAuthStore } from '@/stores/useAuthStore';
import { useTimerStore } from '@/stores/useTimerStore';
import type { User } from '@/types';

// Mock stores
vi.mock('@/stores/useAuthStore', () => ({
  useAuthStore: vi.fn(),
}));

vi.mock('@/stores/useTimerStore', () => ({
  useTimerStore: vi.fn(),
}));

// Mock VisualTimer component
vi.mock('@/components/features/VisualTimer', () => ({
  VisualTimer: ({ compact }: { compact?: boolean }) => (
    <div data-testid="visual-timer" data-compact={compact}>
      Visual Timer Component
    </div>
  ),
}));

// Mock Button component
vi.mock('@/components/ui/Button', () => ({
  Button: ({ children, onClick, icon, className, variant, size, ...props }: any) => (
    <button
      onClick={onClick}
      className={className}
      data-variant={variant}
      data-size={size}
      {...props}
    >
      {icon}
      {children}
    </button>
  ),
}));

describe('Header', () => {
  const mockUser: User = {
    id: 'user-123',
    email: 'test@example.com',
    username: 'testuser',
    displayName: 'Test User',
    avatar: 'https://example.com/avatar.jpg',
    preferences: {
      theme: 'light',
      timezone: 'Asia/Taipei',
      workingHours: { start: '09:00', end: '17:00' },
      pomodoroSettings: {
        workDuration: 25,
        shortBreak: 5,
        longBreak: 15,
        sessionsUntilLongBreak: 4,
      },
      notificationSettings: {
        desktop: true,
        email: false,
        mobile: true,
        quiet_hours: { enabled: false, start: '22:00', end: '08:00' },
      },
      energyTracking: true,
      gamificationEnabled: true,
      densityLevel: 'normal',
    },
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
  };

  const mockLogout = vi.fn();
  const mockOnMenuClick = vi.fn();

  beforeEach(() => {
    vi.mocked(useAuthStore).mockReturnValue({
      user: mockUser,
      logout: mockLogout,
    });

    vi.mocked(useTimerStore).mockReturnValue({
      isRunning: false,
      timeRemaining: 1500, // 25 minutes
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('基本渲染', () => {
    it('應該正確渲染標題', () => {
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByText('Dashboard')).toBeInTheDocument();
    });

    it('應該顯示歡迎訊息', () => {
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByText('Welcome back, Test User!')).toBeInTheDocument();
    });

    it('應該在沒有 displayName 時顯示 username', () => {
      const userWithoutDisplayName = { ...mockUser, displayName: '' };
      vi.mocked(useAuthStore).mockReturnValue({
        user: userWithoutDisplayName,
        logout: mockLogout,
      });

      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByText('Welcome back, testuser!')).toBeInTheDocument();
    });

    it('應該在沒有用戶時不顯示歡迎訊息', () => {
      vi.mocked(useAuthStore).mockReturnValue({
        user: null,
        logout: mockLogout,
      });

      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.queryByText(/Welcome back/)).not.toBeInTheDocument();
    });
  });

  describe('菜單按鈕', () => {
    it('應該預設顯示菜單按鈕', () => {
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByLabelText('Toggle menu')).toBeInTheDocument();
    });

    it('應該能隱藏菜單按鈕', () => {
      render(
        <Header 
          title="Dashboard" 
          onMenuClick={mockOnMenuClick} 
          showMenuButton={false}
        />
      );
      
      expect(screen.queryByLabelText('Toggle menu')).not.toBeInTheDocument();
    });

    it('應該在點擊菜單按鈕時呼叫 onMenuClick', async () => {
      const user = userEvent.setup();
      
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      await user.click(screen.getByLabelText('Toggle menu'));
      
      expect(mockOnMenuClick).toHaveBeenCalledTimes(1);
    });
  });

  describe('計時器顯示', () => {
    it('應該在計時器運行時顯示精簡版計時器', () => {
      vi.mocked(useTimerStore).mockReturnValue({
        isRunning: true,
        timeRemaining: 1500,
      });

      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByTestId('visual-timer')).toBeInTheDocument();
      expect(screen.getByTestId('visual-timer')).toHaveAttribute('data-compact', 'true');
    });

    it('應該在計時器未運行時不顯示精簡版計時器', () => {
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.queryByTestId('visual-timer')).not.toBeInTheDocument();
    });

    it('應該在行動裝置上顯示時間格式', () => {
      vi.mocked(useTimerStore).mockReturnValue({
        isRunning: true,
        timeRemaining: 1500, // 25:00
      });

      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByText('25:00')).toBeInTheDocument();
    });

    it('應該正確格式化時間顯示', () => {
      vi.mocked(useTimerStore).mockReturnValue({
        isRunning: true,
        timeRemaining: 65, // 1:05
      });

      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByText('01:05')).toBeInTheDocument();
    });
  });

  describe('動作按鈕', () => {
    it('應該顯示搜尋按鈕', () => {
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByLabelText('Search')).toBeInTheDocument();
    });

    it('應該顯示快速新增按鈕', () => {
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByLabelText('Quick add')).toBeInTheDocument();
    });

    it('應該顯示通知按鈕', () => {
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByLabelText('Notifications')).toBeInTheDocument();
    });

    it('應該顯示通知徽章', () => {
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByText('3')).toBeInTheDocument();
    });
  });

  describe('用戶選單', () => {
    it('應該顯示用戶頭像', () => {
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      const avatar = screen.getByAltText('Test User');
      expect(avatar).toBeInTheDocument();
      expect(avatar).toHaveAttribute('src', 'https://example.com/avatar.jpg');
    });

    it('應該在沒有頭像時顯示用戶圖示', () => {
      const userWithoutAvatar = { ...mockUser, avatar: undefined };
      vi.mocked(useAuthStore).mockReturnValue({
        user: userWithoutAvatar,
        logout: mockLogout,
      });

      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      // 檢查是否有 User 圖示（透過 lucide-react）
      expect(screen.queryByAltText('Test User')).not.toBeInTheDocument();
    });

    it('應該顯示用戶名稱', () => {
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      // 檢查下拉選單中的用戶名稱
      expect(screen.getAllByText('Test User')).toHaveLength(1);
    });

    it('應該顯示用戶電子郵件', () => {
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByText('test@example.com')).toBeInTheDocument();
    });

    it('應該顯示個人檔案選項', () => {
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByText('Profile')).toBeInTheDocument();
    });

    it('應該顯示設定選項', () => {
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByText('Settings')).toBeInTheDocument();
    });

    it('應該顯示登出選項', () => {
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByText('Sign out')).toBeInTheDocument();
    });

    it('應該在點擊登出時呼叫 logout 函數', async () => {
      const user = userEvent.setup();
      
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      await user.click(screen.getByText('Sign out'));
      
      expect(mockLogout).toHaveBeenCalledTimes(1);
    });
  });

  describe('響應式設計', () => {
    it('應該在桌面版隱藏菜單按鈕', () => {
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      const menuButton = screen.getByLabelText('Toggle menu');
      expect(menuButton).toHaveClass('lg:hidden');
    });

    it('應該在桌面版顯示搜尋按鈕', () => {
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      const searchButton = screen.getByLabelText('Search');
      expect(searchButton).toHaveClass('hidden', 'md:flex');
    });

    it('應該在行動版隱藏精簡計時器', () => {
      vi.mocked(useTimerStore).mockReturnValue({
        isRunning: true,
        timeRemaining: 1500,
      });

      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      const compactTimer = screen.getByTestId('visual-timer').parentElement;
      expect(compactTimer).toHaveClass('hidden', 'md:block');
    });

    it('應該在行動版顯示時間格式', () => {
      vi.mocked(useTimerStore).mockReturnValue({
        isRunning: true,
        timeRemaining: 1500,
      });

      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      const mobileTimer = screen.getByText('25:00').parentElement;
      expect(mobileTimer).toHaveClass('md:hidden');
    });
  });

  describe('客製化屬性', () => {
    it('應該應用客製化 className', () => {
      const { container } = render(
        <Header 
          title="Dashboard" 
          onMenuClick={mockOnMenuClick} 
          className="custom-header"
        />
      );
      
      expect(container.firstChild).toHaveClass('custom-header');
    });
  });

  describe('無障礙功能', () => {
    it('應該有正確的 aria-label', () => {
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByLabelText('Toggle menu')).toBeInTheDocument();
      expect(screen.getByLabelText('Search')).toBeInTheDocument();
      expect(screen.getByLabelText('Quick add')).toBeInTheDocument();
      expect(screen.getByLabelText('Notifications')).toBeInTheDocument();
    });

    it('應該有正確的圖片 alt 文字', () => {
      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      const avatar = screen.getByAltText('Test User');
      expect(avatar).toBeInTheDocument();
    });
  });

  describe('邊界情況', () => {
    it('應該處理極短的用戶名稱', () => {
      const shortNameUser = { ...mockUser, displayName: 'A', username: 'a' };
      vi.mocked(useAuthStore).mockReturnValue({
        user: shortNameUser,
        logout: mockLogout,
      });

      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByText('Welcome back, A!')).toBeInTheDocument();
    });

    it('應該處理極長的標題', () => {
      const longTitle = 'This is a very long title that might overflow the container';
      
      render(<Header title={longTitle} onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByText(longTitle)).toBeInTheDocument();
    });

    it('應該處理零秒的計時器', () => {
      vi.mocked(useTimerStore).mockReturnValue({
        isRunning: true,
        timeRemaining: 0,
      });

      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByText('00:00')).toBeInTheDocument();
    });

    it('應該處理大於一小時的計時器', () => {
      vi.mocked(useTimerStore).mockReturnValue({
        isRunning: true,
        timeRemaining: 3661, // 61:01
      });

      render(<Header title="Dashboard" onMenuClick={mockOnMenuClick} />);
      
      expect(screen.getByText('61:01')).toBeInTheDocument();
    });
  });
});