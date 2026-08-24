import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { MatDialog } from '@angular/material/dialog';
import { HubConnectionState } from '@microsoft/signalr';
import { of } from 'rxjs';
import { App } from './app';
import { BASE_URL } from './app.config';
import { ParkingSignalRService } from './services/parking-signalR.service';

class MockParkingSignalRService {
  isConnected = signal(false);
  hubConnection = { state: HubConnectionState.Disconnected } as unknown as ParkingSignalRService['hubConnection'];
  receivedStatus$ = of<string | null>(null);
  receiveParkingData$ = of(null);
  startConnection = vi.fn();
  invokeGetParkingData = vi.fn();
}

describe('App', () => {
  let mockSignalRService: MockParkingSignalRService;

  beforeEach(() => {
    (window as any).ymaps3 = {
      ready: Promise.resolve(),
      YMap: class {
        addChild = vi.fn();
      },
      YMapDefaultSchemeLayer: class { },
      YMapDefaultFeaturesLayer: class { },
      YMapFeatureDataSource: class { },
      YMapLayer: class { },
      YMapListener: class { },
      YMapFeature: class {
        update = vi.fn();
      },
      import: vi.fn().mockResolvedValue({})
    };

    mockSignalRService = new MockParkingSignalRService();

    TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideHttpClient(),
        { provide: BASE_URL, useValue: 'https://localhost:7196' },
        { provide: ParkingSignalRService, useValue: mockSignalRService }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should show the "not connected" button when SignalR is disconnected', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.status-disconnected')).toBeTruthy();
    expect(compiled.querySelector('.status-connected')).toBeFalsy();
  });

  it('should show the "connected" button when SignalR is connected', () => {
    mockSignalRService.isConnected = signal(true);

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.status-connected')).toBeTruthy();
    expect(compiled.querySelector('.status-disconnected')).toBeFalsy();
  });

  it('should try to start the SignalR connection when the status button is clicked', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('.status-disconnected');
    button.click();

    expect(mockSignalRService.startConnection).toHaveBeenCalled();
  });

  it('should not try to start the connection again if already connecting', () => {
    mockSignalRService.hubConnection = { state: HubConnectionState.Connecting } as any;

    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    fixture.detectChanges();

    mockSignalRService.startConnection.mockClear();
    app.startSignalConnection();

    expect(mockSignalRService.startConnection).not.toHaveBeenCalled();
  });

  it('should open the event log dialog when the journal button is clicked', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const dialog = TestBed.inject(MatDialog);
    const openSpy = vi.spyOn(dialog, 'open');

    const buttons: HTMLButtonElement[] = Array.from(fixture.nativeElement.querySelectorAll('button'));
    const journalButton = buttons.find((btn) => btn.textContent?.includes('Журнал'));

    expect(journalButton).toBeTruthy();
    journalButton!.click();

    expect(openSpy).toHaveBeenCalled();
  });
});
