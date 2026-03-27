import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';

@Injectable({
  providedIn: 'root'
})
export class ParkingSignalRService {

  // Сигнал со списком мест. Карта будет "слушать" его автоматически.
  public slots = signal<any[]>([]);
  private hubConnection!: signalR.HubConnection;

  constructor() { }

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
    this.hubConnection.on('ReceiveStatus', (data: any) => {
      console.log('ReceiveStatus', data);
      // this.slots.set(data);
    });
  }

  public invokeGetParkingData() {
    this.hubConnection.invoke('GetParkingData');
  }

}
