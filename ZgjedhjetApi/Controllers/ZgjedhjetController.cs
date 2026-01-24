using Microsoft.AspNetCore.Mvc;
using ZgjedhjetApi.Enums;
using ZgjedhjetApi.Models.DTOs;
using ZgjedhjetApi.Services;

namespace ZgjedhjetApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ZgjedhjetController : ControllerBase
    {
        private readonly ILogger<ZgjedhjetController> _logger;
        private readonly IZgjedhjetService _service;

        public ZgjedhjetController(ILogger<ZgjedhjetController> logger, IZgjedhjetService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<CsvImportResponse>> MigrateData(IFormFile file)
        {
            var res = await _service.ImportCsvAsync(file);

            if (!res.Success)
            {
                if (res.Message.StartsWith("InternalServerError"))
                    return StatusCode(StatusCodes.Status500InternalServerError, res);

                return BadRequest(res);
            }

            return Ok(res);
        }

        [HttpGet]
        public async Task<ActionResult<ZgjedhjetAggregatedResponse>> GetZgjedhjet(
        [FromQuery] string? kategoria = null,
        [FromQuery] string? komuna = null,
        [FromQuery] string? qendra_e_votimit = null,
        [FromQuery] string? vendvotimi = null,
        [FromQuery] string? partia = null)
        {
            try
            {
                Kategoria? k = null;
                if (!string.IsNullOrWhiteSpace(kategoria) && !string.Equals(kategoria, "TeGjitha", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Enum.TryParse<Kategoria>(NormalizeEnumToken(kategoria), true, out var parsedK))
                        return BadRequest("Invalid kategoria");
                    k = parsedK;
                }

                Komuna? km = null;
                if (!string.IsNullOrWhiteSpace(komuna) && !string.Equals(komuna, "TeGjitha", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Enum.TryParse<Komuna>(NormalizeEnumToken(komuna), true, out var parsedKm))
                        return BadRequest("Invalid komuna");
                    km = parsedKm;
                }

                Partia? p = null;
                if (!string.IsNullOrWhiteSpace(partia) && !string.Equals(partia, "TeGjitha", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Enum.TryParse<Partia>(NormalizeEnumToken(partia), true, out var parsedP))
                        return BadRequest("Invalid partia");
                    p = parsedP;
                }

                var response = await _service.GetZgjedhjetAsync(
                    k, km, qendra_e_votimit, vendvotimi, p);

                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        private static string NormalizeEnumToken(string s)
        {
            return (s ?? string.Empty).Trim().Replace(" ", "_");
        }
    }
}
