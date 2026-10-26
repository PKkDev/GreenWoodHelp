using FFMediaToolkit.Decoding;
using GreenWoodParking.API.DTO;
using GreenWoodParking.API.Hubs;
using GreenWoodParking.API.Model;
using GreenWoodParking.Shared;
using GreenWoodParking.Shared.CVAT.Model;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing; 

namespace GreenWoodParking.API.Services
{
    public class ParkingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHubContext<ParkingHub> _hubContext;
        private readonly Yolo26Service _yolo26Service;

        private readonly string[] needIds = ["p29", "p28", "p31", "p30", "p21", "p22", "p13", "p14", "p16", "p15", "p39", "p40", "p38", "p37", "p24", "p25"];
        //  private readonly string[] needIds = ["p38", "p37", "p24", "p25"];
        private readonly string url = "https://gw.videosreda.ru";
        private readonly string playlist = "playlist.m3u8";

        private const int MaxFrameAttempts = 4;
        private const int MaxSkippedFrames = 300; // защита от потока без корректных timestamp
        private static readonly TimeSpan FrameRetryDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan SkipDuration = TimeSpan.FromSeconds(1);

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
            await _hubContext.Clients.Client(connectionId).SendAsync(SignalMethods.ReceiveWorkStatus, "Работа запущена", ct);

            await _hubContext.Clients.Client(connectionId).SendAsync(SignalMethods.ReceiveWorkStatus, "Получение cameras.json", ct);
            using HttpClient client = _httpClientFactory.CreateClient("CameraDataClient");
            using var camerasResponse = await client.GetAsync($"{url}/cameras.json", ct);
            if (!camerasResponse.IsSuccessStatusCode)
            {
                await _hubContext.Clients.Client(connectionId).SendAsync(SignalMethods.ReceiveWorkStatus, $"Ошибка получения cameras.json: {(int)camerasResponse.StatusCode}", ct);
                return;
            }
            var camerasResponseContent = await camerasResponse.Content.ReadAsStringAsync(ct);
            var cameras = JsonConvert.DeserializeObject<GreenWoodCameras>(camerasResponseContent);

            Console.WriteLine("Получен cameras.json");

