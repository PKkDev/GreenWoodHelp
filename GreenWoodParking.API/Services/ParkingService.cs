using FFMediaToolkit.Decoding;
using GreenWoodParking.API.DTO;
using GreenWoodParking.API.Hubs;
using GreenWoodParking.API.Model;
using GreenWoodParking.Shared;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace GreenWoodParking.API.Services
{
    public class ParkingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHubContext<ParkingHub> _hubContext;
        private readonly Yolo26Service _yolo26Service;

        private readonly List<string> needIds = new() { "p29", "p28", "p31", "p30", "p21", "p22", "p13", "p14", "p16", "p15" };
        //  private readonly List<string> needIds = new() { "p28" };
        private readonly string url = "https://gw.videosreda.ru";
        private readonly string playlist = "playlist.m3u8";

        private readonly string _pathToScreenFolder;

        private readonly ParkingSpacesService _parkingSpacesService;

        public ParkingService(
            IHttpClientFactory httpClientFactory,
            IHubContext<ParkingHub> hubContext,
            Yolo26Service yolo26Service,
            ParkingSpacesService parkingSpacesService)
        {
            _httpClientFactory = httpClientFactory;
            _hubContext = hubContext;
            _yolo26Service = yolo26Service;

            _pathToScreenFolder = System.IO.Path.Combine(AppContext.BaseDirectory, "cameraview");

            _parkingSpacesService = parkingSpacesService;
        }

        public void StartWorkForuser(string connectionId)
        {
            _hubContext.Clients.Client(connectionId).SendAsync("ReceiveWorkStatus", "Работа запущена");

            _hubContext.Clients.Client(connectionId).SendAsync("ReceiveWorkStatus", "Получение cameras.json");
            using HttpClient client = _httpClientFactory.CreateClient("CameraDataClient");
            var camerasResponse = client.GetAsync($"{url}/cameras.json").GetAwaiter().GetResult();
            var camerasResponseContent = camerasResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var cameras = JsonConvert.DeserializeObject<GreenWoodCameras>(camerasResponseContent);

            var camerasNeeded = cameras.Cameras.Where(x => needIds.Contains(x.Id));
            foreach (var camera in camerasNeeded)
            {
                _hubContext.Clients.Client(connectionId).SendAsync("ReceiveWorkStatus", $"Получение кадров для {camera.Id}");

                var pathToScreenFolderCamera = System.IO.Path.Combine(_pathToScreenFolder, $"{connectionId}", camera.Id);
                if (!Directory.Exists(pathToScreenFolderCamera))
                    Directory.CreateDirectory(pathToScreenFolderCamera);

                var filename = GetFrameFromCamera(connectionId, camera, pathToScreenFolderCamera, 0, 4);
                if (filename != null)
                {
                    CheckParking(connectionId, pathToScreenFolderCamera, filename, camera);
                }
            }
        }

        private string GetFrameFromCamera(
            string connectionId,
            CameraData camera,
            string pathToScreenFolderCamera,
            int index, int max)
        {
            try
            {
                if (index >= max)
                    return null;

                _hubContext.Clients.Client(connectionId).SendAsync("ReceiveWorkStatus", $"Подключение к камере попытка - {index}");

                var cameraVideoUrl = $"{camera.Url}/{playlist}";

                var options = new MediaOptions() { VideoPixelFormat = FFMediaToolkit.Graphics.ImagePixelFormat.Rgb24, };
                using var file = MediaFile.Open(cameraVideoUrl);
                var video = file.Video;

                var buffer = new byte[file.Video.FrameByteCount];
                var bmp = Image.WrapMemory<Bgr24>(buffer, file.Video.Info.FrameSize.Width, file.Video.Info.FrameSize.Height);

                //var startTime = DateTime.Now;
                //double skipSeconds = 1.0;
                //_hubContext.Clients.Client(connectionId).SendAsync("ReceiveWorkStatus", $"Ждём: {skipSeconds} сек.");
                //while ((DateTime.Now - startTime).TotalSeconds < skipSeconds)
                //{
                //    if (!file.Video.TryGetNextFrame(buffer)) break;
                //}
                _hubContext.Clients.Client(connectionId).SendAsync("ReceiveWorkStatus", $"Берём фрейм");

                file.Video.TryGetNextFrame(buffer);
                var filename = $"{DateTime.Now:yyyyMMddHHmmss}.png";

                var path = System.IO.Path.Combine(pathToScreenFolderCamera, filename);
                using var resultBmp = bmp.Clone();
                resultBmp.Mutate(x => x.Resize(1080, 864, KnownResamplers.Lanczos3));
                resultBmp.SaveAsJpeg(path);
                bmp.Dispose();
                resultBmp.Dispose();
                _hubContext.Clients.Client(connectionId).SendAsync("ReceiveWorkStatus", $"Фрейм сохранён как: {filename}");

                return filename;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Получение кадров ошибка - {ex.Message}");
                return GetFrameFromCamera(connectionId, camera, pathToScreenFolderCamera, ++index, max);
            }
        }

        private void CheckParking(
            string connectionId,
            string pathToScreenFolderCamera, string filename,
            CameraData cameraData)
        {
            var path = System.IO.Path.Combine(pathToScreenFolderCamera, filename);
            var camera = _parkingSpacesService.Parser[cameraData.Id];

            var predicts = _yolo26Service.Predict(path);
            var detectedCars = predicts.Where(x => x.LabelId == 2 || x.LabelId == 7); // Машины и грузовики

            foreach (var space in camera.ParkingSpaces)
            {
                bool occupied = detectedCars.Any(d => Yolo26ServiceHelper.IsSpaceOccupied(space.Points, d));
                space.IsOccupied = occupied;
            }

            ParkingSlotDto result = new(camera.Id, camera.ParkingSpaces.Any(x => !x.IsOccupied));
            result.ImgUrl = $"{connectionId}/{camera.Id}/{filename}";
            result.TotalCount = camera.ParkingSpaces.Count;
            result.ParkingSlotCount = camera.ParkingSpaces.Count(x => !x.IsOccupied);

            _hubContext.Clients.Client(connectionId).SendAsync("ReceiveParkingData", result);
        }
    }
}
