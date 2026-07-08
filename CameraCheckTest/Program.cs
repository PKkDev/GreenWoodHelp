using CameraCheckTest.Model;
using FFMediaToolkit;
using FFMediaToolkit.Decoding;
using Newtonsoft.Json;
using ObjectDetection.WinApp.Services;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using YOLO26.Shared.CvatWorker;

namespace CameraCheckTest
{
    internal class Program
    {
        public static string pathToScreenFolder = "D:\\Projects\\GreenWood\\GreenWoodHelp\\FileAssets\\cameraview";
        public static string pathToParkingFolder = "D:\\Projects\\GreenWood\\GreenWoodHelp\\FileAssets\\parkingview";
        public static string[] needIds = ["p29", "p28", "p31", "p30", "p21", "p22", "p13", "p14", "p16", "p15", "p39", "p40", "p38", "p37", "p24", "p25"];
        // public static string[] needIds = ["p24"];
        public static string url = "https://gw.videosreda.ru";
        public static string playlist = "playlist.m3u8";

        public static object consoleLock = new object();

        static void Main(string[] args)
        {
            //StartSaveCameraView();

            CleareOutputFolder();
            StartSaveCameraViewV2();
            StartSaveParking();

            WriteSuccess("Работа завершена");
            Console.ReadKey();
        }

