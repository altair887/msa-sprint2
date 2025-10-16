using System.Text.Json.Serialization;

namespace BookingService.DTOs;

public class ReviewDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("hotelId")]
    public string HotelId { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("rating")]
    public int Rating { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }
}
