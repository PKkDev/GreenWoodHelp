namespace GreenWoodParking.API.DTO
{
    /// <summary>
    /// Статистика по сохранённым снимкам камер
    /// </summary>
    /// <param name="FileCount">Количество файлов</param>
    /// <param name="TotalBytes">Занятое дисковое пространство в байтах</param>
    public record CameraAssetsStatsDto(int FileCount, long TotalBytes);
}
