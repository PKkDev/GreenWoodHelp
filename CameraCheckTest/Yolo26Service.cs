using Microsoft.ML;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Net;
using YOLO26.Shared.CvatWorker.Model;
using YOLO26.Shared.YOLOWorker.DataStructures;

namespace ObjectDetection.WinApp.Services
{
    public class Yolo26Service
    {
        private readonly MLContext _mlContext;
        private DataViewSchema _modelSchema { get; set; }
        private PredictionEngine<Yolo26InputData, Yolo26OutputData> _predictionEngine;

        private const float ModelSize = 640f;

        public Yolo26Service()
        {
            _mlContext = new();
            LoadModel();
        }

        private void LoadModel()
        {
            if (_modelSchema != null && _predictionEngine != null)
                return;

            var modelLocation = System.IO.Path.Combine(Environment.CurrentDirectory, "YoloModel\\yolo26m.zip");
            FileInfo fi = new(modelLocation);
            if (!fi.Exists)
                throw new Exception("saved yolo model not found");

            ITransformer trainedModel = _mlContext.Model.Load(modelLocation, out var modelSchema);
            _modelSchema = modelSchema;
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<Yolo26InputData, Yolo26OutputData>(trainedModel);
        }

        public List<YOLO26Result> Predict(string imagePath)
        {
            using Image<Rgb24> image = Image.Load<Rgb24>(imagePath);

            //image.Mutate(x => x
            //.Contrast(1.2f) // Усиливаем разницу между машиной и асфальтом
            //.Brightness(1.1f) // Немного поднимаем общую яркость
            //.GaussianSharpen() // Делаем грани четче, чтобы YOLO было за что зацепиться
            //);
            //image.Save(imagePath);

            int orgW = image.Width;
            int orgH = image.Height;

            var imageData = PrepareImage(image);
            var input = new Yolo26InputData { Image = imageData };

            Yolo26OutputData output = _predictionEngine.Predict(input);
            var detections = new List<YOLO26Result>();

            // Разбор 300 предсказаний (каждое по 6 значений)
            for (int i = 0; i < 300; i++)
            {
                int offset = i * 6;
                float score = output.Predictions[offset + 4];

                if (score > 0.2f) // Порог уверенности 0.4f
                {
                    float x1 = output.Predictions[offset + 0];
                    float y1 = output.Predictions[offset + 1];
                    float x2 = output.Predictions[offset + 2];
                    float y2 = output.Predictions[offset + 3];
                    int classId = (int)output.Predictions[offset + 5];

                    // Масштабируем обратно под оригинал (если модель 640x640)
                    float x1_real = x1 * (orgW / ModelSize);
                    float y1_real = y1 * (orgH / ModelSize);
                    float x2_real = x2 * (orgW / ModelSize);
                    float y2_real = y2 * (orgH / ModelSize);

                    Console.WriteLine($"Найден объект {classId} [Conf: {score:P0}] на {x1_real:0},{y1_real:0}");

                    detections.Add(new YOLO26Result
                    {
                        X1 = x1_real,
                        Y1 = y1_real,
                        X2 = x2_real,
                        Y2 = y2_real,
                        Score = score,
                        LabelId = (int)output.Predictions[offset + 5]
                    });
                }
            }

            Console.WriteLine($"Найдено объектов {detections.Count}");

            return detections;
        }

        public async Task<List<YOLO26Result>> PredictAsync(string imagePath)
        {
            var t = Task.Run(() =>
            {
                return Predict(imagePath);
            });
            t.Wait();
            return t.Result;
        }

        /// <summary>
        /// Модель ожидает массив float[], 
        /// где пиксели нормализованы (0..1) и идут в порядке RGB (а не BGR) и в формате CHW (сначала все красные, потом зеленые, потом синие).
        /// </summary> 
        public float[] PrepareImage(Image<Rgb24> image)
        {
            image.Mutate(x => x.Resize(640, 640));

            float[] fc = new float[1 * 3 * 640 * 640];

            // Заполняем массив в формате CHW (Planar)
            for (int y = 0; y < 640; y++)
            {
                for (int x = 0; x < 640; x++)
                {
                    var pixel = image[x, y];
                    fc[0 * 640 * 640 + y * 640 + x] = pixel.R / 255f;
                    fc[1 * 640 * 640 + y * 640 + x] = pixel.G / 255f;
                    fc[2 * 640 * 640 + y * 640 + x] = pixel.B / 255f;
                }
            }
            return fc;
        }

