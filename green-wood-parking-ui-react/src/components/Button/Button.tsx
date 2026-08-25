import type { ButtonHTMLAttributes, ReactNode } from 'react';
import styles from './Button.module.css';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'extended' | 'icon' | 'text';
  children: ReactNode;
}

export function Button({ variant = 'extended', className, children, ...rest }: ButtonProps) {
  const classes = [styles.button, styles[variant], className].filter(Boolean).join(' ');

  return (
    <button type="button" className={classes} {...rest}>
      {children}
    </button>
  );
}
