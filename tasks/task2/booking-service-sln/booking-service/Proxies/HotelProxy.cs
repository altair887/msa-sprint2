using System.Text.Json;
using BookingService.DTOs;

namespace BookingService.Proxies;

public interface IHotelProxy
{
    Task<HotelDto?> GetHotelByIdAsync(string hotelId);
    Task<bool> IsHotelOperationalAsync(string hotelId);
    Task<bool> IsHotelFullyBookedAsync(string hotelId);
    Task<List<HotelDto>> FindHotelsByCityAsync(string city);
    Task<List<HotelDto>> GetTopRatedHotelsInCityAsync(string city, int limit = 5);
}

public class HotelProxy : IHotelProxy
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HotelProxy> _logger;
    private readonly string _baseUrl;

    public HotelProxy(HttpClient httpClient, ILogger<HotelProxy> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["hotelioMonolithUrl"] ?? throw new ArgumentNullException("hotelioMonolithUrl is required");
    }

    public async Task<HotelDto?> GetHotelByIdAsync(string hotelId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/hotels/{hotelId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<HotelDto>(content);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotel by ID {HotelId}", hotelId);
            return null;
        }
    }

    public async Task<bool> IsHotelOperationalAsync(string hotelId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/hotels/{hotelId}/operational");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return bool.Parse(content);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if hotel is operational {HotelId}", hotelId);
            return false;
        }
    }

    public async Task<bool> IsHotelFullyBookedAsync(string hotelId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/hotels/{hotelId}/fully-booked");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return bool.Parse(content);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if hotel is fully booked {HotelId}", hotelId);
            return false;
        }
    }

    public async Task<List<HotelDto>> FindHotelsByCityAsync(string city)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/hotels/by-city?city={Uri.EscapeDataString(city)}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var hotels = JsonSerializer.Deserialize<List<HotelDto>>(content);
                return hotels ?? new List<HotelDto>();
            }
            return new List<HotelDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding hotels by city {City}", city);
            return new List<HotelDto>();
        }
    }

    public async Task<List<HotelDto>> GetTopRatedHotelsInCityAsync(string city, int limit = 5)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/hotels/top-rated?city={Uri.EscapeDataString(city)}&limit={limit}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var hotels = JsonSerializer.Deserialize<List<HotelDto>>(content);
                return hotels ?? new List<HotelDto>();
            }
            return new List<HotelDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top rated hotels in city {City}", city);
            return new List<HotelDto>();
        }
    }
}
