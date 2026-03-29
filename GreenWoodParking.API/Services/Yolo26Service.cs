using Microsoft.Extensions.ML;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using YOLO26.Shared.YOLOWorker.DataStructures;

namespace GreenWoodParking.API.Services
{
    public class Yolo26Service
    {
        private readonly PredictionEnginePool<Yolo26InputData, Yolo26OutputData> _predictionEnginePool;

        private const float ModelSize = 640f;

        public Yolo26Service(PredictionEnginePool<Yolo26InputData, Yolo26OutputData> predictionEnginePool)
        {
            _predictionEnginePool = predictionEnginePool;
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

            Yolo26OutputData output = _predictionEnginePool.Predict("Yolo26m", input);
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
    }
}
