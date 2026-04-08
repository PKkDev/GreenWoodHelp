using FFMediaToolkit.Decoding;
using GreenWoodParking.API.DTO;
using GreenWoodParking.API.Hubs;
using GreenWoodParking.API.Model;
using GreenWoodParking.Shared;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using YOLO26.Shared.CvatWorker.Model;

namespace GreenWoodParking.API.Services
{
    public class ParkingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHubContext<ParkingHub> _hubContext;
        private readonly Yolo26Service _yolo26Service;

        private readonly List<string> needIds = new() { "p29", "p28", "p31", "p30", "p21", "p22", "p13", "p14", "p16", "p15", "p39", "p40" };
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

        public async Task StartWorkForClient(string connectionId, CancellationToken ct)
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveWorkStatus", "Работа запущена", ct);

            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveWorkStatus", "Получение cameras.json", ct);
            using HttpClient client = _httpClientFactory.CreateClient("CameraDataClient");
            var camerasResponse = await client.GetAsync($"{url}/cameras.json", ct);
            var camerasResponseContent = await camerasResponse.Content.ReadAsStringAsync(ct);
            var cameras = JsonConvert.DeserializeObject<GreenWoodCameras>(camerasResponseContent);

            Console.WriteLine("Получен cameras.json");

            if (cameras != null)
            {
                Console.WriteLine($"в cameras.json записей: {cameras.Cameras.Count}");

                var camerasNeeded = cameras.Cameras.Where(x => needIds.Contains(x.Id));

                Console.WriteLine($"в cameras.json нужных записей: {camerasNeeded.Count()}/{needIds.Count}");

                foreach (var camera in camerasNeeded)
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveWorkStatus", $"Получение кадров для {camera.Id}", ct);

                    var pathToScreenFolderCamera = System.IO.Path.Combine(_pathToScreenFolder, $"{connectionId}", camera.Id);
                    if (!Directory.Exists(pathToScreenFolderCamera))
                        Directory.CreateDirectory(pathToScreenFolderCamera);

                    var filename = await GetFrameFromCamera(connectionId, camera, pathToScreenFolderCamera, 0, 4, ct);
                    if (filename != null)
                    {
                        await CheckParking(connectionId, pathToScreenFolderCamera, filename, camera, ct);
                    }
                }
            }
            else
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveWorkStatus", "Пустой cameras.json", ct);
            }
        }

        private async Task<string?> GetFrameFromCamera(
            string connectionId,
            CameraData camera,
            string pathToScreenFolderCamera,
            int index, int max,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                if (index >= max)
                    return null;

                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveWorkStatus", $"Подключение к камере попытка - {index}", ct);

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
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveWorkStatus", $"Берём фрейм", ct);

                file.Video.TryGetNextFrame(buffer);
                var filename = $"{DateTime.Now:yyyyMMddHHmmss}.png";

                var path = System.IO.Path.Combine(pathToScreenFolderCamera, filename);
                using var resultBmp = bmp.Clone();
                resultBmp.Mutate(x => x.Resize(1080, 864, KnownResamplers.Lanczos3));
                resultBmp.SaveAsJpeg(path);
                bmp.Dispose();
                resultBmp.Dispose();
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveWorkStatus", $"Фрейм сохранён как: {filename}", ct);

                return filename;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Получение кадров ошибка - {ex}");
                return await GetFrameFromCamera(connectionId, camera, pathToScreenFolderCamera, ++index, max, ct);
            }
        }

        private async Task CheckParking(
            string connectionId,
            string pathToScreenFolderCamera, string filename,
            CameraData cameraData,
            CancellationToken ct)
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

            var freeSpaces = camera.ParkingSpaces.Where(x => !x.IsOccupied);

            await DrawParking(path, path, freeSpaces, ct);

            ParkingSlotDto result = new(camera.Id, camera.ParkingSpaces.Any(x => !x.IsOccupied));
            result.ImgUrl = $"{connectionId}/{camera.Id}/{filename}";
            result.TotalCount = camera.ParkingSpaces.Count;
            result.ParkingSlotCount = camera.ParkingSpaces.Count(x => !x.IsOccupied);

            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveParkingData", result, ct);
        }

        public async Task DrawParking(string sourcePath, string outputPath, IEnumerable<ParkingSpace> freeSpaces, CancellationToken ct)
        {
            using var image = await Image.LoadAsync<Rgb24>(sourcePath, ct);
            var font = SystemFonts.CreateFont("Arial", 14, FontStyle.Bold);

            image.Mutate(ctx =>
            {
                foreach (var space in freeSpaces)
                {
                    // Зеленый - свободно
                    Color color = Color.Lime;
                    // Подписываем номер места  
                    ctx.DrawText(space.Id.ToString(), font, color, new PointF(space.Points[0].X + 5, space.Points[0].Y + 5));
                    // Рисуем зону парковки
                    ctx.DrawPolygon(color, 2f, space.Points);
                }

            });

            await image.SaveAsync(outputPath, ct);
        }

    }
}