        public static void StartSaveCameraView()
        {
            // Настройка опций ChromeDriver
            var options = new ChromeOptions();
            options.AddArgument("--start-maximized"); // Разворачиваем окно на весь экран

            // Инициализация драйвера
            using var driver = new ChromeDriver(options);

            foreach (var needId in needIds)
            {
                try
                {
                    // Открываем URL
                    driver.Navigate().GoToUrl(url);
                    Console.WriteLine("Страница загружена");

                    // Ждём загрузки первого элемента (максимум 10 секунд)
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                    var point = wait.Until(d => d.FindElement(By.Id(needId)));
                    Console.WriteLine($"Найден point {needId}");

                    var id = point.GetAttribute("id");

                    Console.WriteLine($"Клик на {id} выполнен");
                    point.Click();

                    Console.WriteLine("Ожидание 2 секунды перед кликом на кнопку полноэкранного режима");
                    Thread.Sleep(TimeSpan.FromSeconds(2));

                    // Ищем кнопку полноэкранного режима
                    var fullscreenButton = wait.Until(d =>
                        d.FindElement(By.CssSelector("button.vjs-fullscreen-control.vjs-control.vjs-button[title='Fullscreen'][aria-disabled='false']")));
                    Console.WriteLine("Кнопка полноэкранного режима найдена");

                    // Кликаем на кнопку полноэкранного режима
                    Console.WriteLine("Клик на кнопку полноэкранного режима выполнен");
                    fullscreenButton.Click();

                    // Ждём 1 секунды, чтобы полноэкранный режим успел активироваться
                    Thread.Sleep(TimeSpan.FromSeconds(1));

                    // Делаем скриншот
                    var screenshot = driver.GetScreenshot();
                    var filename = $"{DateTime.Now:yyyyMMddHHmmss}.png";
                    var pathToScreenFolderCamera = System.IO.Path.Combine(pathToScreenFolder, id);
                    if (!Directory.Exists(pathToScreenFolderCamera))
                        Directory.CreateDirectory(pathToScreenFolderCamera);
                    var path = System.IO.Path.Combine(pathToScreenFolderCamera, filename);
                    File.WriteAllBytes(path, screenshot.AsByteArray);
                    Console.WriteLine($"Скриншот сохранён как: {filename}");
                }
                catch (NoSuchElementException ex)
                {
                    Console.WriteLine($"Ошибка: элемент не найден — {ex.Message}");
                }
                catch (WebDriverTimeoutException)
                {
                    Console.WriteLine("Ошибка: время ожидания загрузки элемента истекло.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public static void StartSaveCameraViewV2()
        {
            // https://github.com/radek-k/FFMediaToolkit#setup
            // https://github.com/GyanD/codexffmpeg/releases?q=7.&expanded=true
            // ffmpeg-7.1.1-full_build-shared
            FFmpegLoader.FFmpegPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Files", "ffmpeg");

            Console.WriteLine($"Получение cameras.json");
            using HttpClient client = new(new RetryHandler(new HttpClientHandler()));
            var camerasResponse = client.GetAsync($"{url}/cameras.json").GetAwaiter().GetResult();
            var camerasResponseContent = camerasResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var cameras = JsonConvert.DeserializeObject<GreenWoodCameras>(camerasResponseContent);

            WriteSuccess("Получен cameras.json");

            var camerasNeeded = cameras.Cameras.Where(x => needIds.Contains(x.Id));
            List<Task> tasks = new();
            foreach (var camera in camerasNeeded)
            {
                // 1. Фиксируем переменную для замыкания
                var currentCamera = camera;
                tasks.Add(Task.Run(() =>
                {
                    Console.WriteLine($"[{currentCamera.Id}] Получение кадров");

                    var pathToScreenFolderCamera = System.IO.Path.Combine(pathToScreenFolder, currentCamera.Id);
                    if (!Directory.Exists(pathToScreenFolderCamera))
                        Directory.CreateDirectory(pathToScreenFolderCamera);

                    GetFrameFromCamera(currentCamera, pathToScreenFolderCamera, 0, 4);

                }));
            }

            Task.WhenAll(tasks).GetAwaiter().GetResult();
        }

        public static void GetFrameFromCamera(CameraData camera, string pathToScreenFolderCamera, int index, int max)
        {
            try
            {
                if (index >= max)
                    return;

                Console.WriteLine($"[{camera.Id}] Подключение к камере попытка - {index}");

                var cameraVideoUrl = $"{camera.Url}/{playlist}";

                var options = new MediaOptions() { VideoPixelFormat = FFMediaToolkit.Graphics.ImagePixelFormat.Rgb24, };
                using var file = MediaFile.Open(cameraVideoUrl);
                var video = file.Video;
                //Console.WriteLine($"Исходное разрешение: {video.Info.FrameSize}");

                var buffer = new byte[file.Video.FrameByteCount];
                var bmp = Image.WrapMemory<Bgr24>(buffer, file.Video.Info.FrameSize.Width, file.Video.Info.FrameSize.Height);

                var startTime = DateTime.Now;
                double skipSeconds = 1.0;
                Console.WriteLine($"[{camera.Id}] Ждём {skipSeconds} сек.");
                while ((DateTime.Now - startTime).TotalSeconds < skipSeconds)
                {
                    if (!file.Video.TryGetNextFrame(buffer)) break;
                }
                Console.WriteLine($"[{camera.Id}] Берём кадр");

                file.Video.TryGetNextFrame(buffer);
                var filename = $"{DateTime.Now:yyyyMMddHHmmss}.png";

                var path = System.IO.Path.Combine(pathToScreenFolderCamera, filename);
                using var resultBmp = bmp.Clone();
                resultBmp.Mutate(x => x.Resize(1080, 864, KnownResamplers.Lanczos3));
                resultBmp.SaveAsJpeg(path);
                bmp.Dispose();
                resultBmp.Dispose();
                WriteSuccess($"[{camera.Id}] Фрейм сохранён как: {filename}");
            }
            catch (Exception ex)
            {
                WriteError($"[{camera.Id}] Получение кадров ошибка - {ex.Message}");
                GetFrameFromCamera(camera, pathToScreenFolderCamera, ++index, max);
            }
        }

        public static void CleareOutputFolder()
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(pathToParkingFolder);
            foreach (FileInfo file in di.GetFiles())
                file.Delete();
            foreach (DirectoryInfo dir in di.GetDirectories())
                dir.Delete(true);
        }

        public static void StartSaveParking()
        {
            Console.WriteLine("Зпуска этапа AI");
            var yolo26Service = new Yolo26Service();

            CvatParser parser = new();
            parser.Load("p31", System.IO.Path.Combine(Environment.CurrentDirectory, "Files", "p31.xml"));
            parser.Load("p30", System.IO.Path.Combine(Environment.CurrentDirectory, "Files", "p30.xml"));
            parser.Load("p29", System.IO.Path.Combine(Environment.CurrentDirectory, "Files", "p29.xml"));
            parser.Load("p28", System.IO.Path.Combine(Environment.CurrentDirectory, "Files", "p28.xml"));
            parser.Load("p22", System.IO.Path.Combine(Environment.CurrentDirectory, "Files", "p22.xml"));
            parser.Load("p21", System.IO.Path.Combine(Environment.CurrentDirectory, "Files", "p21.xml"));
            parser.Load("p15", System.IO.Path.Combine(Environment.CurrentDirectory, "Files", "p15.xml"));
            parser.Load("p16", System.IO.Path.Combine(Environment.CurrentDirectory, "Files", "p16.xml"));
            parser.Load("p13", System.IO.Path.Combine(Environment.CurrentDirectory, "Files", "p13.xml"));
            parser.Load("p14", System.IO.Path.Combine(Environment.CurrentDirectory, "Files", "p14.xml"));
            parser.Load("p39", System.IO.Path.Combine(Environment.CurrentDirectory, "Files", "p39.xml"));
            parser.Load("p40", System.IO.Path.Combine(Environment.CurrentDirectory, "Files", "p40.xml"));
            parser.Load("p37", System.IO.Path.Combine(Environment.CurrentDirectory, "Files", "p37.xml"));
            parser.Load("p38", System.IO.Path.Combine(Environment.CurrentDirectory, "Files", "p38.xml"));
            parser.Load("p24", System.IO.Path.Combine(Environment.CurrentDirectory, "Files", "p24.xml"));
            //parser.Load("p25", System.IO.Path.Combine(Environment.CurrentDirectory, "Files", "p25.xml"));

            System.IO.DirectoryInfo diImages = new DirectoryInfo(pathToScreenFolder);
            var folders = diImages.GetDirectories();

            foreach (var item in parser.ParkingData.Where(x => needIds.Contains(x.Id)))
            {
                Console.WriteLine($"[{item.Id}] Обработка");

                var folder = folders.First(x => x.Name == item.Id);
                if (folder != null)
                {
                    var myFile = folder.GetFiles()
                        .OrderByDescending(f => f.LastWriteTime)
                        .First();

                    var predicts = yolo26Service.Predict(myFile.FullName);

                    var imagePathProcessedParking = $"{pathToParkingFolder}\\{item.Id}_{myFile.Name}-parking.png";
                    yolo26Service.DrawDetectionsAndParking(myFile.FullName, imagePathProcessedParking, item.ParkingSpaces, predicts);
                }
            }
        }

        public static void WriteSuccess(string text)
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(text);
                Console.ResetColor();
            }
        }

        public static void WriteError(string text)
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(text);
                Console.ResetColor();
            }
        }
    }
}
