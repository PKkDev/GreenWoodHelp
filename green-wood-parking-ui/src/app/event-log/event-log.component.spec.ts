import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { EventLogComponent } from './event-log.component';

describe('EventLogComponent', () => {
  let component: EventLogComponent;
  let fixture: ComponentFixture<EventLogComponent>;
  let mockDialogRef: MatDialogRef<EventLogComponent>;

  function createComponent(events: Map<Date, string>): void {
    TestBed.configureTestingModule({
      imports: [EventLogComponent],
      providers: [
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { events } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(EventLogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(() => {
    mockDialogRef = {
      close: vi.fn()
    } as unknown as MatDialogRef<EventLogComponent>;
  });

  it('should create', () => {
    createComponent(new Map());
    expect(component).toBeTruthy();
  });

  it('should expose the events passed in via dialog data', () => {
    const events = new Map<Date, string>([[new Date('2026-08-12T10:00:00'), 'Подключено']]);
    createComponent(events);

    expect(component.events()).toBe(events);
  });

  it('should show the empty state when there are no events', () => {
    createComponent(new Map());

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Событий пока нет');
  });

  it('should render an item for every event passed via dialog data', () => {
    const events = new Map<Date, string>([
      [new Date('2026-08-12T10:00:00'), 'Подключено'],
      [new Date('2026-08-12T10:05:00'), 'Место занято']
    ]);
    createComponent(events);

    const items = (fixture.nativeElement as HTMLElement).querySelectorAll('.event-item');
    expect(items.length).toBe(2);
  });

  it('should render the most recent event first', () => {
    const older = new Date('2026-08-12T09:00:00');
    const newer = new Date('2026-08-12T11:00:00');
    const events = new Map<Date, string>([
      [older, 'Старое событие'],
      [newer, 'Новое событие']
    ]);
    createComponent(events);

    const messages = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('.event-message')
    ).map((el) => el.textContent?.trim());

    expect(messages[0]).toBe('Новое событие');
    expect(messages[1]).toBe('Старое событие');
  });

  it('compareDates should order events with the newest date first', () => {
    createComponent(new Map());

    const older = { key: new Date('2026-01-01'), value: 'a' };
    const newer = { key: new Date('2026-06-01'), value: 'b' };

    expect(component.compareDates(newer, older)).toBeLessThan(0);
    expect(component.compareDates(older, newer)).toBeGreaterThan(0);
    expect(component.compareDates(older, older)).toBe(0);
  });
});
