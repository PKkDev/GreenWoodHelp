import { AfterViewInit, Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ParkingSignalRService } from './parking-signalR.service';
import { ParkingSlotDto } from './parking-slot-dto';
import { parkingSLots } from './parking-slots';

import { HttpClient } from '@angular/common/http';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import type { YMapFeature as YMapFeatureType } from '@yandex/ymaps3-types';
import { CameraViewComponent } from './camera-view/camera-view.component';
const ymaps3: typeof import('@yandex/ymaps3-types') = (window as any).ymaps3;
const { YMap, YMapDefaultSchemeLayer, YMapListener, YMapFeatureDataSource, YMapLayer } = ymaps3;

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, MatSnackBarModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements AfterViewInit {
  protected readonly title = signal('green-wood-parking-ui');

  private mapContainer = viewChild.required<ElementRef<HTMLDivElement>>('mapContainer');
  private map = signal<any | null>(null);

  private parkingSlotMap = new Map<string, number[][][]>();
  private featureMap = new Map<string, YMapFeatureType>();
  private parkingSlotResponse = new Map<string, ParkingSlotDto>();

  private readonly dialog = inject(MatDialog);
  private readonly _snackBar = inject(MatSnackBar);

  constructor(
    private readonly parkingSignalRService: ParkingSignalRService,
    private readonly httpClient: HttpClient) {
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

    this.parkingSignalRService.startConnection();

    this.parkingSignalRService.receivedStatus$.subscribe((data: string) => {
      console.log('ReceiveWorkStatus', data);
      this._snackBar.open(data, 'Close', {
        duration: 2000,
        verticalPosition: 'top',
        horizontalPosition: 'right'
      });
    });

    this.parkingSignalRService.receiveParkingData$.subscribe((data: ParkingSlotDto | null) => {
      console.log('ReceiveParkingData', data);
      this.updateParkingOnMap(data);
    });
  }

  private async initMap() {
    await ymaps3.ready;

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
          this.httpClient.get(`https://localhost:7196/api/file-view/camera/${actualResult.imgUrl}`, { responseType: 'blob' })
            .subscribe({
              next: (value) => {
                console.log(value)
                this.dialog.open(CameraViewComponent, {
                  maxWidth: '95vw',
                  maxHeight: '95vh',
                  panelClass: 'full-screen-modal', // Кастомный класс для стилей
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