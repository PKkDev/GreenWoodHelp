import { useCallback, useEffect, useRef, useState } from 'react';

export interface ToastMessage {
  id: number;
  message: string;
}

const AUTO_DISMISS_MS = 3000;

export function useToastQueue() {
  const [current, setCurrent] = useState<ToastMessage | null>(null);
  const queueRef = useRef<ToastMessage[]>([]);
  const idRef = useRef(0);

  const dismiss = useCallback(() => {
    setCurrent(queueRef.current.shift() ?? null);
  }, []);

  const show = useCallback((message: string) => {
    idRef.current += 1;
    const toast: ToastMessage = { id: idRef.current, message };

    setCurrent((prev) => {
      if (prev) {
        queueRef.current.push(toast);
        return prev;
      }
      return toast;
    });
  }, []);

  useEffect(() => {
    if (!current) {
      return;
    }

    const timer = setTimeout(dismiss, AUTO_DISMISS_MS);
    return () => { clearTimeout(timer); };
  }, [current, dismiss]);

  return { current, show, dismiss };
}
