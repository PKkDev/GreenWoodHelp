import * as signalR from '@microsoft/signalr';
import { BASE_URL } from '../appConfig';
import type { ParkingSlotDto } from '../models/parkingSlotDto';

type Listener<T> = (value: T) => void;

class ParkingSignalRService {
  public hubConnection: signalR.HubConnection;

  private statusListeners = new Set<Listener<string | null>>();
  private parkingDataListeners = new Set<Listener<ParkingSlotDto | null>>();
  private connectedListeners = new Set<Listener<boolean>>();
  private connected = false;

  constructor() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${BASE_URL}/parking-hub`)
      .withAutomaticReconnect()
      .build();

    this.setupSignalRListeners();
  }

  public get isConnected(): boolean {
    return this.connected;
  }

  public onStatus(listener: Listener<string | null>): () => void {
    this.statusListeners.add(listener);
    return () => this.statusListeners.delete(listener);
  }

  public onParkingData(listener: Listener<ParkingSlotDto | null>): () => void {
    this.parkingDataListeners.add(listener);
    return () => this.parkingDataListeners.delete(listener);
  }

  public onConnectedChange(listener: Listener<boolean>): () => void {
    this.connectedListeners.add(listener);
    return () => this.connectedListeners.delete(listener);
  }

  public startConnection(): void {
    this.hubConnection
      .start()
      .then(() => {
        console.log('SignalR Connected!');
        this.statusListeners.forEach((listener) => { listener('Подключено'); });
        this.setConnected(true);
        this.invokeGetParkingData();
      })
      .catch((err: unknown) => {
        console.error('SignalR Error: ', err);
        this.setConnected(false);
        const message = err instanceof Error ? err.message : String(err);
        this.statusListeners.forEach((listener) => { listener(message); });
      });

    this.hubConnection.on('ReceiveWorkStatus', (data: string) => {
      this.statusListeners.forEach((listener) => { listener(data); });
    });

    this.hubConnection.on('ReceiveParkingData', (data: ParkingSlotDto) => {
      this.parkingDataListeners.forEach((listener) => { listener(data); });
    });
  }

  public invokeGetParkingData(): void {
    void this.hubConnection.invoke('GetParkingData');
  }

  private setupSignalRListeners(): void {
    this.hubConnection.onclose(() => { this.setConnected(false); });
    this.hubConnection.onreconnecting(() => { this.setConnected(false); });
    this.hubConnection.onreconnected(() => { this.setConnected(true); });
  }

  private setConnected(value: boolean): void {
    this.connected = value;
    this.connectedListeners.forEach((listener) => { listener(value); });
  }
}

export const parkingSignalRService = new ParkingSignalRService();
