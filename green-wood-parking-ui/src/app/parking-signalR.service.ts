import { Injectable, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import * as signalR from '@microsoft/signalr';
import { ParkingSlotDto } from './parking-slot-dto';

@Injectable({
  providedIn: 'root'
})
export class ParkingSignalRService {

  private receivedStatus = signal<any>(null);
  public receivedStatus$ = toObservable(this.receivedStatus);

  private receiveParkingData = signal<ParkingSlotDto | null>(null);
  public receiveParkingData$ = toObservable(this.receiveParkingData);

  public hubConnection!: signalR.HubConnection;

  public startConnection() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7196/parking-hub')
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => {
        console.log('SignalR Connected!');
        this.invokeGetParkingData();
      })
      .catch(err => console.error('SignalR Error: ', err));

    // Слушаем событие от бэкенда
    this.hubConnection.on('ReceiveWorkStatus', (data: any) => {
      this.receivedStatus.set(data);
    });

    this.hubConnection.on('ReceiveParkingData', (data: ParkingSlotDto) => {
      this.receiveParkingData.set(data);
    });
  }

  public invokeGetParkingData() {
    this.hubConnection.invoke('GetParkingData');
  }

}
