using System.Text.Json;
using BookingService.DTOs;

namespace BookingService.Proxies;

public interface IPromoCodeProxy
{
    Task<PromoCodeDto?> GetPromoByCodeAsync(string code);
    Task<bool> IsPromoValidAsync(string code, bool isVipUser = false);
    Task<PromoCodeDto?> ValidatePromoAsync(string code, string userId);
}

public class PromoCodeProxy : IPromoCodeProxy
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PromoCodeProxy> _logger;
    private readonly string _baseUrl;

    public PromoCodeProxy(HttpClient httpClient, ILogger<PromoCodeProxy> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["hotelioMonolithUrl"] ?? throw new ArgumentNullException("hotelioMonolithUrl is required");
    }

    public async Task<PromoCodeDto?> GetPromoByCodeAsync(string code)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/promos/{code}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PromoCodeDto>(content);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting promo by code {Code}", code);
            return null;
        }
    }

    public async Task<bool> IsPromoValidAsync(string code, bool isVipUser = false)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/promos/{code}/valid?isVipUser={isVipUser}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return bool.Parse(content);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if promo is valid {Code}", code);
            return false;
        }
    }

    public async Task<PromoCodeDto?> ValidatePromoAsync(string code, string userId)
    {
        try
        {
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("userId", userId)
            });

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/promos/validate", formData);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PromoCodeDto>(content);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating promo {Code} for user {UserId}", code, userId);
            return null;
        }
    }
}
