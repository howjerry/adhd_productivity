import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MatrixFilters } from '../MatrixFilters';
import { EnergyLevel } from '@/types';

describe('MatrixFilters', () => {
  const mockOnEnergyFilterChange = vi.fn();
  const mockOnTagFilterChange = vi.fn();
  
  const defaultProps = {
    energyFilter: null,
    tagFilter: null,
    availableTags: ['work', 'personal', 'urgent', 'meeting', 'project'],
    onEnergyFilterChange: mockOnEnergyFilterChange,
    onTagFilterChange: mockOnTagFilterChange,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('基本渲染', () => {
    it('應該正確渲染篩選器標題', () => {
      render(<MatrixFilters {...defaultProps} />);
      
      expect(screen.getByText('Filters:')).toBeInTheDocument();
    });

    it('應該渲染所有能量級別按鈕', () => {
      render(<MatrixFilters {...defaultProps} />);
      
      expect(screen.getByText('All Energy')).toBeInTheDocument();
      
      Object.values(EnergyLevel).forEach(level => {
        expect(screen.getByText(level)).toBeInTheDocument();
      });
    });

    it('應該渲染標籤篩選器按鈕', () => {
      render(<MatrixFilters {...defaultProps} />);
      
      expect(screen.getByText('All Tags')).toBeInTheDocument();
      expect(screen.getByText('#work')).toBeInTheDocument();
      expect(screen.getByText('#personal')).toBeInTheDocument();
      expect(screen.getByText('#urgent')).toBeInTheDocument();
      expect(screen.getByText('#meeting')).toBeInTheDocument();
      expect(screen.getByText('#project')).toBeInTheDocument();
    });

    it('應該在沒有可用標籤時不顯示標籤篩選器', () => {
      render(<MatrixFilters {...defaultProps} availableTags={[]} />);
      
      expect(screen.queryByText('All Tags')).not.toBeInTheDocument();
      expect(screen.queryByText('|')).not.toBeInTheDocument();
    });

    it('應該最多顯示5個標籤', () => {
      const manyTags = ['tag1', 'tag2', 'tag3', 'tag4', 'tag5', 'tag6', 'tag7'];
      
      render(<MatrixFilters {...defaultProps} availableTags={manyTags} />);
      
      expect(screen.getByText('#tag1')).toBeInTheDocument();
      expect(screen.getByText('#tag5')).toBeInTheDocument();
      expect(screen.queryByText('#tag6')).not.toBeInTheDocument();
      expect(screen.queryByText('#tag7')).not.toBeInTheDocument();
    });
  });

  describe('能量級別篩選', () => {
    it('應該預設選中 "All Energy"', () => {
      render(<MatrixFilters {...defaultProps} />);
      
      expect(screen.getByText('All Energy')).toHaveClass('filter-active');
    });

    it('應該正確顯示選中的能量級別', () => {
      render(<MatrixFilters {...defaultProps} energyFilter={EnergyLevel.HIGH} />);
      
      expect(screen.getByText('All Energy')).not.toHaveClass('filter-active');
      expect(screen.getByText(EnergyLevel.HIGH)).toHaveClass('filter-active');
    });

    it('應該在點擊 "All Energy" 時呼叫 onEnergyFilterChange(null)', async () => {
      const user = userEvent.setup();
      
      render(<MatrixFilters {...defaultProps} energyFilter={EnergyLevel.HIGH} />);
      
      await user.click(screen.getByText('All Energy'));
      
      expect(mockOnEnergyFilterChange).toHaveBeenCalledWith(null);
    });

    it('應該在點擊能量級別時呼叫 onEnergyFilterChange', async () => {
      const user = userEvent.setup();
      
      render(<MatrixFilters {...defaultProps} />);
      
      await user.click(screen.getByText(EnergyLevel.HIGH));
      
      expect(mockOnEnergyFilterChange).toHaveBeenCalledWith(EnergyLevel.HIGH);
    });

    it('應該在再次點擊相同能量級別時取消選擇', async () => {
      const user = userEvent.setup();
      
      render(<MatrixFilters {...defaultProps} energyFilter={EnergyLevel.HIGH} />);
      
      await user.click(screen.getByText(EnergyLevel.HIGH));
      
      expect(mockOnEnergyFilterChange).toHaveBeenCalledWith(null);
    });
  });

  describe('標籤篩選', () => {
    it('應該預設選中 "All Tags"', () => {
      render(<MatrixFilters {...defaultProps} />);
      
      expect(screen.getByText('All Tags')).toHaveClass('filter-active');
    });

    it('應該正確顯示選中的標籤', () => {
      render(<MatrixFilters {...defaultProps} tagFilter="work" />);
      
      expect(screen.getByText('All Tags')).not.toHaveClass('filter-active');
      expect(screen.getByText('#work')).toHaveClass('filter-active');
    });

    it('應該在點擊 "All Tags" 時呼叫 onTagFilterChange(null)', async () => {
      const user = userEvent.setup();
      
      render(<MatrixFilters {...defaultProps} tagFilter="work" />);
      
      await user.click(screen.getByText('All Tags'));
      
      expect(mockOnTagFilterChange).toHaveBeenCalledWith(null);
    });

    it('應該在點擊標籤時呼叫 onTagFilterChange', async () => {
      const user = userEvent.setup();
      
      render(<MatrixFilters {...defaultProps} />);
      
      await user.click(screen.getByText('#work'));
      
      expect(mockOnTagFilterChange).toHaveBeenCalledWith('work');
    });

    it('應該在再次點擊相同標籤時取消選擇', async () => {
      const user = userEvent.setup();
      
      render(<MatrixFilters {...defaultProps} tagFilter="work" />);
      
      await user.click(screen.getByText('#work'));
      
      expect(mockOnTagFilterChange).toHaveBeenCalledWith(null);
    });
  });

  describe('分隔線顯示', () => {
    it('應該在有標籤時顯示分隔線', () => {
      render(<MatrixFilters {...defaultProps} />);
      
      expect(screen.getByText('|')).toBeInTheDocument();
    });

    it('應該在沒有標籤時不顯示分隔線', () => {
      render(<MatrixFilters {...defaultProps} availableTags={[]} />);
      
      expect(screen.queryByText('|')).not.toBeInTheDocument();
    });
  });

  describe('同時篩選', () => {
    it('應該支援同時設置能量級別和標籤篩選', () => {
      render(
        <MatrixFilters 
          {...defaultProps} 
          energyFilter={EnergyLevel.HIGH}
          tagFilter="work"
        />
      );
      
      expect(screen.getByText(EnergyLevel.HIGH)).toHaveClass('filter-active');
      expect(screen.getByText('#work')).toHaveClass('filter-active');
      expect(screen.getByText('All Energy')).not.toHaveClass('filter-active');
      expect(screen.getByText('All Tags')).not.toHaveClass('filter-active');
    });
  });

  describe('無障礙功能', () => {
    it('應該支援鍵盤導航', async () => {
      const user = userEvent.setup();
      
      render(<MatrixFilters {...defaultProps} />);
      
      const allEnergyButton = screen.getByText('All Energy');
      
      await user.tab();
      expect(allEnergyButton).toHaveFocus();
      
      await user.keyboard('{Enter}');
      expect(mockOnEnergyFilterChange).toHaveBeenCalledWith(null);
    });

    it('應該有正確的按鈕語意', () => {
      render(<MatrixFilters {...defaultProps} />);
      
      const buttons = screen.getAllByRole('button');
      expect(buttons.length).toBeGreaterThan(0);
      
      buttons.forEach(button => {
        expect(button).toBeInTheDocument();
      });
    });
  });

  describe('邊界情況', () => {
    it('應該處理空的標籤陣列', () => {
      render(<MatrixFilters {...defaultProps} availableTags={[]} />);
      
      expect(screen.getByText('All Energy')).toBeInTheDocument();
      expect(screen.queryByText('All Tags')).not.toBeInTheDocument();
    });

    it('應該處理單一標籤', () => {
      render(<MatrixFilters {...defaultProps} availableTags={['work']} />);
      
      expect(screen.getByText('All Tags')).toBeInTheDocument();
      expect(screen.getByText('#work')).toBeInTheDocument();
    });

    it('應該處理極長的標籤名稱', () => {
      const longTag = 'this-is-a-very-long-tag-name-that-might-overflow';
      
      render(<MatrixFilters {...defaultProps} availableTags={[longTag]} />);
      
      expect(screen.getByText(`#${longTag}`)).toBeInTheDocument();
    });

    it('應該處理包含特殊字符的標籤', () => {
      const specialTags = ['tag@email', 'tag-with-dash', 'tag_with_underscore'];
      
      render(<MatrixFilters {...defaultProps} availableTags={specialTags} />);
      
      specialTags.forEach(tag => {
        expect(screen.getByText(`#${tag}`)).toBeInTheDocument();
      });
    });
  });

  describe('效能測試', () => {
    it('應該高效處理大量標籤', () => {
      const manyTags = Array.from({ length: 100 }, (_, i) => `tag${i}`);
      
      const startTime = performance.now();
      
      render(<MatrixFilters {...defaultProps} availableTags={manyTags} />);
      
      const endTime = performance.now();
      const renderTime = endTime - startTime;
      
      // 渲染時間應該少於100ms (在現代瀏覽器中這是合理的期望)
      expect(renderTime).toBeLessThan(100);
      
      // 只應該顯示前5個標籤
      expect(screen.getByText('#tag0')).toBeInTheDocument();
      expect(screen.getByText('#tag4')).toBeInTheDocument();
      expect(screen.queryByText('#tag5')).not.toBeInTheDocument();
    });
  });
});