        /// <summary>
        /// Модель ожидает массив float[], 
        /// где пиксели нормализованы (0..1) и идут в порядке RGB (а не BGR) и в формате CHW (сначала все красные, потом зеленые, потом синие).
        /// </summary> 
        public float[] PrepareImage(string imagePath)
        {
            using Image<Rgb24> image = Image.Load<Rgb24>(imagePath);

            return PrepareImage(image);
        }


        public void DrawDetectionsAndParking(string sourcePath, string outputPath, List<ParkingSpace> spaces, List<YOLO26Result> detections)
        {
            //var imagePathProcessed = $"{pathToParkingFolder}\\{myFile.Name}-detected.png";

            DrawDetectedCars(sourcePath, outputPath, detections);
            DrawParking(outputPath, outputPath, spaces, detections);
            ////DrawOccupancyGridMap(outputPath, outputPath, spaces, detections);
            DrawContactGridMap(outputPath, outputPath, spaces, detections);
        }

        public void DrawDetectedCars(string sourcePath, string outputPath, List<YOLO26Result> detections)
        {
            using var image = Image.Load<Rgb24>(sourcePath);
            var font = SystemFonts.CreateFont("Arial", 14, FontStyle.Bold);
            var pen = Pens.Solid(Color.Lime, 1f);

            image.Mutate(ctx =>
            {
                foreach (var det in detections)
                {
                    if (det.LabelId != 2 && det.LabelId != 7) // Машины и грузовики
                        continue;

                    // Отрисовываем стандартный зеленый бокс YOLO
                    var carRect = new RectangleF(det.X1, det.Y1, det.X2 - det.X1, det.Y2 - det.Y1);
                    ctx.Draw(pen, carRect);

                    string label = $"ID:{det.LabelId} {det.Score:P0}";
                    FontRectangle textSize = TextMeasurer.MeasureBounds(label, new TextOptions(font));

                    // Рисуем полупрозрачный фон для текста для лучшей читаемости
                    var textBgRect = new RectangleF(det.X1, det.Y1 - textSize.Height - 2, textSize.Width + 4, textSize.Height + 2);
                    ctx.Fill(Color.Lime.WithAlpha(0.6f), textBgRect);

                    // Отрисовываем label YOLO 
                    ctx.DrawText(label, font, Color.White, new PointF(det.X1 + 2, det.Y1 - textSize.Height - 1));
                }
            });

            image.Save(outputPath);
        }

        public void DrawParking(string sourcePath, string outputPath, List<ParkingSpace> spaces, List<YOLO26Result> detections)
        {
            using var image = Image.Load<Rgb24>(sourcePath);
            var font = SystemFonts.CreateFont("Arial", 14, FontStyle.Bold);

            var detectedCars = detections.Where(x => x.LabelId == 2 || x.LabelId == 7); // Машины и грузовики

            image.Mutate(ctx =>
            {
                foreach (var space in spaces)
                {
                    bool occupied = detectedCars.Any(d => IsSpaceOccupied(space.Points, d));
                    space.IsOccupied = occupied;
                    Console.WriteLine($"Место #{space.Id}: {(space.IsOccupied ? "ЗАНЯТО" : "СВОБОДНО")}");
                    // Красный - занято, Зеленый - свободно
                    Color color = occupied ? Color.Red : Color.Lime;

                    // Подписываем номер места  
                    ctx.DrawText(space.Id.ToString(), font, color, new PointF(space.Points[0].X + 5, space.Points[0].Y + 5));
                    // Рисуем зону парковки
                    ctx.DrawPolygon(color, 2f, space.Points);
                }

                #region Алгоритм "расстояние между центрами"
                foreach (var det in detectedCars)
                {
                    // Рисуем центр машины
                    var carBottomCenter = new PointF((det.X1 + det.X2) / 2, det.Y2 - 7f);
                    ctx.Fill(Color.Yellow, new EllipsePolygon(carBottomCenter, 2f));
                    ctx.DrawText("С.C.", font, Color.Yellow, new PointF(carBottomCenter.X + 5, carBottomCenter.Y + 5));

                    foreach (var space in spaces)
                    {
                        // Рисуем центр пакровки
                        var spaceCenter = GetPolygonCenter(space.Points);
                        ctx.Fill(Color.Yellow, new EllipsePolygon(spaceCenter, 2f));
                        ctx.DrawText("P.C.", font, Color.Yellow, new PointF(spaceCenter.X + 5, spaceCenter.Y + 5));

                        float distance = GetDistance(spaceCenter, carBottomCenter);

                        // Если центр машины находится в радиусе допуска - рисуем линию
                        if (distance < 50f)
                        {
                            // Рисуем пунктирную линию
                            ctx.DrawLine(Color.Yellow, 2f, new PointF[] { spaceCenter, carBottomCenter });
                            // Подписываем полученное расстояние 
                            var middlePoint = new PointF((spaceCenter.X + carBottomCenter.X) / 2f, (spaceCenter.Y + carBottomCenter.Y) / 2f);
                            ctx.DrawText(distance.ToString("F1"), font, Color.Yellow, middlePoint);
                        }
                    }
                }
                #endregion Алгоритм "расстояние между центрами"

                #region Алгоритм Point in Polygon
                foreach (var det in detectedCars)
                {
                    var pointsToTest = GetPoints(det);
                    // Рисуем каждую точку как маленький красный кружок 
                    foreach (var p in pointsToTest)
                    {
                        var pointCheck = spaces.Any(x => IsPointInPolygon(x.Points, p));
                        // Красный - точка попала в бокс парковки, Зеленый - не попала
                        Color pointColor = pointCheck ? Color.Red : Color.Lime;
                        ctx.Fill(pointColor, new EllipsePolygon(p, 2f));
                        //ctx.DrawText($"C.D.", font, Color.Red, new PointF(p.X + 5, p.Y + 5));
                    }
                }
                #endregion Алгоритм Point in Polygon

            });

            image.Save(outputPath);
        }

