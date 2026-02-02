import { useEffect, useCallback } from 'react';

/**
 * Hook to handle ESC key press
 * @param onEscape Callback function to execute when ESC is pressed
 * @param enabled Whether the hook is enabled (default: true)
 */
export function useEscapeKey(onEscape: () => void, enabled = true) {
  const handleKeyDown = useCallback(
    (event: KeyboardEvent) => {
      if (event.key === 'Escape' && enabled) {
        onEscape();
      }
    },
    [onEscape, enabled]
  );

  useEffect(() => {
    if (enabled) {
      document.addEventListener('keydown', handleKeyDown);
      return () => document.removeEventListener('keydown', handleKeyDown);
    }
  }, [handleKeyDown, enabled]);
}

/**
 * Hook to handle multiple keyboard shortcuts
 * @param shortcuts Map of key combinations to callback functions
 * @param enabled Whether the hook is enabled (default: true)
 */
export function useKeyboardShortcuts(
  shortcuts: Record<string, () => void>,
  enabled = true
) {
  const handleKeyDown = useCallback(
    (event: KeyboardEvent) => {
      if (!enabled) return;

      const key = event.key.toLowerCase();
      const ctrl = event.ctrlKey;
      const shift = event.shiftKey;
      const alt = event.altKey;

      // Build shortcut string (e.g., "ctrl+s", "shift+n")
      let shortcut = '';
      if (ctrl) shortcut += 'ctrl+';
      if (shift) shortcut += 'shift+';
      if (alt) shortcut += 'alt+';
      shortcut += key;

      if (shortcuts[shortcut]) {
        event.preventDefault();
        shortcuts[shortcut]();
      }
    },
    [shortcuts, enabled]
  );

  useEffect(() => {
    if (enabled) {
      document.addEventListener('keydown', handleKeyDown);
      return () => document.removeEventListener('keydown', handleKeyDown);
    }
  }, [handleKeyDown, enabled]);
}
