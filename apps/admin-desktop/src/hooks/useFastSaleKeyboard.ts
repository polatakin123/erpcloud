import { useEffect } from 'react';

interface KeyboardHandlers {
  onBarcodeFocus?: () => void;
  onSearchFocus?: () => void;
  onCustomerPicker?: () => void;
  onDiscountFocus?: () => void;
  onPaymentFocus?: () => void;
  onCustomerPickerForCredit?: () => void;
  onCompleteSale?: () => void;
  onToggleRecent?: () => void;
  onCancel?: () => void;
  onNextLine?: () => void;
  onPrevLine?: () => void;
  onDeleteLine?: () => void;
  onIncreaseQty?: () => void;
  onDecreaseQty?: () => void;
}

export function useFastSaleKeyboard(
  handlers: KeyboardHandlers,
  enabled: boolean = true
) {
  useEffect(() => {
    if (!enabled) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      // Ignore if typing in input/textarea
      const target = e.target as HTMLElement;
      const isInput = target.tagName === 'INPUT' || target.tagName === 'TEXTAREA';

      // F Keys - work even in inputs
      if (e.key === 'F1') {
        e.preventDefault();
        handlers.onBarcodeFocus?.();
        return;
      }
      if (e.key === 'F2') {
        e.preventDefault();
        handlers.onSearchFocus?.();
        return;
      }
      if (e.key === 'F3') {
        e.preventDefault();
        handlers.onCustomerPicker?.();
        return;
      }
      if (e.key === 'F4') {
        e.preventDefault();
        handlers.onDiscountFocus?.();
        return;
      }
      if (e.key === 'F6') {
        e.preventDefault();
        handlers.onPaymentFocus?.();
        return;
      }
      if (e.key === 'F7') {
        e.preventDefault();
        handlers.onCustomerPickerForCredit?.();
        return;
      }
      if (e.key === 'F9') {
        e.preventDefault();
        handlers.onCompleteSale?.();
        return;
      }
      if (e.key === 'F10') {
        e.preventDefault();
        handlers.onToggleRecent?.();
        return;
      }

      // ESC - works always
      if (e.key === 'Escape') {
        e.preventDefault();
        handlers.onCancel?.();
        return;
      }

      // Navigation keys - only when NOT in input
      if (!isInput) {
        if (e.key === 'ArrowDown') {
          e.preventDefault();
          handlers.onNextLine?.();
        }
        if (e.key === 'ArrowUp') {
          e.preventDefault();
          handlers.onPrevLine?.();
        }
        if (e.key === 'Delete') {
          e.preventDefault();
          handlers.onDeleteLine?.();
        }
        if (e.key === '+' || e.key === '=') {
          e.preventDefault();
          handlers.onIncreaseQty?.();
        }
        if (e.key === '-' || e.key === '_') {
          e.preventDefault();
          handlers.onDecreaseQty?.();
        }
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [handlers, enabled]);
}
