namespace ZgjedhjetApi.Configuration;

public sealed class RedisOptions
{
    public string ConnectionString { get; set; } = "";
    public string SuggestionsKey { get; set; } = "";
}
