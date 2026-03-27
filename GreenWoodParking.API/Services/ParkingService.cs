using FFMediaToolkit.Decoding;
using GreenWoodParking.API.Hubs;
using GreenWoodParking.API.Model;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.ML;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using YOLO26.Shared.CvatWorker;
using YOLO26.Shared.YOLOWorker.DataStructures;

namespace GreenWoodParking.API.Services
{
    public class ParkingService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHubContext<ParkingHub> _hubContext;
        private readonly Yolo26Service _yolo26Service;

        private readonly List<string> needIds = new() { "p29", "p28", "p31", "p30", "p21", "p22", "p13", "p14", "p16", "p15" };
        private readonly string url = "https://gw.videosreda.ru";
        private readonly string playlist = "playlist.m3u8";

        private readonly string _pathToScreenFolder;

        public ParkingService(
            IWebHostEnvironment env,
            IHttpClientFactory httpClientFactory,
            IHubContext<ParkingHub> hubContext,
            Yolo26Service yolo26Service)
        {
            _env = env;
            _httpClientFactory = httpClientFactory;
            _hubContext = hubContext;
            _yolo26Service = yolo26Service;

            _pathToScreenFolder = System.IO.Path.Combine(AppContext.BaseDirectory, "cameraview");
        }

        public void StartWorkForuser(string connectionId)
        {
            _hubContext.Clients.Client(connectionId).SendAsync("ReceiveStatus", "Работа запущена");

            _hubContext.Clients.Client(connectionId).SendAsync("ReceiveStatus", "Получение cameras.json");
            using HttpClient client = new(new RetryHandler(new HttpClientHandler()));
            var camerasResponse = client.GetAsync($"{url}/cameras.json").GetAwaiter().GetResult();
            var camerasResponseContent = camerasResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var cameras = JsonConvert.DeserializeObject<GreenWoodCameras>(camerasResponseContent);

            var camerasNeeded = cameras.Cameras.Where(x => needIds.Contains(x.Id));
            foreach (var camera in camerasNeeded)
            {
                _hubContext.Clients.Client(connectionId).SendAsync("ReceiveStatus", $"Получение кадров для {camera.Id}");

                var pathToScreenFolderCamera = System.IO.Path.Combine(_pathToScreenFolder, camera.Id);
                if (!Directory.Exists(pathToScreenFolderCamera))
                    Directory.CreateDirectory(pathToScreenFolderCamera);

                GetFrameFromCamera(connectionId, camera, pathToScreenFolderCamera, 0, 4);
            }

            StartSaveParking(connectionId);
        }

        private void GetFrameFromCamera(
            string connectionId,
            CameraData camera,
            string pathToScreenFolderCamera,
            int index, int max)
        {
            try
            {
                if (index >= max)
                    return;

                _hubContext.Clients.Client(connectionId).SendAsync("ReceiveStatus", $"Подключение к камере попытка - {index}");

                var cameraVideoUrl = $"{camera.Url}/{playlist}";

                var options = new MediaOptions() { VideoPixelFormat = FFMediaToolkit.Graphics.ImagePixelFormat.Rgb24, };
                using var file = MediaFile.Open(cameraVideoUrl);
                var video = file.Video;
                //Console.WriteLine($"Исходное разрешение: {video.Info.FrameSize}");

                var buffer = new byte[file.Video.FrameByteCount];
                var bmp = Image.WrapMemory<Bgr24>(buffer, file.Video.Info.FrameSize.Width, file.Video.Info.FrameSize.Height);

                var startTime = DateTime.Now;
                double skipSeconds = 1.0;
                _hubContext.Clients.Client(connectionId).SendAsync("ReceiveStatus", $"Ждём: {skipSeconds} сек.");
                while ((DateTime.Now - startTime).TotalSeconds < skipSeconds)
                {
                    if (!file.Video.TryGetNextFrame(buffer)) break;
                }
                _hubContext.Clients.Client(connectionId).SendAsync("ReceiveStatus", $"Берём фрейм");

                file.Video.TryGetNextFrame(buffer);
                var filename = $"{DateTime.Now:yyyyMMddHHmmss}.png";

                var path = System.IO.Path.Combine(pathToScreenFolderCamera, filename);
                using var resultBmp = bmp.Clone();
                resultBmp.Mutate(x => x.Resize(1080, 864, KnownResamplers.Lanczos3));
                resultBmp.SaveAsJpeg(path);
                bmp.Dispose();
                resultBmp.Dispose();
                _hubContext.Clients.Client(connectionId).SendAsync("ReceiveStatus", $"Фрейм сохранён как: {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Получение кадров ошибка - {ex.Message}");
                GetFrameFromCamera(connectionId, camera, pathToScreenFolderCamera, ++index, max);
            }
        }

        private void StartSaveParking(string connectionId)
        {
            CvatParser parser = new();
            parser.Load("p31", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p31.json"));
            parser.Load("p30", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p30.json"));
            parser.Load("p29", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p29.json"));
            parser.Load("p28", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p28.json"));
            parser.Load("p22", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p22.json"));
            parser.Load("p21", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p21.json"));
            parser.Load("p15", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p15.json"));
            parser.Load("p16", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p16.json"));
            parser.Load("p13", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p13.json"));
            parser.Load("p14", System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Files", "p14.json"));

            System.IO.DirectoryInfo diImages = new DirectoryInfo(_pathToScreenFolder);
            var folders = diImages.GetDirectories();

            foreach (var item in parser.ParkingData)
            {
                Console.WriteLine($"Обработка для {item.Id}");

                var folder = folders.First(x => x.Name == item.Id);
                if (folder != null)
                {
                    var myFile = folder.GetFiles()
                        .OrderByDescending(f => f.LastWriteTime)
                        .First();

                    var predicts = _yolo26Service.Predict(myFile.FullName);
                    _hubContext.Clients.Client(connectionId).SendAsync("ReceiveStatus", predicts);
                }
            }
        }
    }
}
