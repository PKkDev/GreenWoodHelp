import { Injectable } from '@angular/core';

interface Point { x: number; y: number; }
interface GeoPoint { lon: number; lat: number; }

@Injectable({
  providedIn: 'root'
})
export class CoordinateTransformerService {

  /**
   * Коэффициенты трансформации (рассчитываются один раз при калибровке)
   */
  private matrix = {
    a: 0, b: 0, c: 0,
    d: 0, e: 0, f: 0
  };

  /**
   * Калибровка по 3 точкам (минимально необходимо для аффинного преобразования)
   * p - точки на видео (px), g - точки на карте (lon/lat)
   */
  public calibrate(p: Point[], g: GeoPoint[]) {
    const [p1, p2, p3] = p;
    const [g1, g2, g3] = g;

    const det = (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

    this.matrix.a = ((g1.lon - g3.lon) * (p2.y - p3.y) - (g2.lon - g3.lon) * (p1.y - p3.y)) / det;
    this.matrix.b = ((g2.lon - g3.lon) * (p1.x - p3.x) - (g1.lon - g3.lon) * (p2.x - p3.x)) / det;
    this.matrix.c = g1.lon - this.matrix.a * p1.x - this.matrix.b * p1.y;

    this.matrix.d = ((g1.lat - g3.lat) * (p2.y - p3.y) - (g2.lat - g3.lat) * (p1.y - p3.y)) / det;
    this.matrix.e = ((g2.lat - g3.lat) * (p1.x - p3.x) - (g1.lat - g3.lat) * (p2.x - p3.x)) / det;
    this.matrix.f = g1.lat - this.matrix.d * p1.x - this.matrix.e * p1.y;
  }

  /**
  * Перевод одной точки из пикселей в GPS
  */
  public pixelToGeo(p: Point): GeoPoint {
    return {
      lon: this.matrix.a * p.x + this.matrix.b * p.y + this.matrix.c,
      lat: this.matrix.d * p.x + this.matrix.e * p.y + this.matrix.f
    };
  }

  /**
   * Трансформация всего полигона (массива координат от YOLO)
   */
  public transformPolygon(pixels: Point[]): number[][] {
    return pixels.map(p => {
      const geo = this.pixelToGeo(p);
      return [geo.lon, geo.lat];
    });
  }

}
