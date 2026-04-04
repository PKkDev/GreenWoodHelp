import { DatePipe, KeyValue, KeyValuePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-event-log',
  templateUrl: './event-log.component.html',
  styleUrls: ['./event-log.component.scss'],
  imports: [DatePipe, KeyValuePipe, MatDialogModule],
})
export class EventLogComponent implements OnInit {

  private readonly dialogRef = inject(MatDialogRef<EventLogComponent>);
  private readonly data: { events: Map<Date, string> } = inject(MAT_DIALOG_DATA);
  public events = signal<Map<Date, string>>(this.data.events);

  constructor() { }

  ngOnInit() {
  }

  public compareDates = (a: KeyValue<Date, string>, b: KeyValue<Date, string>): number => {
    return b.key.getTime() - a.key.getTime();
  }

}
