using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using ZgjedhjetApi.Data;
using ZgjedhjetApi.Enums;
using ZgjedhjetApi.Models.DTOs;
using ZgjedhjetApi.Models.Entities;

namespace ZgjedhjetApi.Services
{
    public class ZgjedhjetService : IZgjedhjetService
    {
        private readonly LifeDbContext _db;
        private readonly ILogger<ZgjedhjetService> _logger;

        public ZgjedhjetService(LifeDbContext db, ILogger<ZgjedhjetService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<CsvImportResponse> ImportCsvAsync(IFormFile file)
        {
            var response = new CsvImportResponse();

            if (file == null || file.Length == 0)
            {
                response.Success = false;
                response.Message = "File is missing or empty";
                return response;
            }

            var expectedHeader = new[]
            {
                "Kategoria","Komuna","Qendra_e_votimit","Vendvotimi",
                "Partia111","Partia112","Partia113","Partia114","Partia115","Partia116","Partia117","Partia118",
                "Partia119","Partia120","Partia121","Partia122","Partia123","Partia124","Partia125","Partia126",
                "Partia127","Partia128","Partia129","Partia130","Partia131","Partia132","Partia133","Partia134",
                "Partia135","Partia136","Partia137","Partia138"
            };

            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream, Encoding.UTF8, true);

                var headerLine = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    response.Success = false;
                    response.Message = "CSV header is missing";
                    return response;
                }

                var header = ParseCsvLine(headerLine.Trim('\uFEFF'));
                if (header.Count != expectedHeader.Length || !header.SequenceEqual(expectedHeader))
                {
                    response.Success = false;
                    response.Message = "CSV header does not match expected columns";
                    return response;
                }

                var rows = new List<Zgjedhjet>();
                var lineNumber = 1;

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    lineNumber++;

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = ParseCsvLine(line);
                    if (parts.Count != expectedHeader.Length)
                    {
                        response.Errors.Add($"Line {lineNumber}: Invalid column count");
                        continue;
                    }

                    var kategoriaStr = NormalizeEnumToken(parts[0]);
                    var komunaStr = NormalizeEnumToken(parts[1]);
                    var qendra = parts[2].Trim();
                    var vendvotimi = parts[3].Trim();

                    if (!Enum.TryParse<Kategoria>(kategoriaStr, true, out _))
                    {
                        response.Errors.Add($"Line {lineNumber}: Invalid Kategoria '{parts[0]}'");
                        continue;
                    }

                    if (!Enum.TryParse<Komuna>(komunaStr, true, out _))
                    {
                        response.Errors.Add($"Line {lineNumber}: Invalid Komuna '{parts[1]}'");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(qendra) || string.IsNullOrWhiteSpace(vendvotimi))
                    {
                        response.Errors.Add($"Line {lineNumber}: Qendra_e_votimit/Vendvotimi is empty");
                        continue;
                    }

                    int p111, p112, p113, p114, p115, p116, p117, p118,
                        p119, p120, p121, p122, p123, p124, p125, p126,
                        p127, p128, p129, p130, p131, p132, p133, p134,
                        p135, p136, p137, p138;

                    if (!TryParseInt(parts[4], out p111) ||
                        !TryParseInt(parts[5], out p112) ||
                        !TryParseInt(parts[6], out p113) ||
                        !TryParseInt(parts[7], out p114) ||
                        !TryParseInt(parts[8], out p115) ||
                        !TryParseInt(parts[9], out p116) ||
                        !TryParseInt(parts[10], out p117) ||
                        !TryParseInt(parts[11], out p118) ||
                        !TryParseInt(parts[12], out p119) ||
                        !TryParseInt(parts[13], out p120) ||
                        !TryParseInt(parts[14], out p121) ||
                        !TryParseInt(parts[15], out p122) ||
                        !TryParseInt(parts[16], out p123) ||
                        !TryParseInt(parts[17], out p124) ||
                        !TryParseInt(parts[18], out p125) ||
                        !TryParseInt(parts[19], out p126) ||
                        !TryParseInt(parts[20], out p127) ||
                        !TryParseInt(parts[21], out p128) ||
                        !TryParseInt(parts[22], out p129) ||
                        !TryParseInt(parts[23], out p130) ||
                        !TryParseInt(parts[24], out p131) ||
                        !TryParseInt(parts[25], out p132) ||
                        !TryParseInt(parts[26], out p133) ||
                        !TryParseInt(parts[27], out p134) ||
                        !TryParseInt(parts[28], out p135) ||
                        !TryParseInt(parts[29], out p136) ||
                        !TryParseInt(parts[30], out p137) ||
                        !TryParseInt(parts[31], out p138))
                    {
                        response.Errors.Add($"Line {lineNumber}: Invalid numeric value(s)");
                        continue;
                    }

                    rows.Add(new Zgjedhjet
                    {
                        Kategoria = kategoriaStr,
                        Komuna = komunaStr,
                        Qendra_e_votimit = qendra,
                        Vendvotimi = vendvotimi,
                        Partia111 = p111,
                        Partia112 = p112,
                        Partia113 = p113,
                        Partia114 = p114,
                        Partia115 = p115,
                        Partia116 = p116,
                        Partia117 = p117,
                        Partia118 = p118,
                        Partia119 = p119,
                        Partia120 = p120,
                        Partia121 = p121,
                        Partia122 = p122,
                        Partia123 = p123,
                        Partia124 = p124,
                        Partia125 = p125,
                        Partia126 = p126,
                        Partia127 = p127,
                        Partia128 = p128,
                        Partia129 = p129,
                        Partia130 = p130,
                        Partia131 = p131,
                        Partia132 = p132,
                        Partia133 = p133,
                        Partia134 = p134,
                        Partia135 = p135,
                        Partia136 = p136,
                        Partia137 = p137,
                        Partia138 = p138
                    });
                }

