import { Modal } from '../components/Modal/Modal';
import styles from './EventLog.module.css';

export interface EventLogProps {
  events: Map<Date, string>;
  onClose: () => void;
}

export function EventLog({ events, onClose }: EventLogProps) {
  const sortedEvents = [...events.entries()].sort(([a], [b]) => b.getTime() - a.getTime());

  return (
    <Modal onClose={onClose}>
      <h2 className={styles.title}>История событий</h2>
      <div className={styles.eventList}>
        {sortedEvents.length === 0 && <p>Событий пока нет</p>}
        {sortedEvents.map(([date, message], index) => (
          <div className={styles.eventItem} key={`${date.getTime().toString()}-${index.toString()}`}>
            <span className={styles.eventTime}>{formatTime(date)}</span>
            <span className={styles.eventMessage}>{message}</span>
          </div>
        ))}
      </div>
    </Modal>
  );
}

function formatTime(date: Date): string {
  const pad = (value: number): string => value.toString().padStart(2, '0');
  return `${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
}
