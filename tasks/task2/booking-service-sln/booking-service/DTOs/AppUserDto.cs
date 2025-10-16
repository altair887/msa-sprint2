using System.Text.Json.Serialization;

namespace BookingService.DTOs;

public class AppUserDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("blacklisted")]
    public bool Blacklisted { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }
}
