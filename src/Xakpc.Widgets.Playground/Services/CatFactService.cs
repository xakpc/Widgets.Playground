using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Xakpc.Widgets.Playground.Services;

public sealed class CatFactService
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://catfact.ninja/"),
        Timeout = TimeSpan.FromSeconds(10)
    };

    public async Task<string?> GetFactAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await Http.GetFromJsonAsync<CatFactResponse>("fact", ct);
            var fact = response?.Fact?.Trim();
            return string.IsNullOrWhiteSpace(fact) ? null : fact;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private sealed class CatFactResponse
    {
        [JsonPropertyName("fact")]
        public string? Fact { get; init; }
    }
}
