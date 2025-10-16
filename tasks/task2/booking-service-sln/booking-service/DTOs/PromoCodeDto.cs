using System.Text.Json.Serialization;

namespace BookingService.DTOs;

public class PromoCodeDto
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("discount")]
    public double Discount { get; set; }

    [JsonPropertyName("vipOnly")]
    public bool VipOnly { get; set; }

    [JsonPropertyName("expired")]
    public bool Expired { get; set; }

    [JsonPropertyName("validUntil")]
    public string? ValidUntil { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
