namespace BookingHistoryService.Models;

public class BookingHistory
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string HotelId { get; set; } = string.Empty;
    public string? PromoCode { get; set; }
    public double DiscountPercent { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime EventProcessedAt { get; set; }
}
