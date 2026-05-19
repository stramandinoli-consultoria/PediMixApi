using System.Text.Json.Serialization;
using System.Text.Json;

namespace PediMix.API.Services;

public interface IViaCepService
{
    Task<ViaCepResponse?> GetAddressByCepAsync(string cep, CancellationToken cancellationToken = default);
}

public class ViaCepService : IViaCepService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://viacep.com.br/ws";

    public ViaCepService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ViaCepResponse?> GetAddressByCepAsync(string cep, CancellationToken cancellationToken = default)
    {
        try
        {
            var cleaned = new string(cep.Where(char.IsDigit).ToArray());

            if (cleaned.Length != 8)
            {
                return null;
            }

            var url = $"{BaseUrl}/{cleaned}/json/";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<ViaCepResponse>(content, options);
            
            // ViaCEP retorna "erro": true quando CEP não encontrado
            if (result?.Erro == true)
            {
                return null;
            }

            return result;
        }
        catch (Exception)
        {
            return null;
        }
    }
}

public class ViaCepResponse
{
    [JsonPropertyName("cep")]
    public string? Cep { get; set; }

    [JsonPropertyName("logradouro")]
    public string? Street { get; set; }

    [JsonPropertyName("complemento")]
    public string? Complement { get; set; }

    [JsonPropertyName("bairro")]
    public string? Neighborhood { get; set; }

    [JsonPropertyName("localidade")]
    public string? City { get; set; }

    [JsonPropertyName("uf")]
    public string? State { get; set; }

    [JsonPropertyName("ibge")]
    public string? Ibge { get; set; }

    [JsonPropertyName("gia")]
    public string? Gia { get; set; }

    [JsonPropertyName("ddd")]
    public string? Ddd { get; set; }

    [JsonPropertyName("siafi")]
    public string? Siafi { get; set; }

    [JsonPropertyName("erro")]
    public bool Erro { get; set; }
}
