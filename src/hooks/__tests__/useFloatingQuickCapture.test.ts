import { describe, it, expect, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useFloatingQuickCapture } from '../useFloatingQuickCapture';

describe('useFloatingQuickCapture', () => {
  describe('初始狀態', () => {
    it('應該有正確的初始狀態', () => {
      const { result } = renderHook(() => useFloatingQuickCapture());
      
      expect(result.current.isVisible).toBe(false);
      expect(result.current.isMinimized).toBe(true);
    });
  });

  describe('show 功能', () => {
    it('應該能顯示並展開浮動元件', () => {
      const { result } = renderHook(() => useFloatingQuickCapture());
      
      act(() => {
        result.current.show();
      });
      
      expect(result.current.isVisible).toBe(true);
      expect(result.current.isMinimized).toBe(false);
    });
  });

  describe('hide 功能', () => {
    it('應該能隱藏浮動元件', () => {
      const { result } = renderHook(() => useFloatingQuickCapture());
      
      // 先顯示
      act(() => {
        result.current.show();
      });
      
      expect(result.current.isVisible).toBe(true);
      
      // 然後隱藏
      act(() => {
        result.current.hide();
      });
      
      expect(result.current.isVisible).toBe(false);
    });

    it('應該在隱藏時保持 isMinimized 狀態不變', () => {
      const { result } = renderHook(() => useFloatingQuickCapture());
      
      // 先顯示並展開
      act(() => {
        result.current.show();
      });
      
      const minimizedBeforeHide = result.current.isMinimized;
      
      // 隱藏
      act(() => {
        result.current.hide();
      });
      
      expect(result.current.isMinimized).toBe(minimizedBeforeHide);
    });
  });

  describe('toggle 功能', () => {
    it('應該在未顯示時顯示並展開', () => {
      const { result } = renderHook(() => useFloatingQuickCapture());
      
      expect(result.current.isVisible).toBe(false);
      
      act(() => {
        result.current.toggle();
      });
      
      expect(result.current.isVisible).toBe(true);
      expect(result.current.isMinimized).toBe(false);
    });

    it('應該在已顯示時切換最小化狀態', () => {
      const { result } = renderHook(() => useFloatingQuickCapture());
      
      // 先顯示
      act(() => {
        result.current.show();
      });
      
      expect(result.current.isVisible).toBe(true);
      expect(result.current.isMinimized).toBe(false);
      
      // 切換到最小化
      act(() => {
        result.current.toggle();
      });
      
      expect(result.current.isVisible).toBe(true);
      expect(result.current.isMinimized).toBe(true);
      
      // 再次切換回展開
      act(() => {
        result.current.toggle();
      });
      
      expect(result.current.isVisible).toBe(true);
      expect(result.current.isMinimized).toBe(false);
    });
  });

  describe('minimize 功能', () => {
    it('應該設置 isMinimized 為 true', () => {
      const { result } = renderHook(() => useFloatingQuickCapture());
      
      // 先顯示並展開
      act(() => {
        result.current.show();
      });
      
      expect(result.current.isMinimized).toBe(false);
      
      // 最小化
      act(() => {
        result.current.minimize();
      });
      
      expect(result.current.isMinimized).toBe(true);
    });

    it('應該在未顯示時也能設置最小化狀態', () => {
      const { result } = renderHook(() => useFloatingQuickCapture());
      
      expect(result.current.isVisible).toBe(false);
      
      act(() => {
        result.current.minimize();
      });
      
      expect(result.current.isMinimized).toBe(true);
      expect(result.current.isVisible).toBe(false);
    });

    it('應該在已最小化時保持最小化狀態', () => {
      const { result } = renderHook(() => useFloatingQuickCapture());
      
      // 初始狀態已經是最小化
      expect(result.current.isMinimized).toBe(true);
      
      act(() => {
        result.current.minimize();
      });
      
      expect(result.current.isMinimized).toBe(true);
    });
  });

  describe('狀態組合測試', () => {
    it('應該正確處理完整的顯示流程', () => {
      const { result } = renderHook(() => useFloatingQuickCapture());
      
      // 初始狀態：隱藏且最小化
      expect(result.current.isVisible).toBe(false);
      expect(result.current.isMinimized).toBe(true);
      
      // 顯示：可見且展開
      act(() => {
        result.current.show();
      });
      
      expect(result.current.isVisible).toBe(true);
      expect(result.current.isMinimized).toBe(false);
      
      // 最小化：可見但最小化
      act(() => {
        result.current.minimize();
      });
      
      expect(result.current.isVisible).toBe(true);
      expect(result.current.isMinimized).toBe(true);
      
      // 隱藏：不可見且保持最小化
      act(() => {
        result.current.hide();
      });
      
      expect(result.current.isVisible).toBe(false);
      expect(result.current.isMinimized).toBe(true);
    });

    it('應該正確處理 toggle 的邊界情況', () => {
      const { result } = renderHook(() => useFloatingQuickCapture());
      
      // 從隱藏狀態開始 toggle
      act(() => {
        result.current.toggle();
      });
      
      expect(result.current.isVisible).toBe(true);
      expect(result.current.isMinimized).toBe(false);
      
      // 手動最小化
      act(() => {
        result.current.minimize();
      });
      
      // 再次 toggle 應該展開
      act(() => {
        result.current.toggle();
      });
      
      expect(result.current.isVisible).toBe(true);
      expect(result.current.isMinimized).toBe(false);
    });
  });

  describe('函數穩定性', () => {
    it('應該返回有效的函數', () => {
      const { result } = renderHook(() => useFloatingQuickCapture());
      
      // 檢查所有返回的函數都是有效的
      expect(typeof result.current.show).toBe('function');
      expect(typeof result.current.hide).toBe('function');
      expect(typeof result.current.toggle).toBe('function');
      expect(typeof result.current.minimize).toBe('function');
    });
  });

  describe('多次呼叫測試', () => {
    it('應該正確處理連續呼叫 show', () => {
      const { result } = renderHook(() => useFloatingQuickCapture());
      
      // 連續呼叫 show
      act(() => {
        result.current.show();
        result.current.show();
        result.current.show();
      });
      
      expect(result.current.isVisible).toBe(true);
      expect(result.current.isMinimized).toBe(false);
    });

    it('應該正確處理連續呼叫 hide', () => {
      const { result } = renderHook(() => useFloatingQuickCapture());
      
      // 先顯示
      act(() => {
        result.current.show();
      });
      
      // 連續呼叫 hide
      act(() => {
        result.current.hide();
        result.current.hide();
        result.current.hide();
      });
      
      expect(result.current.isVisible).toBe(false);
    });

    it('應該正確處理連續呼叫 toggle', () => {
      const { result } = renderHook(() => useFloatingQuickCapture());
      
      // 連續 toggle 應該在顯示/隱藏和最小化/展開間切換
      act(() => {
        result.current.toggle(); // 顯示並展開
      });
      
      expect(result.current.isVisible).toBe(true);
      expect(result.current.isMinimized).toBe(false);
      
      act(() => {
        result.current.toggle(); // 最小化
      });
      
      expect(result.current.isVisible).toBe(true);
      expect(result.current.isMinimized).toBe(true);
      
      act(() => {
        result.current.toggle(); // 展開
      });
      
      expect(result.current.isVisible).toBe(true);
      expect(result.current.isMinimized).toBe(false);
    });
  });

  describe('邊界條件測試', () => {
    it('應該在快速連續操作時保持狀態一致性', () => {
      const { result } = renderHook(() => useFloatingQuickCapture());
      
      // 快速連續執行多種操作
      act(() => {
        result.current.show();
        result.current.minimize();
        result.current.toggle();
        result.current.hide();
        result.current.show();
      });
      
      // 最終狀態應該是顯示且展開
      expect(result.current.isVisible).toBe(true);
      expect(result.current.isMinimized).toBe(false);
    });
  });
});