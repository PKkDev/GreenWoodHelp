import { Button } from '../Button/Button';
import type { ToastMessage } from './useToastQueue';
import styles from './Toast.module.css';

interface ToastProps {
  toast: ToastMessage;
  onClose: () => void;
}

export function Toast({ toast, onClose }: ToastProps) {
  return (
    <div className={styles.toast} role="status">
      <span className={styles.message}>{toast.message}</span>
      <Button variant="text" className={styles.close} onClick={onClose}>
        Закрыть
      </Button>
    </div>
  );
}
