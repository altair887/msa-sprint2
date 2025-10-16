using System.Text.Json;
using BookingService.DTOs;

namespace BookingService.Proxies;

public interface IAppUserProxy
{
    Task<AppUserDto?> GetUserByIdAsync(string userId);
    Task<string?> GetUserStatusAsync(string userId);
    Task<bool> IsUserBlacklistedAsync(string userId);
    Task<bool> IsUserActiveAsync(string userId);
    Task<bool> IsUserAuthorizedAsync(string userId);
    Task<bool> IsUserVipAsync(string userId);
}

public class AppUserProxy : IAppUserProxy
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AppUserProxy> _logger;
    private readonly string _baseUrl;

    public AppUserProxy(HttpClient httpClient, ILogger<AppUserProxy> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["hotelioMonolithUrl"] ?? throw new ArgumentNullException("hotelioMonolithUrl is required");
    }

    public async Task<AppUserDto?> GetUserByIdAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/users/{userId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<AppUserDto>(content);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID {UserId}", userId);
            return null;
        }
    }

    public async Task<string?> GetUserStatusAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/users/{userId}/status");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user status for {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> IsUserBlacklistedAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/users/{userId}/blacklisted");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return bool.Parse(content);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user is blacklisted {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> IsUserActiveAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/users/{userId}/active");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return bool.Parse(content);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user is active {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> IsUserAuthorizedAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/users/{userId}/authorized");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return bool.Parse(content);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user is authorized {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> IsUserVipAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/users/{userId}/vip");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return bool.Parse(content);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user is VIP {UserId}", userId);
            return false;
        }
    }
}
