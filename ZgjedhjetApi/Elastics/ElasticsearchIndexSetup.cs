using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using System.Text.Json;

namespace ZgjedhjetApi.Elastics;

public static class ElasticsearchIndexSetup
{
    public static async Task EnsureIndexAsync(ElasticsearchClient client, string indexName, CancellationToken ct)
    {
        var exists = await client.Indices.ExistsAsync(indexName, ct);
        if (exists.Exists) return;

        var body = new
        {
            settings = new
            {
                analysis = new
                {
                    analyzer = new
                    {
                        komuna_analyzer = new
                        {
                            type = "custom",
                            tokenizer = "standard",
                            filter = new[] { "lowercase", "asciifolding" }
                        }
                    }
                }
            },
            mappings = new
            {
                properties = new
                {
                    id = new { type = "keyword" },
                    kategoria = new { type = "keyword" },
                    komuna = new { type = "text", analyzer = "komuna_analyzer" },
                    komunaKeyword = new { type = "keyword" },
                    qendra_e_votimit = new { type = "keyword" },
                    vendvotimi = new { type = "keyword" }
                }
            }
        };

        var json = JsonSerializer.Serialize(body);

        var response = await client.Transport.RequestAsync<StringResponse>(
            Elastic.Transport.HttpMethod.PUT,
            $"/{indexName}",
            PostData.String(json),
            ct);

        if (response == null )
            throw new Exception($"Failed creating index '{indexName}'. Response: {response?.Body}");
    }
}
