import { HttpClient } from '@angular/common/http';
import { AfterViewInit, Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { RouterOutlet } from '@angular/router';
import { HubConnectionState } from '@microsoft/signalr';
import { CameraViewComponent } from './camera-view/camera-view.component';
import { EventLogComponent } from './event-log/event-log.component';
import { ParkingSignalRService } from './parking-signalR.service';
import { ParkingSlotDto } from './parking-slot-dto';
import { parkingSLots } from './parking-slots';

import type { YMapFeature as YMapFeatureType, YMap as YMapType } from '@yandex/ymaps3-types';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, MatSnackBarModule, MatButtonModule, MatIconModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements AfterViewInit {
  protected readonly title = signal('green-wood-parking-ui');

  private mapContainer = viewChild.required<ElementRef<HTMLDivElement>>('mapContainer');
  private map = signal<YMapType | null>(null);

  private parkingSlotMap = new Map<string, number[][][]>();
  private featureMap = new Map<string, YMapFeatureType>();
  private parkingSlotResponse = new Map<string, ParkingSlotDto>();

  private eventsMap = new Map<Date, string>();

  private readonly _dialog = inject(MatDialog);
  private readonly _snackBar = inject(MatSnackBar);
  public readonly _parkingSignalRService = inject(ParkingSignalRService);
  public readonly _httpClient = inject(HttpClient);

  constructor() {
    this.parkingSlotMap.set('p29', parkingSLots['p29']);
    this.parkingSlotMap.set('p28', parkingSLots['p28']);
    this.parkingSlotMap.set('p30', parkingSLots['p30']);
    this.parkingSlotMap.set('p31', parkingSLots['p31']);
    this.parkingSlotMap.set('p21', parkingSLots['p21']);
    this.parkingSlotMap.set('p22', parkingSLots['p22']);
    this.parkingSlotMap.set('p13', parkingSLots['p13']);
    this.parkingSlotMap.set('p14', parkingSLots['p14']);
    this.parkingSlotMap.set('p15', parkingSLots['p15']);
    this.parkingSlotMap.set('p16', parkingSLots['p16']);
  }

  public ngAfterViewInit() {
    this.title.set('green-wood-parking-ui');

    this.initMap().then(() => this.addAllParking());

    this.startSignalConnection();

    this._parkingSignalRService.receivedStatus$.subscribe((message: string | null) => {
      console.log('ReceiveWorkStatus', message);
      if (message) {
        this._snackBar.open(message, 'Закрыть', {
          duration: 1000 * 3333,
          verticalPosition: 'top',
          horizontalPosition: 'right',

        });
        this.eventsMap.set(new Date(), message);
      }
    });

    this._parkingSignalRService.receiveParkingData$.subscribe((data: ParkingSlotDto | null) => {
      console.log('ReceiveParkingData', data);
      this.updateParkingOnMap(data);
    });
  }

  public startSignalConnection() {
    const state = this._parkingSignalRService.hubConnection.state;
    if (state === HubConnectionState.Connected || state === HubConnectionState.Connecting) {
      return
    }

    this._parkingSignalRService.startConnection();
  }

  public onRefresh(): void { }

  public onOpenEventLog(): void {
    this._dialog.open(EventLogComponent, {
      data: { events: new Map(this.eventsMap) }
    });
  }

  private initMap(): Promise<void> {
    return ymaps3.ready.then(() => {

      const { YMap, YMapDefaultSchemeLayer, YMapListener, YMapFeatureDataSource, YMapLayer } = ymaps3;

      const mapInstance = new YMap(this.mapContainer().nativeElement, {
        location: {
          center: [49.340300, 53.526747],
          zoom: 18
        },
        showScaleInCopyrights: false
      }, [
        new YMapDefaultSchemeLayer({}),
        new YMapFeatureDataSource({ id: 'featureSource', dynamic: false }),
        new YMapLayer({ type: 'features', source: 'featureSource', zIndex: 1400 }),
      ]);

      const listener = new YMapListener({
        onClick: (object: any) => {
          if (!object) {
            return
          }

          const actualResult = this.parkingSlotResponse.get(object.entity.id);
          if (actualResult) {
            this._httpClient.get(`https://localhost:7196/api/file-view/camera/${actualResult.imgUrl}`, { responseType: 'blob' })
              .subscribe({
                next: (value) => {
                  console.log(value)
                  this._dialog.open(CameraViewComponent, {
                    maxWidth: '95vw',
                    maxHeight: '95vh',
                    panelClass: 'full-screen-modal',
                    data: { file: value }
                  });
                },
                error: (err) => console.error(err),
              })
          }
        }
      });

      mapInstance.addChild(listener);

      this.map.set(mapInstance);
    });
  }

  protected addAllParking() {
    const map = this.map();

    if (!map) {
      return;
    }

    for (const key of this.parkingSlotMap.keys()) {
      const value = this.parkingSlotMap.get(key);
      if (value) {
        const feature = this.getParkingPlace(key, value)
        map.addChild(feature);
        this.featureMap.set(key, feature);
      }
    }
  }

  private getParkingPlace(id: string, coordinates: any[][]): YMapFeatureType {
    return new ymaps3.YMapFeature({
      id: id,
      source: 'featureSource',
      geometry: {
        type: 'Polygon',
        coordinates: coordinates
      },
      style: {
        stroke: [{ width: 2, color: '#eee' }],
        fill: 'rgb(144, 132, 131)'
      }
    });
  }

  private updateParkingOnMap(slot: ParkingSlotDto | null) {
    if (!slot) {
      return
    }

    this.parkingSlotResponse.set(slot.id, slot);

    if (this.featureMap.has(slot.id)) {
      const feature = this.featureMap.get(slot.id);
      if (feature) {
        feature.update({
          style: {
            stroke: [{ width: 2, color: '#eee' }],
            fill: slot.isHaveParkingSlot ? '#3bb300' : '#f43',
          }
        });
      }
    }
  }
}