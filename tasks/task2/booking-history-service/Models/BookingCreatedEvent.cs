namespace BookingHistoryService.Models;

public class BookingCreatedEvent
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string HotelId { get; set; } = string.Empty;
    public string? PromoCode { get; set; }
    public double DiscountPercent { get; set; }
    public double Price { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}
