using GreenWoodParking.API.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace GreenWoodParking.API.Controllers
{
    [Route("file-view")]
    [ApiController]
    public class CameraViewController : ControllerBase
    {
        private static readonly string PathToScreenFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "cameraview"));

        private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

        [HttpGet("camera/{connectionId}/{cameraId}/{fileName}")]
        public IActionResult GetCameraView(string connectionId, string cameraId, string fileName)
        {
            Console.WriteLine($"Получение файла для connectionId:{connectionId}, cameraId: {cameraId},fileName: {fileName}");

            if (!IsSafeSegment(connectionId) || !IsSafeSegment(cameraId) || !IsSafeSegment(fileName))
            {
                return BadRequest(new { message = "Недопустимый путь к файлу" });
            }

            var pathToScreenFolderCamera = Path.GetFullPath(Path.Combine(PathToScreenFolder, connectionId, cameraId, fileName));

            // Дополнительная защита: итоговый путь обязан остаться внутри папки cameraview
            if (!pathToScreenFolderCamera.StartsWith(PathToScreenFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Недопустимый путь к файлу" });
            }

            if (!System.IO.File.Exists(pathToScreenFolderCamera))
            {
                return NotFound(new { message = "Файл не найден на сервере" });
            }

            if (!ContentTypeProvider.TryGetContentType(fileName, out var contentType))
                contentType = "application/octet-stream";

            return PhysicalFile(pathToScreenFolderCamera, contentType, fileName);
        }

        [HttpGet("camera-assets/stats")]
        public ActionResult<CameraAssetsStatsDto> GetCameraAssetsStats()
        {
            var di = new DirectoryInfo(PathToScreenFolder);
            if (!di.Exists)
                return Ok(new CameraAssetsStatsDto(0, 0));

            var files = di.EnumerateFiles("*", SearchOption.AllDirectories).ToList();
            return Ok(new CameraAssetsStatsDto(files.Count, files.Sum(f => f.Length)));
        }

        [HttpDelete("camera-assets")]
        public IActionResult RemoveOldCameraAssets()
        {
            Console.WriteLine($"Удаление старых снимков камеры");

            DirectoryInfo di = new(PathToScreenFolder);
            if (!di.Exists)
                return Ok();

            foreach (FileInfo file in di.GetFiles())
                file.Delete();
            foreach (DirectoryInfo dir in di.GetDirectories())
                dir.Delete(true);

            return Ok();
        }

        /// <summary>
        /// Сегмент пути должен быть простым именем: без разделителей, "..", и недопустимых символов
        /// </summary>
        private static bool IsSafeSegment(string segment)
        {
            return !string.IsNullOrWhiteSpace(segment)
                && segment != "."
                && segment != ".."
                && segment.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                && segment.IndexOfAny(['/', '\\']) < 0
                && Path.GetFileName(segment) == segment;
        }
    }
}
