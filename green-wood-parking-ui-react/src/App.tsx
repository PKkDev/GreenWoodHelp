import { HubConnectionState } from '@microsoft/signalr';
import { useEffect, useRef, useState } from 'react';
import type {
  DomEventHandlerObject,
  LngLat,
  YMap as YMapType,
  YMapFeature as YMapFeatureType,
} from '@yandex/ymaps3-types';
import styles from './App.module.css';
import { BASE_URL } from './appConfig';
import { CameraView } from './camera-view/CameraView';
import { Button } from './components/Button/Button';
import { Icon } from './components/Icon/Icon';
import { Toast } from './components/Toast/Toast';
import { useToastQueue } from './components/Toast/useToastQueue';
import { EventLog } from './event-log/EventLog';
import type { ParkingSlotDto } from './models/parkingSlotDto';
import { parkingSLots } from './models/parkingSlots';
import { parkingSignalRService } from './services/parkingSignalRService';
import { useParkingConnectionStatus } from './services/useParkingConnectionStatus';

interface CameraViewState {
  fetchImage: () => Promise<Blob>;
}

function App() {
  const mapContainerRef = useRef<HTMLDivElement>(null);
  const mapRef = useRef<YMapType | null>(null);
  const featureMapRef = useRef(new Map<string, YMapFeatureType>());
  const parkingSlotResponseRef = useRef(new Map<string, ParkingSlotDto>());
  const eventsMapRef = useRef(new Map<Date, string>());

  const [cameraView, setCameraView] = useState<CameraViewState | null>(null);
  const [eventLogEvents, setEventLogEvents] = useState<Map<Date, string> | null>(null);

  const isConnected = useParkingConnectionStatus();
  const { current: toast, show: showToast, dismiss: dismissToast } = useToastQueue();

  useEffect(() => {
    let cancelled = false;
    const featureMap = featureMapRef.current;

    void initMap().then(() => {
      if (cancelled) {
        mapRef.current?.destroy();
        mapRef.current = null;
        return;
      }
      addAllParking();
    });

    startSignalConnection();

    const unsubscribeStatus = parkingSignalRService.onStatus((message) => {
      console.log('ReceiveWorkStatus', message);
      if (message) {
        showToast(message);
        eventsMapRef.current.set(new Date(), message);
      }
    });

    const unsubscribeData = parkingSignalRService.onParkingData((data) => {
      console.log('ReceiveParkingData', data);
      updateParkingOnMap(data);
    });

    return () => {
      cancelled = true;
      unsubscribeStatus();
      unsubscribeData();
      mapRef.current?.destroy();
      mapRef.current = null;
      featureMap.clear();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- mount-only setup, mirrors the original Angular ngAfterViewInit
  }, []);

  function startSignalConnection(): void {
    const state = parkingSignalRService.hubConnection.state;
    if (state === HubConnectionState.Connected || state === HubConnectionState.Connecting) {
      return;
    }

    parkingSignalRService.startConnection();
  }

  async function initMap(): Promise<void> {
    await ymaps3.ready;

    const { YMap, YMapDefaultSchemeLayer, YMapListener, YMapFeatureDataSource, YMapLayer } = ymaps3;

    if (!mapContainerRef.current) {
      return;
    }

    const mapInstance = new YMap(
      mapContainerRef.current,
      {
        location: {
          center: [49.340963, 53.527073],
          zoom: 18,
        },
        showScaleInCopyrights: false,
      },
      [
        new YMapDefaultSchemeLayer({}),
        new YMapFeatureDataSource({ id: 'featureSource', dynamic: false }),
        new YMapLayer({ type: 'features', source: 'featureSource', zIndex: 1400 }),
      ],
    );

    const listener = new YMapListener({
      onClick: (object: DomEventHandlerObject) => {
        if (object?.type !== 'feature') {
          return;
        }

        const featureId = object.entity.id;
        const actualResult = parkingSlotResponseRef.current.get(featureId);
        if (actualResult) {
          const fetchImage = (): Promise<Blob> => {
            const latest = parkingSlotResponseRef.current.get(featureId) ?? actualResult;
            return fetch(`${BASE_URL}/file-view/camera/${latest.imgUrl}`).then((res) => res.blob());
          };

          setCameraView({ fetchImage });
        }
      },
    });

    mapInstance.addChild(listener);
    mapRef.current = mapInstance;
  }

  function addAllParking(): void {
    const map = mapRef.current;
    if (!map) {
      return;
    }

    for (const [id, coordinates] of Object.entries(parkingSLots)) {
      const feature = getParkingPlace(id, coordinates);
      map.addChild(feature);
      featureMapRef.current.set(id, feature);
    }
  }

  function getParkingPlace(id: string, coordinates: LngLat[][]): YMapFeatureType {
    return new ymaps3.YMapFeature({
      id,
      source: 'featureSource',
      geometry: {
        type: 'Polygon',
        coordinates,
      },
      style: {
        stroke: [{ width: 2, color: '#eee' }],
        fill: 'rgb(144, 132, 131)',
      },
    });
  }

  function updateParkingOnMap(slot: ParkingSlotDto | null): void {
    if (!slot) {
      return;
    }

    parkingSlotResponseRef.current.set(slot.id, slot);

    const feature = featureMapRef.current.get(slot.id);
    if (feature) {
      feature.update({
        style: {
          stroke: [{ width: 2, color: '#eee' }],
          fill: slot.isHaveParkingSlot ? '#3bb300' : '#f43',
        },
      });
    }
  }

  function handleOpenEventLog(): void {
    setEventLogEvents(new Map(eventsMapRef.current));
  }

  return (
    <>
      <div className={styles.map} ref={mapContainerRef} />

      <div className={styles.toolBar}>
        <Button
          className={isConnected ? styles.statusConnected : styles.statusDisconnected}
          onClick={startSignalConnection}
        >
          <Icon name={isConnected ? 'wifi' : 'wifi_off'} />
          {isConnected ? 'Подключено' : 'Не подключено'}
        </Button>

        <Button onClick={handleOpenEventLog}>
          <Icon name="event_note" />
          Журнал
        </Button>
      </div>

      {cameraView && <CameraView fetchImage={cameraView.fetchImage} onClose={() => { setCameraView(null); }} />}

      {eventLogEvents && <EventLog events={eventLogEvents} onClose={() => { setEventLogEvents(null); }} />}

      {toast && <Toast toast={toast} onClose={dismissToast} />}
    </>
  );
}

export default App;
