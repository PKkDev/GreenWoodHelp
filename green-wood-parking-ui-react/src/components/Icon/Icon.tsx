interface IconProps {
  name: string;
  className?: string;
}

export function Icon({ name, className }: IconProps) {
  return (
    <span className={['material-icons', className].filter(Boolean).join(' ')} aria-hidden="true">
      {name}
    </span>
  );
}