                _db.Zgjedhjet.RemoveRange(_db.Zgjedhjet);
                await _db.SaveChangesAsync();

                await _db.Zgjedhjet.AddRangeAsync(rows);
                await _db.SaveChangesAsync();

                response.RecordsImported = rows.Count;
                response.Success = response.Errors.Count == 0;
                response.Message = response.Success
                    ? $"Successfully imported {rows.Count} records"
                    : $"Imported {rows.Count} records with {response.Errors.Count} error(s)";

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CSV import failed");
                response.Success = false;
                response.Message = "InternalServerError while importing CSV";
                response.Errors.Add(ex.Message);
                return response;
            }
        }

        public async Task<ZgjedhjetAggregatedResponse> GetZgjedhjetAsync(
            Kategoria? kategoria,
            Komuna? komuna,
            string? qendra,
            string? vendvotimi,
            Partia? partia)
        {
            if (!string.IsNullOrWhiteSpace(vendvotimi))
            {
                var exists = await _db.Zgjedhjet.AsNoTracking()
                    .AnyAsync(x => x.Vendvotimi == vendvotimi);

                if (!exists)
                    throw new KeyNotFoundException($"Vendvotimi '{vendvotimi}' not found");
            }

            var query = _db.Zgjedhjet.AsNoTracking().AsQueryable();

            if (kategoria.HasValue && kategoria.Value != Kategoria.TeGjitha)
                query = query.Where(x => x.Kategoria == kategoria.Value.ToString());

            if (komuna.HasValue && komuna.Value != Komuna.TeGjitha)
                query = query.Where(x => x.Komuna == komuna.Value.ToString());

            if (!string.IsNullOrWhiteSpace(qendra))
                query = query.Where(x => x.Qendra_e_votimit == qendra);

            if (!string.IsNullOrWhiteSpace(vendvotimi))
                query = query.Where(x => x.Vendvotimi == vendvotimi);

            var data = await query.ToListAsync();
            var response = new ZgjedhjetAggregatedResponse();

            if (partia.HasValue && partia.Value != Partia.TeGjitha)
            {
                response.Results.Add(new PartiaVotesResponse
                {
                    Partia = partia.Value.ToString(),
                    TotalVota = data.Sum(x => GetVotes(x, partia.Value))
                });
                return response;
            }

            foreach (var p in Enum.GetValues<Partia>().Where(x => x != Partia.TeGjitha))
            {
                response.Results.Add(new PartiaVotesResponse
                {
                    Partia = p.ToString(),
                    TotalVota = data.Sum(x => GetVotes(x, p))
                });
            }

            response.Results = response.Results.OrderByDescending(x => x.TotalVota).ToList();
            return response;
        }

        private static string NormalizeEnumToken(string s)
        {
            return (s ?? string.Empty).Trim().Replace(" ", "_");
        }

        private static bool TryParseInt(string s, out int value)
        {
            return int.TryParse(s?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static int GetVotes(Zgjedhjet z, Partia partia)
        {
            return partia switch
            {
                Partia.Partia111 => z.Partia111,
                Partia.Partia112 => z.Partia112,
                Partia.Partia113 => z.Partia113,
                Partia.Partia114 => z.Partia114,
                Partia.Partia115 => z.Partia115,
                Partia.Partia116 => z.Partia116,
                Partia.Partia117 => z.Partia117,
                Partia.Partia118 => z.Partia118,
                Partia.Partia119 => z.Partia119,
                Partia.Partia120 => z.Partia120,
                Partia.Partia121 => z.Partia121,
                Partia.Partia122 => z.Partia122,
                Partia.Partia123 => z.Partia123,
                Partia.Partia124 => z.Partia124,
                Partia.Partia125 => z.Partia125,
                Partia.Partia126 => z.Partia126,
                Partia.Partia127 => z.Partia127,
                Partia.Partia128 => z.Partia128,
                Partia.Partia129 => z.Partia129,
                Partia.Partia130 => z.Partia130,
                Partia.Partia131 => z.Partia131,
                Partia.Partia132 => z.Partia132,
                Partia.Partia133 => z.Partia133,
                Partia.Partia134 => z.Partia134,
                Partia.Partia135 => z.Partia135,
                Partia.Partia136 => z.Partia136,
                Partia.Partia137 => z.Partia137,
                Partia.Partia138 => z.Partia138,
                _ => 0
            };
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (line == null) return result;

            var sb = new StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                    continue;
                }

                sb.Append(c);
            }

            result.Add(sb.ToString());
            return result;
        }


        internal static ZgjedhjetAggregatedResponse BuildAggregatedResponse(
            IEnumerable<Zgjedhjet> rows,
            Partia? partiaFilter)
        {
            var data = rows?.ToList() ?? new List<Zgjedhjet>();
            var response = new ZgjedhjetAggregatedResponse();

            if (partiaFilter.HasValue && partiaFilter.Value != Partia.TeGjitha)
            {
                response.Results.Add(new PartiaVotesResponse
                {
                    Partia = partiaFilter.Value.ToString(),
                    TotalVota = data.Sum(x => GetVotes(x, partiaFilter.Value))
                });

                return response;
            }

            foreach (var p in Enum.GetValues<Partia>().Where(x => x != Partia.TeGjitha))
            {
                response.Results.Add(new PartiaVotesResponse
                {
                    Partia = p.ToString(),
                    TotalVota = data.Sum(x => GetVotes(x, p))
                });
            }

            response.Results = response.Results.OrderByDescending(x => x.TotalVota).ToList();
            return response;
        }
    }
}
