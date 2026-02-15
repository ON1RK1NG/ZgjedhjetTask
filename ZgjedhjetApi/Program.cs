using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Elastic.Clients.Elasticsearch;
using StackExchange.Redis;
using System.Text.Json.Serialization;
using ZgjedhjetApi.Configuration;
using ZgjedhjetApi.Data;
using ZgjedhjetApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.UseInlineDefinitionsForEnums();
});

builder.Services.AddScoped<IZgjedhjetService, ZgjedhjetService>();

builder.Services.AddDbContext<LifeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LifeDatabase")));

builder.Services.Configure<ElasticsearchOptions>(builder.Configuration.GetSection("Elasticsearch"));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));

builder.Services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;
    var settings = new ElasticsearchClientSettings(new Uri(opts.Url))
        .DefaultIndex(opts.IndexName);

    return new ElasticsearchClient(settings);
});


builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
    return ConnectionMultiplexer.Connect(opts.ConnectionString);
});


builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
    return ConnectionMultiplexer.Connect(opts.ConnectionString);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