        public void DrawOccupancyGridMap(string sourcePath, string outputPath, List<ParkingSpace> spaces, List<YOLO26Result> detections)
        {
            using var image = Image.Load<Rgb24>(sourcePath);

            var font = SystemFonts.CreateFont("Arial", 14, FontStyle.Bold);

            image.Mutate(ctx =>
            {
                foreach (var space in spaces)
                {
                    // Находим границы полигона для сканирования
                    float minX = space.Points.Min(p => p.X);
                    float maxX = space.Points.Max(p => p.X);
                    float minY = space.Points.Min(p => p.Y);
                    float maxY = space.Points.Max(p => p.Y);

                    int totalPoints = 0;
                    int occupiedPoints = 0;

                    // Шаг сетки (чем меньше, тем точнее, но медленнее)
                    float step = 12f;

                    // Сканируем область
                    for (float x = minX; x <= maxX; x += step)
                    {
                        for (float y = minY; y <= maxY; y += step)
                        {
                            var testPoint = new PointF(x, y);

                            // Если точка внутри парковочного полигона
                            if (IsPointInPolygon(space.Points, testPoint))
                            {
                                totalPoints++;
                                bool isCovered = false;

                                // Проверяем, накрыла ли её хоть одна машина (её Rectangle YOLO)
                                foreach (var det in detections.Where(d => d.LabelId == 2))
                                {
                                    var carRect = new RectangleF(det.X1, det.Y1, det.X2 - det.X1, det.Y2 - det.Y1);
                                    if (carRect.Contains(testPoint))
                                    {
                                        isCovered = true;
                                        break;
                                    }
                                }

                                // Рисуем точку: Красная - занято, Зеленая - свободно
                                Color pointColor = isCovered ? Color.Red : Color.Lime;
                                if (isCovered) occupiedPoints++;

                                // EllipsePolygon создает маленький кружок радиусом 2 пикселя
                                ctx.Fill(pointColor, new EllipsePolygon(testPoint, 2f));
                            }
                        }
                    }

                    // Выводим процент занятости над полигоном
                    if (totalPoints > 0)
                    {
                        float rate = (float)occupiedPoints / totalPoints;
                        string text = $"Occ: {(rate * 100):F0}%";
                        ctx.DrawText(text, font, Color.White, new PointF(minX, minY - 20));
                    }
                }
            });

            image.Save(outputPath);
        }

