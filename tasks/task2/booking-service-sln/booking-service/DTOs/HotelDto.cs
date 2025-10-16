using System.Text.Json.Serialization;

namespace BookingService.DTOs;

public class HotelDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("operational")]
    public bool Operational { get; set; }

    [JsonPropertyName("fullyBooked")]
    public bool FullyBooked { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("rating")]
    public double Rating { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
