using System.Text.Json.Serialization;

namespace BookingService.Models;

public class BookingCreatedEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("hotelId")]
    public string HotelId { get; set; } = string.Empty;

    [JsonPropertyName("promoCode")]
    public string? PromoCode { get; set; }

    [JsonPropertyName("discountPercent")]
    public double DiscountPercent { get; set; }

    [JsonPropertyName("price")]
    public double Price { get; set; }

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;
}
