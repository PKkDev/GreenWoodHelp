import { AfterViewInit, Component, ElementRef, signal, viewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ParkingSignalRService } from './parking-signalR.service';

const ymaps3: typeof import('@yandex/ymaps3-types') = (window as any).ymaps3;
const { YMap, YMapDefaultSchemeLayer, YMapListener, YMapDefaultFeaturesLayer, YMapFeatureDataSource, YMapLayer } = ymaps3;

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements AfterViewInit {
  protected readonly title = signal('green-wood-parking-ui');

  private mapContainer = viewChild.required<ElementRef<HTMLDivElement>>('mapContainer');
  private map = signal<any>(null);

  constructor(private readonly parkingSignalRService: ParkingSignalRService) { }

  public ngAfterViewInit() {
    this.title.set('green-wood-parking-ui');

    this.initMap().then(() => this.addAllParking());

    this.parkingSignalRService.startConnection();
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

    this.map.set(mapInstance);

    const listener = new YMapListener({
      onClick: (object: any) => {
        console.log(object);
      }
    });

    this.map().addChild(listener);
  }

  protected addAllParking() {
    const map = this.map();

    const p29 = this.getParkingPlace(
      'p29',
      [[
        [49.341242, 53.527888],
        [49.341309, 53.527887],
        [49.341272, 53.527757],
        [49.341197, 53.527748]
      ]]);
    map.addChild(p29);

    const p28 = this.getParkingPlace(
      'p28',
      [[
        [49.341223, 53.527740],
        [49.341400, 53.527722],
        [49.341352, 53.527554],
        [49.341146, 53.527561]
      ]]);
    map.addChild(p28);

    const p30 = this.getParkingPlace(
      'p30',
      [[
        [49.341264, 53.527300],
        [49.341309, 53.527297],
        [49.341266, 53.527094],
        [49.341215, 53.527097]
      ]]);
    map.addChild(p30);

    const p31 = this.getParkingPlace(
      'p31',
      [[
        [49.341290, 53.527543],
        [49.341368, 53.527535],
        [49.341325, 53.527318],
        [49.341272, 53.527323]
      ]]);
    map.addChild(p31);

    const p21 = this.getParkingPlace(
      'p21',
      [[
        [49.341121, 53.527043],
        [49.341269, 53.527033],
        [49.341229, 53.526832],
        [49.341076, 53.526837]
      ]]);
    map.addChild(p21);

    const p22 = this.getParkingPlace(
      'p22',
      [[
        [49.341068, 53.526805],
        [49.341223, 53.526789],
        [49.341199, 53.526634],
        [49.341001, 53.526643]
      ]]);
    map.addChild(p22);

    const p13 = this.getParkingPlace(
      'p13',
      [[
        [49.340152, 53.527115],
        [49.340354, 53.527098],
        [49.340372, 53.526883],
        [49.340187, 53.526911]
      ]]);
    map.addChild(p13);

    const p14 = this.getParkingPlace(
      'p14',
      [[
        [49.340185, 53.526882],
        [49.340351, 53.526872],
        [49.340285, 53.526575],
        [49.340108, 53.526602]
      ]]);
    map.addChild(p14);

    const p15 = this.getParkingPlace(
      'p15',
      [[
        [49.340169, 53.527292],
        [49.340359, 53.527277],
        [49.340324, 53.527096],
        [49.340161, 53.527131]
      ]]);
    map.addChild(p15);

    const p16 = this.getParkingPlace(
      'p16',
      [[
        [49.340257, 53.527564],
        [49.340445, 53.527548],
        [49.340372, 53.527297],
        [49.340182, 53.527326]
      ]]);
    map.addChild(p16);
  }

  private getParkingPlace(id: string, coordinates: any[][]) {
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
        // fill: '#f43'
        // fill: '#3bb300'
      }
    });
  }
}
