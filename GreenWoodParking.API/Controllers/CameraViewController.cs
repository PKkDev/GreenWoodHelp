using Microsoft.AspNetCore.Mvc;

namespace GreenWoodParking.API.Controllers
{
    [Route("file-view")]
    [ApiController]
    public class CameraViewController : ControllerBase
    {
        [HttpGet("camera/{connectionId}/{cameraId}/{fileName}")]
        public IActionResult GetCameraView(string connectionId, string cameraId, string fileName)
        {
            Console.WriteLine($"Получение файла для connectionId:{connectionId}, cameraId: {cameraId},fileName: {fileName}");

            var pathToScreenFolder = System.IO.Path.Combine(AppContext.BaseDirectory, "cameraview");
            var pathToScreenFolderCamera = System.IO.Path.Combine(pathToScreenFolder, connectionId, cameraId, fileName);

            if (!System.IO.File.Exists(pathToScreenFolderCamera))
            {
                return NotFound(new { message = "Файл не найден на сервере" });
            }

            byte[] fileBytes = System.IO.File.ReadAllBytes(pathToScreenFolderCamera);
            string contentType = "application/octet-stream";

            return File(fileBytes, contentType, fileName);
        }
    }
}
