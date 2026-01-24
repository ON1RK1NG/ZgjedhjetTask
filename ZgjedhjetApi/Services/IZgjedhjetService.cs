using ZgjedhjetApi.Enums;
using ZgjedhjetApi.Models.DTOs;

namespace ZgjedhjetApi.Services
{
    public interface IZgjedhjetService
    {
        Task<CsvImportResponse> ImportCsvAsync(IFormFile file);

        Task<ZgjedhjetAggregatedResponse> GetZgjedhjetAsync(
            Kategoria? kategoria,
            Komuna? komuna,
            string? qendra_e_votimit,
            string? vendvotimi,
            Partia? partia);
    }
}
