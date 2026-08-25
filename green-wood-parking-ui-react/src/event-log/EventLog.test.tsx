import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { EventLog } from './EventLog';

describe('EventLog', () => {
  it('shows an empty state when there are no events', () => {
    render(<EventLog events={new Map()} onClose={vi.fn()} />);

    expect(screen.getByText('Событий пока нет')).toBeInTheDocument();
  });

  it('renders events sorted by newest first', () => {
    const older = new Date(2026, 0, 1, 10, 0, 0);
    const newer = new Date(2026, 0, 1, 12, 30, 15);
    const events = new Map([
      [older, 'Первое событие'],
      [newer, 'Второе событие'],
    ]);

    render(<EventLog events={events} onClose={vi.fn()} />);

    const items = screen.getAllByText(/событие/);
    expect(items[0]).toHaveTextContent('Второе событие');
    expect(items[1]).toHaveTextContent('Первое событие');
    expect(screen.getByText('12:30:15')).toBeInTheDocument();
    expect(screen.getByText('10:00:00')).toBeInTheDocument();
  });
});