        public void DrawContactGridMap(string sourcePath, string outputPath, List<ParkingSpace> spaces, List<YOLO26Result> detections)
        {
            using var image = Image.Load<Rgb24>(sourcePath);
            var font = SystemFonts.CreateFont("Arial", 14, FontStyle.Bold);

            image.Mutate(ctx =>
            {
                // 1. Сначала рисуем синие рамки "Зоны контакта" для всех машин
                foreach (var det in detections)
                {
                    if (det.LabelId != 2 && det.LabelId != 7) // Машины и грузовики
                        continue;

                    // Вычисляем синюю "Зону контакта" (нижние 25% высоты)
                    float contactZoneHeight = (det.Y2 - det.Y1) * 0.35f;
                    float contactZoneTop = det.Y2 - contactZoneHeight;
                    var contactRect = new RectangleF(det.X1, contactZoneTop, det.X2 - det.X1, contactZoneHeight);

                    // Рисуем синюю рамку "Зоны контакта"
                    ctx.Draw(Color.Blue, 1f, contactRect);

                    // Подпишем её
                    ctx.DrawText("Contact Zone (0.35f)", font, Color.Blue, new PointF(det.X1, contactZoneTop - 20));
                }

                // 2. Затем рисуем точки сетки внутри парковочных полигонов
                foreach (var space in spaces)
                {
                    // Находим границы полигона для сканирования
                    float minX = space.Points.Min(p => p.X);
                    float maxX = space.Points.Max(p => p.X);
                    float minY = space.Points.Min(p => p.Y);
                    float maxY = space.Points.Max(p => p.Y);

                    // Шаг сетки (чем меньше, тем точнее, но медленнее)
                    float step = 12f;

                    // Сканируем область
                    for (float x = minX; x <= maxX; x += step)
                    {
                        for (float y = minY; y <= maxY; y += step)
                        {
                            var testPoint = new PointF(x, y);

                            // Если точка внутри парковочного полигона
                            if (IsPointInPolygon(space.Points, testPoint))
                            {
                                bool isCoveredByContactZone = false;

                                // Проверяем, накрыла ли её синяя "Зона контакта" хоть одной машины
                                foreach (var det in detections.Where(d => d.LabelId == 2))
                                {
                                    float contactZoneHeight = (det.Y2 - det.Y1) * 0.35f;
                                    float contactZoneTop = det.Y2 - contactZoneHeight;
                                    var contactRect = new RectangleF(det.X1, contactZoneTop, det.X2 - det.X1, contactZoneHeight);

                                    if (contactRect.Contains(testPoint))
                                    {
                                        isCoveredByContactZone = true;
                                        break;
                                    }
                                }

                                // Рисуем точку: Красная - занято, Зеленая - свободно
                                Color pointColor = isCoveredByContactZone ? Color.Red : Color.Lime;

                                // EllipsePolygon создает маленький кружок радиусом 2 пикселя
                                ctx.Fill(pointColor, new EllipsePolygon(testPoint, 2f));
                            }
                        }
                    }
                }
            });

            image.Save(outputPath);
        }

        /// <summary>
        /// Получение 5-ти точек по нижней границе рамки YOLO
        /// </summary> 
        public PointF[] GetPoints(YOLO26Result det)
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

        public bool IsSpaceOccupied(PointF[] spacePoints, YOLO26Result det)
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
            bool hasOccupancyRate = occupancyRate > 0.4f;

            return hasEnoughPoints || isCenterClose || hasOccupancyRate;
        }

        /// <summary>
        /// Алгоритм «Point in Polygon»
        /// Чтобы понять, занято ли место, нам нужно проверить, попадает ли центральная точка (или углы) найденной машины внутрь вашего четырехугольника. 
        /// Самый популярный алгоритм для этого — Ray Casting (выпуск луча)
        /// </summary> 
        public bool IsPointInPolygon(PointF[] polygon, PointF point)
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
        public PointF GetPolygonCenter(PointF[] points)
        {
            float centerX = points.Average(p => p.X);
            float centerY = points.Average(p => p.Y);
            return new PointF(centerX, centerY);
        }

        /// <summary>
        /// Считаем Евклидово расстояние между центром парковки и центром машины
        /// </summary> 
        public float GetDistance(PointF p1, PointF p2)
        {
            return (float)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        /// <summary>
        /// методом «Дискретной сетки»
        /// Мы просто «заполняем» ваш полигон виртуальными точками и проверяем, сколько из них накрыл прямоугольник машины
        /// </summary> 
        public float GetOccupancyRate(PointF[] polyPoints, YOLO26Result det)
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
                    if (IsPointInPolygon(polyPoints, testPoint))
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
        public float GetOccupancyRateSafe(PointF[] polyPoints, YOLO26Result det)
        {
            // 1. Определяем "Зону контакта" (только нижние 20-25% высоты машины)
            float contactZoneHeight = (det.Y2 - det.Y1) * 0.35f;
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

            float step = 12f;
            for (float x = minX; x <= maxX; x += step)
            {
                for (float y = minY; y <= maxY; y += step)
                {
                    var testPoint = new PointF(x, y);
                    if (IsPointInPolygon(polyPoints, testPoint))
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
}