            if (cameras != null)
            {
                Console.WriteLine($"в cameras.json записей: {cameras.Cameras.Count}");

                var camerasNeeded = cameras.Cameras.Where(x => needIds.Contains(x.Id));

                Console.WriteLine($"в cameras.json нужных записей: {camerasNeeded.Count()}/{needIds.Length}");

                List<Task<(string?, CameraData, string)>> tasks = [];
                foreach (var camera in camerasNeeded)
                {
                    var currentCamera = camera;
                    tasks.Add(Task.Run(async () =>
                    {
                        await _hubContext.Clients.Client(connectionId).SendAsync(SignalMethods.ReceiveWorkStatus, $"Получение кадров для {currentCamera.Id}", ct);

                        var pathToScreenFolderCamera = System.IO.Path.Combine(_pathToScreenFolder, $"{connectionId}", currentCamera.Id);
                        if (!Directory.Exists(pathToScreenFolderCamera))
                            Directory.CreateDirectory(pathToScreenFolderCamera);

                        var filename = await GetFrameFromCamera(connectionId, currentCamera, pathToScreenFolderCamera, ct);
                        return (filename, currentCamera, pathToScreenFolderCamera);

                    }, ct));
                }
                await Task.WhenAll(tasks);

                foreach (var task in tasks)
                {
                    var filename = task.Result.Item1;
                    var currentCamera = task.Result.Item2;
                    var pathToScreenFolderCamera = task.Result.Item3;

                    if (!string.IsNullOrEmpty(filename))
                    {
                        await CheckParking(connectionId, pathToScreenFolderCamera, filename, currentCamera, ct);
                    }
                }

                ////List<Task> tasks = [];
                //foreach (var camera in camerasNeeded)
                //{
                //    var currentCamera = camera;
                //    //tasks.Add(Task.Run(async () =>
                //    //{
                //    await _hubContext.Clients.Client(connectionId).SendAsync(SignalMethods.ReceiveWorkStatus, $"Получение кадров для {currentCamera.Id}", ct);

                //    var pathToScreenFolderCamera = System.IO.Path.Combine(_pathToScreenFolder, $"{connectionId}", currentCamera.Id);
                //    if (!Directory.Exists(pathToScreenFolderCamera))
                //        Directory.CreateDirectory(pathToScreenFolderCamera);

                //    var filename = await GetFrameFromCamera(connectionId, currentCamera, pathToScreenFolderCamera, 0, 4, ct);
                //    if (filename != null)
                //    {
                //        await CheckParking(connectionId, pathToScreenFolderCamera, filename, currentCamera, ct);
                //    }
                //    //}, ct));
                //}
                ////Task.WhenAll(tasks).GetAwaiter().GetResult();
            }
            else
            {
                await _hubContext.Clients.Client(connectionId).SendAsync(SignalMethods.ReceiveWorkStatus, "Пустой cameras.json", ct);
            }
        }

        private async Task<string?> GetFrameFromCamera(
            string connectionId,
            CameraData camera,
            string pathToScreenFolderCamera,
            CancellationToken ct)
        {
            for (int attempt = 1; attempt <= MaxFrameAttempts; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync(SignalMethods.ReceiveWorkStatus, $"Подключение к камере {camera.Id} попытка - {attempt}/{MaxFrameAttempts}", ct);

                    return await SaveFrameFromCamera(connectionId, camera, pathToScreenFolderCamera, ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Получение кадров ошибка ({camera.Id}, попытка {attempt}) - {ex}");

                    if (attempt < MaxFrameAttempts)
                        await Task.Delay(FrameRetryDelay * attempt, ct);
                }
            }

            return null;
        }

        private async Task<string> SaveFrameFromCamera(
            string connectionId,
            CameraData camera,
            string pathToScreenFolderCamera,
            CancellationToken ct)
        {
            var cameraVideoUrl = $"{camera.Url}/{playlist}";

            var options = new MediaOptions() { VideoPixelFormat = FFMediaToolkit.Graphics.ImagePixelFormat.Rgb24, };
            using var file = MediaFile.Open(cameraVideoUrl, options);
            var video = file.Video;

            var buffer = new byte[video.FrameByteCount];
            if (!video.TryGetNextFrame(buffer))
                throw new InvalidOperationException($"Не удалось прочитать первый кадр с камеры {camera.Id}");

            #region Skip 1 sec
            // Первые кадры HLS часто битые/серые, поэтому пропускаем SkipDuration по timestamp кадров
            await _hubContext.Clients.Client(connectionId).SendAsync(SignalMethods.ReceiveWorkStatus, $"Ждём: {SkipDuration.TotalSeconds} сек.", ct);
            var startPosition = video.Position;
            int skippedFrames = 0;
            while (video.Position - startPosition < SkipDuration && skippedFrames < MaxSkippedFrames)
            {
                ct.ThrowIfCancellationRequested();
                if (!video.TryGetNextFrame(buffer)) break;
                skippedFrames++;
            }
            #endregion Skip 1 sec

            await _hubContext.Clients.Client(connectionId).SendAsync(SignalMethods.ReceiveWorkStatus, $"Берём фрейм", ct);

            var filename = $"{DateTime.Now:yyyyMMddHHmmss}.png";
            var path = System.IO.Path.Combine(pathToScreenFolderCamera, filename);

            using var image = Image.LoadPixelData<Rgb24>(buffer, video.Info.FrameSize.Width, video.Info.FrameSize.Height);
            image.Mutate(x => x.Resize(1080, 864, KnownResamplers.Lanczos3));
            await image.SaveAsPngAsync(path, ct);

            await _hubContext.Clients.Client(connectionId).SendAsync(SignalMethods.ReceiveWorkStatus, $"Фрейм сохранён как: {filename}", ct);

            return filename;
        }

        private async Task CheckParking(
            string connectionId,
            string pathToScreenFolderCamera, string filename,
            CameraData cameraData,
            CancellationToken ct)
        {
            var path = System.IO.Path.Combine(pathToScreenFolderCamera, filename);
            var camera = _parkingSpacesService.Parser[cameraData.Id];

            if (camera == null)
            {
                Console.WriteLine($"Не заданы координаты парковки для {cameraData.Id}");
            }
            else
            {
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

                await _hubContext.Clients.Client(connectionId).SendAsync(SignalMethods.ReceiveParkingData, result, ct);
            }
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
