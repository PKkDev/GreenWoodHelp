import styles from './Spinner.module.css';

interface SpinnerProps {
  diameter?: number;
}

export function Spinner({ diameter = 40 }: SpinnerProps) {
  return (
    <span
      className={styles.spinner}
      style={{ width: diameter, height: diameter }}
      role="status"
      aria-label="Загрузка"
    />
  );
}
