import { inject, Injectable, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import * as signalR from '@microsoft/signalr';
import { BASE_URL } from './app.config';
import { ParkingSlotDto } from './parking-slot-dto';

@Injectable({
  providedIn: 'root'
})
export class ParkingSignalRService {

  private receivedStatus = signal<string | null>(null);
  public receivedStatus$ = toObservable(this.receivedStatus);

  private receiveParkingData = signal<ParkingSlotDto | null>(null);
  public receiveParkingData$ = toObservable(this.receiveParkingData);

  public hubConnection: signalR.HubConnection;

  private _isConnected = signal<boolean>(false);
  public isConnected = this._isConnected.asReadonly();

  public readonly _baseUrl = inject(BASE_URL);

  constructor() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this._baseUrl}/parking-hub`)
      .withAutomaticReconnect()
      .build();

    this.setupSignalRListeners();
  }

  private setupSignalRListeners() {
    // Обновляем сигнал при смене состояний
    this.hubConnection.onclose(() => this._isConnected.set(false));
    this.hubConnection.onreconnecting(() => this._isConnected.set(false));
    this.hubConnection.onreconnected(() => this._isConnected.set(true));
  }

  public startConnection() {
    this.hubConnection
      .start()
      .then(() => {
        console.log('SignalR Connected!');
        this.receivedStatus.set('Подключено');
        this._isConnected.set(true);
        this.invokeGetParkingData();
      })
      .catch((err: { message: string }) => {
        console.error('SignalR Error: ', err);
        this._isConnected.set(false);
        this.receivedStatus.set(err.message);
      });

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
