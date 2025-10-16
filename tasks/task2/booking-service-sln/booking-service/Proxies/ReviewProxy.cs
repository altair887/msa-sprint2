using System.Text.Json;
using BookingService.DTOs;

namespace BookingService.Proxies;

public interface IReviewProxy
{
    Task<List<ReviewDto>> GetReviewsForHotelAsync(string hotelId);
    Task<bool> IsHotelTrustedAsync(string hotelId);
}

public class ReviewProxy : IReviewProxy
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReviewProxy> _logger;
    private readonly string _baseUrl;

    public ReviewProxy(HttpClient httpClient, ILogger<ReviewProxy> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["hotelioMonolithUrl"] ?? throw new ArgumentNullException("hotelioMonolithUrl is required");
    }

    public async Task<List<ReviewDto>> GetReviewsForHotelAsync(string hotelId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/reviews/hotel/{hotelId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var reviews = JsonSerializer.Deserialize<List<ReviewDto>>(content);
                return reviews ?? new List<ReviewDto>();
            }
            return new List<ReviewDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews for hotel {HotelId}", hotelId);
            return new List<ReviewDto>();
        }
    }

    public async Task<bool> IsHotelTrustedAsync(string hotelId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/reviews/hotel/{hotelId}/trusted");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return bool.Parse(content);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if hotel is trusted {HotelId}", hotelId);
            return false;
        }
    }
}
