using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using ZgjedhjetApi.Configuration;
using ZgjedhjetApi.Data;
using ZgjedhjetApi.Elastics;
using ZgjedhjetApi.Elastics.Models;
using ZgjedhjetApi.Enums;
using ZgjedhjetApi.Models.DTOs;
using ZgjedhjetApi.Models.Entities;
using ZgjedhjetApi.Services;

namespace ZgjedhjetApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ZgjedhjetElasticSearchController : ControllerBase
{
    private readonly LifeDbContext _db;
    private readonly ElasticsearchClient _elastic;
    private readonly ElasticsearchOptions _esOptions;
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisOptions _redisOptions;

    public ZgjedhjetElasticSearchController(
        LifeDbContext db,
        ElasticsearchClient elastic,
        IConnectionMultiplexer redis,
        IOptions<ElasticsearchOptions> esOptions,
        IOptions<RedisOptions> redisOptions)
    {
        _db = db;
        _elastic = elastic;
        _redis = redis;
        _esOptions = esOptions.Value;
        _redisOptions = redisOptions.Value;
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportToElasticsearch(CancellationToken ct)
    {
        await ElasticsearchIndexSetup.EnsureIndexAsync(_elastic, _esOptions.IndexName, ct);

        var rows = await _db.Zgjedhjet
            .AsNoTracking()
            .ToListAsync(ct);

        if (rows.Count == 0)
            return Ok(new { imported = 0, message = "No rows found in SQL." });

        static string MakeId(string kategoria, string komuna, string qendra, string vendvotimi)
            => $"{kategoria}|{komuna}|{qendra}|{vendvotimi}";

        var docs = rows.Select(r => new ZgjedhjetDocument
        {
            Id = MakeId(r.Kategoria, r.Komuna, r.Qendra_e_votimit, r.Vendvotimi),
            Kategoria = r.Kategoria,
            Komuna = r.Komuna,
            KomunaKeyword = r.Komuna,
            Qendra_e_votimit = r.Qendra_e_votimit,
            Vendvotimi = r.Vendvotimi,
            Partia111 = r.Partia111,
            Partia112 = r.Partia112,
            Partia113 = r.Partia113,
            Partia114 = r.Partia114,
            Partia115 = r.Partia115,
            Partia116 = r.Partia116,
            Partia117 = r.Partia117,
            Partia118 = r.Partia118,
            Partia119 = r.Partia119,
            Partia120 = r.Partia120,
            Partia121 = r.Partia121,
            Partia122 = r.Partia122,
            Partia123 = r.Partia123,
            Partia124 = r.Partia124,
            Partia125 = r.Partia125,
            Partia126 = r.Partia126,
            Partia127 = r.Partia127,
            Partia128 = r.Partia128,
            Partia129 = r.Partia129,
            Partia130 = r.Partia130,
            Partia131 = r.Partia131,
            Partia132 = r.Partia132,
            Partia133 = r.Partia133,
            Partia134 = r.Partia134,
            Partia135 = r.Partia135,
            Partia136 = r.Partia136,
            Partia137 = r.Partia137,
            Partia138 = r.Partia138
        }).ToList();

        var bulkRequest = new BulkRequest(_esOptions.IndexName)
        {
            Refresh = Elastic.Clients.Elasticsearch.Refresh.True,
            Operations = new List<IBulkOperation>(docs.Count)
        };

        foreach (var doc in docs)
        {
            bulkRequest.Operations.Add(new BulkIndexOperation<ZgjedhjetDocument>(doc)
            {
                Id = doc.Id
            });
        }

        var bulk = await _elastic.BulkAsync(bulkRequest, ct);

        if (!bulk.IsValidResponse)
        {
            return StatusCode(500, new
            {
                message = "Bulk request failed.",
                error = bulk.ElasticsearchServerError?.Error?.Reason
            });
        }

        if (bulk.Errors == true)
        {
            var firstError = bulk.ItemsWithErrors?.FirstOrDefault();
            return StatusCode(500, new
            {
                message = "Bulk indexing had errors.",
                error = firstError?.Error?.Reason
            });
        }

        return Ok(new { imported = docs.Count });
    }

    [HttpGet("suggest/komuna")]
    public async Task<IActionResult> SuggestKomuna(
        [FromQuery] string query,
        [FromQuery] int? top,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Ok(Array.Empty<string>());

        var take = top.GetValueOrDefault(10);
        if (take <= 0) take = 10;
        if (take > 50) take = 50;

        var search = await _elastic.SearchAsync<ZgjedhjetDocument>(s => s
            .Index(_esOptions.IndexName)
            .Size(take)
            .Query(q => q.MatchPhrasePrefix(m => m
                .Field("komuna")
                .Query(query)
            ))
            .Collapse(c => c.Field("komunaKeyword.keyword"))
        , ct);

        if (!search.IsValidResponse)
        {
            return StatusCode(500, new
            {
                message = "Elasticsearch suggest search failed.",
                error = search.ElasticsearchServerError?.Error?.Reason
            });
        }

        var results = search.Hits
            .Select(h => h.Source?.KomunaKeyword)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .Take(take)
            .ToList()!;

        if (results.Count > 0)
        {
            var rdb = _redis.GetDatabase();
            var tasks = results.Select(k => rdb.SortedSetIncrementAsync(_redisOptions.SuggestionsKey, k, 1));
            await Task.WhenAll(tasks);
        }

        return Ok(results);
    }

    [HttpGet("stats/suggested-komuna")]
    public async Task<IActionResult> GetSuggestedKomunaStats([FromQuery] int? top)
    {
        var take = top.GetValueOrDefault(10);
        if (take <= 0) take = 10;
        if (take > 100) take = 100;

        var rdb = _redis.GetDatabase();

        var entries = await rdb.SortedSetRangeByRankWithScoresAsync(
            _redisOptions.SuggestionsKey,
            0,
            take - 1,
            Order.Descending);

        var response = entries.Select(e => new
        {
            komuna = (string)e.Element!,
            nrISugjerimeve = (long)e.Score
        });

        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<ZgjedhjetAggregatedResponse>> GetZgjedhjet(
        [FromQuery] string? kategoria = null,
        [FromQuery] string? komuna = null,
        [FromQuery] string? qendra_e_votimit = null,
        [FromQuery] string? vendvotimi = null,
        [FromQuery] string? partia = null,
        CancellationToken ct = default)
    {
        try
        {
            Kategoria? k = null;
            if (!string.IsNullOrWhiteSpace(kategoria) &&
                !string.Equals(kategoria, "TeGjitha", StringComparison.OrdinalIgnoreCase))
            {
                if (!Enum.TryParse<Kategoria>(NormalizeEnumToken(kategoria), true, out var parsedK))
                    return BadRequest("Invalid kategoria");
                k = parsedK;
            }

            Komuna? km = null;
            if (!string.IsNullOrWhiteSpace(komuna) &&
                !string.Equals(komuna, "TeGjitha", StringComparison.OrdinalIgnoreCase))
            {
                if (!Enum.TryParse<Komuna>(NormalizeEnumToken(komuna), true, out var parsedKm))
                    return BadRequest("Invalid komuna");
                km = parsedKm;
            }

            Partia? p = null;
            if (!string.IsNullOrWhiteSpace(partia) &&
                !string.Equals(partia, "TeGjitha", StringComparison.OrdinalIgnoreCase))
            {
                if (!Enum.TryParse<Partia>(NormalizeEnumToken(partia), true, out var parsedP))
                    return BadRequest("Invalid partia");
                p = parsedP;
            }

            if (!string.IsNullOrWhiteSpace(vendvotimi))
            {
                var existsCheck = await _elastic.SearchAsync<ZgjedhjetDocument>(s => s
                    .Index(_esOptions.IndexName)
                    .Size(0)
                    .Query(q => q.Term(t => t.Field("vendvotimi").Value(vendvotimi)))
                , ct);

                if (!existsCheck.IsValidResponse)
                {
                    return StatusCode(500, new
                    {
                        message = "Elasticsearch search failed.",
                        error = existsCheck.ElasticsearchServerError?.Error?.Reason
                    });
                }

                if ((existsCheck.HitsMetadata?.Total?.Value ?? 0) == 0)
                    throw new KeyNotFoundException($"Vendvotimi '{vendvotimi}' not found");
            }

            var filters = new List<Query>();

            if (k.HasValue && k.Value != Kategoria.TeGjitha)
                filters.Add(new TermQuery("kategoria") { Value = k.Value.ToString() });

            if (km.HasValue && km.Value != Komuna.TeGjitha)
                filters.Add(new TermQuery("komunaKeyword") { Value = km.Value.ToString() });

            if (!string.IsNullOrWhiteSpace(qendra_e_votimit))
                filters.Add(new TermQuery("qendra_e_votimit") { Value = qendra_e_votimit });

            if (!string.IsNullOrWhiteSpace(vendvotimi))
                filters.Add(new TermQuery("vendvotimi") { Value = vendvotimi });

            SearchResponse<ZgjedhjetDocument> search;

            if (filters.Count == 0)
            {
                search = await _elastic.SearchAsync<ZgjedhjetDocument>(s => s
                    .Index(_esOptions.IndexName)
                    .Size(10_000)
                    .Query(q => q.MatchAll())
                , ct);
            }
            else
            {
                search = await _elastic.SearchAsync<ZgjedhjetDocument>(s => s
                    .Index(_esOptions.IndexName)
                    .Size(10_000)
                    .Query(q => q.Bool(b => b.Filter(filters)))
                , ct);
            }

            if (!search.IsValidResponse)
            {
                return StatusCode(500, new
                {
                    message = "Elasticsearch search failed.",
                    error = search.ElasticsearchServerError?.Error?.Reason
                });
            }

            var docs = search.Hits
                .Select(h => h.Source)
                .Where(x => x != null)
                .ToList()!;

            var entityRows = docs.Select(d => new Zgjedhjet
            {
                Kategoria = d.Kategoria,
                Komuna = d.KomunaKeyword,
                Qendra_e_votimit = d.Qendra_e_votimit,
                Vendvotimi = d.Vendvotimi,

                Partia111 = d.Partia111,
                Partia112 = d.Partia112,
                Partia113 = d.Partia113,
                Partia114 = d.Partia114,
                Partia115 = d.Partia115,
                Partia116 = d.Partia116,
                Partia117 = d.Partia117,
                Partia118 = d.Partia118,
                Partia119 = d.Partia119,
                Partia120 = d.Partia120,
                Partia121 = d.Partia121,
                Partia122 = d.Partia122,
                Partia123 = d.Partia123,
                Partia124 = d.Partia124,
                Partia125 = d.Partia125,
                Partia126 = d.Partia126,
                Partia127 = d.Partia127,
                Partia128 = d.Partia128,
                Partia129 = d.Partia129,
                Partia130 = d.Partia130,
                Partia131 = d.Partia131,
                Partia132 = d.Partia132,
                Partia133 = d.Partia133,
                Partia134 = d.Partia134,
                Partia135 = d.Partia135,
                Partia136 = d.Partia136,
                Partia137 = d.Partia137,
                Partia138 = d.Partia138
            });

            var response = ZgjedhjetService.BuildAggregatedResponse(entityRows, p);

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    private static string NormalizeEnumToken(string s)
        => (s ?? string.Empty).Trim().Replace(" ", "_");
}
