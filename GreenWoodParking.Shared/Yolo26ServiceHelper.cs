using SixLabors.ImageSharp;
using YOLO26.Shared.YOLOWorker.DataStructures;

namespace GreenWoodParking.Shared;

public static class Yolo26ServiceHelper
{
    public static bool IsSpaceOccupied(PointF[] spacePoints, YOLO26Result det)
    {
        // 1. Считаем количество точек нижней кромки внутри полигона
        var pointsToTest = GetPoints(det);
        int pointsInside = pointsToTest.Count(p => IsPointInPolygon(spacePoints, p));

        // 2. Проверяем расстояние между центрами
        var spaceCenter = GetPolygonCenter(spacePoints);
        var carCenter = new PointF((det.X1 + det.X2) / 2, det.Y2 - 7f); // Центр низа машины
        float distance = GetDistance(spaceCenter, carCenter);

        var occupancyRate = GetOccupancyRateSafe(spacePoints, det);

        // 3. Вердикт:
        // Порог в 50 пикселей можно подстроить под ваше разрешение
        bool isCenterClose = distance < 50f;
        bool hasEnoughPoints = pointsInside >= 1; // 2
        bool hasOccupancyRate = occupancyRate > 0.25f;

        return hasEnoughPoints || isCenterClose || hasOccupancyRate;
    }

    /// <summary>
    /// Получение 5-ти точек по нижней границе рамки YOLO
    /// </summary> 
    public static PointF[] GetPoints(YOLO26Result det)
    {
        float yBottom = det.Y2;
        float xLeft = det.X1;
        float xRight = det.X2;
        float width = xRight - xLeft;

        float offset = 5f;

        var pointsToTest =
            new[] { 0.30f, 0.40f, 0.50f, 0.60f, 0.70f }
            .Select(p => new PointF(xLeft + width * p, yBottom - offset));

        return pointsToTest.ToArray();
    }

    /// <summary>
    /// Алгоритм «Point in Polygon»
    /// Чтобы понять, занято ли место, нам нужно проверить, попадает ли центральная точка (или углы) найденной машины внутрь вашего четырехугольника. 
    /// Самый популярный алгоритм для этого — Ray Casting (выпуск луча)
    /// </summary> 
    public static bool IsPointInPolygon(PointF[] polygon, PointF point)
    {
        bool isInside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
                (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X))
            {
                isInside = !isInside;
            }
        }
        return isInside;
    }

    /// <summary>
    /// Находим "центр масс" парковочного места
    /// </summary> 
    public static PointF GetPolygonCenter(PointF[] points)
    {
        float centerX = points.Average(p => p.X);
        float centerY = points.Average(p => p.Y);
        return new PointF(centerX, centerY);
    }

    /// <summary>
    /// Считаем Евклидово расстояние между центром парковки и центром машины
    /// </summary> 
    public static float GetDistance(PointF p1, PointF p2)
    {
        return (float)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
    }

    /// <summary>
    /// методом «Дискретной сетки»
    /// Мы просто «заполняем» ваш полигон виртуальными точками и проверяем, сколько из них накрыл прямоугольник машины
    /// </summary> 
    public static float GetOccupancyRate(PointF[] polyPoints, YOLO26Result det)
    {
        // 1. Создаем Rectangle из детекции
        var carRect = new RectangleF(det.X1, det.Y1, det.X2 - det.X1, det.Y2 - det.Y1);

        // 2. Находим границы (Bounding Box) самого полигона парковки
        float minX = polyPoints.Min(p => p.X);
        float maxX = polyPoints.Max(p => p.X);
        float minY = polyPoints.Min(p => p.Y);
        float maxY = polyPoints.Max(p => p.Y);

        int pointsInsidePolygon = 0;
        int pointsCoveredByCar = 0;

        // 3. Сканируем область полигона сеткой (например, 15x15 шагов)
        int steps = 15;
        float stepX = (maxX - minX) / steps;
        float stepY = (maxY - minY) / steps;

        for (float x = minX; x <= maxX; x += stepX)
        {
            for (float y = minY; y <= maxY; y += stepY)
            {
                var testPoint = new PointF(x, y);

                // Проверяем, принадлежит ли точка самому парковочному месту
                if (Yolo26ServiceHelper.IsPointInPolygon(polyPoints, testPoint))
                {
                    pointsInsidePolygon++;

                    // Если да, проверяем, накрыла ли её машина
                    if (carRect.Contains(testPoint))
                    {
                        pointsCoveredByCar++;
                    }
                }
            }
        }

        if (pointsInsidePolygon == 0) return 0;

        // Возвращает число от 0.0 до 1.0 (процент заполнения)
        return (float)pointsCoveredByCar / pointsInsidePolygon;
    }

    /// <summary>
    /// Теперь мы будем проверять точку сетки не во всем carRect, а только в его нижней части.
    /// </summary> 
    public static float GetOccupancyRateSafe(PointF[] polyPoints, YOLO26Result det)
    {
        // 1. Определяем "Зону контакта" (только нижние 20-25% высоты машины)
        float contactZoneHeight = (det.Y2 - det.Y1) * 0.40f;
        float contactZoneTop = det.Y2 - contactZoneHeight;

        // Создаем уменьшенный прямоугольник, который "лежит" на асфальте
        var contactRect = new RectangleF(det.X1, contactZoneTop, det.X2 - det.X1, contactZoneHeight);

        // 2. Далее стандартная логика сетки, но используем contactRect
        float minX = polyPoints.Min(p => p.X);
        float maxX = polyPoints.Max(p => p.X);
        float minY = polyPoints.Min(p => p.Y);
        float maxY = polyPoints.Max(p => p.Y);

        int pointsInsidePolygon = 0;
        int pointsCoveredByCar = 0;

        float step = 9f;
        for (float x = minX; x <= maxX; x += step)
        {
            for (float y = minY; y <= maxY; y += step)
            {
                var testPoint = new PointF(x, y);
                if (Yolo26ServiceHelper.IsPointInPolygon(polyPoints, testPoint))
                {
                    pointsInsidePolygon++;
                    // ПРОВЕРКА: Точка парковки должна попасть именно в НИЖНЮЮ ЧАСТЬ машины
                    if (contactRect.Contains(testPoint))
                    {
                        pointsCoveredByCar++;
                    }
                }
            }
        }

        return pointsInsidePolygon == 0 ? 0 : (float)pointsCoveredByCar / pointsInsidePolygon;
    }
}