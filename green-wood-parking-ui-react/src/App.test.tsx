import { HubConnectionState } from '@microsoft/signalr';
import { act, fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

let connectedListener: ((value: boolean) => void) | undefined;

const mockService = {
  hubConnection: { state: HubConnectionState.Disconnected as HubConnectionState },
  isConnected: false,
  startConnection: vi.fn(),
  invokeGetParkingData: vi.fn(),
  onStatus: vi.fn(() => () => {}),
  onParkingData: vi.fn(() => () => {}),
  onConnectedChange: vi.fn((listener: (value: boolean) => void) => {
    connectedListener = listener;
    return () => {};
  }),
};

vi.mock('./services/parkingSignalRService', () => ({ parkingSignalRService: mockService }));

// eslint-disable-next-line @typescript-eslint/no-explicit-any -- test-only global map SDK stub
const ymaps3Stub: any = {
  ready: Promise.resolve(),
  YMap: class {
    addChild = vi.fn();
    destroy = vi.fn();
  },
  YMapDefaultSchemeLayer: class {},
  YMapFeatureDataSource: class {},
  YMapLayer: class {},
  YMapListener: class {},
  YMapFeature: class {
    update = vi.fn();
  },
};

const { default: App } = await import('./App');

describe('App', () => {
  beforeEach(() => {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any -- test-only global map SDK stub
    (globalThis as any).ymaps3 = ymaps3Stub;

    mockService.hubConnection.state = HubConnectionState.Disconnected;
    mockService.isConnected = false;
    mockService.startConnection.mockClear();
    mockService.invokeGetParkingData.mockClear();
    connectedListener = undefined;
  });

  it('renders without crashing', () => {
    render(<App />);
    expect(screen.getByText('Журнал')).toBeInTheDocument();
  });

  it('shows the "not connected" button when SignalR is disconnected', () => {
    render(<App />);

    expect(screen.getByRole('button', { name: /Не подключено/ })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^Подключено/ })).not.toBeInTheDocument();
  });

  it('shows the "connected" button when SignalR is connected', () => {
    render(<App />);

    act(() => connectedListener?.(true));

    expect(screen.getByRole('button', { name: /Подключено/ })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Не подключено/ })).not.toBeInTheDocument();
  });

  it('tries to start the SignalR connection when the status button is clicked', () => {
    render(<App />);
    mockService.startConnection.mockClear();

    fireEvent.click(screen.getByRole('button', { name: /Не подключено/ }));

    expect(mockService.startConnection).toHaveBeenCalled();
  });

  it('does not try to start the connection again if already connecting', () => {
    mockService.hubConnection.state = HubConnectionState.Connecting;
    render(<App />);
    mockService.startConnection.mockClear();

    fireEvent.click(screen.getByRole('button', { name: /Не подключено/ }));

    expect(mockService.startConnection).not.toHaveBeenCalled();
  });

  it('opens the event log dialog when the journal button is clicked', () => {
    render(<App />);

    fireEvent.click(screen.getByRole('button', { name: /Журнал/ }));

    expect(screen.getByText('История событий')).toBeInTheDocument();
  });
});
