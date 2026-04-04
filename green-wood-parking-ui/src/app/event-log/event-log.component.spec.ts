
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { EventLogComponent } from './event-log.component';

describe('EventLogComponent', () => {
  let component: EventLogComponent;
  let fixture: ComponentFixture<EventLogComponent>;

  let mockDialogRef: MatDialogRef<EventLogComponent>;
  const mockData = { events: new Map<Date, string>() };

  beforeEach((() => {
    mockDialogRef = {
      close: vi.fn()
    } as unknown as MatDialogRef<EventLogComponent>;

    TestBed.configureTestingModule({
      imports: [EventLogComponent],
      providers: [
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: mockData },
      ]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(EventLogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
