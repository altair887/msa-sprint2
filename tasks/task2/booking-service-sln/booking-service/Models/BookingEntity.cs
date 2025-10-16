using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingService.Models;

public class BookingEntity
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string HotelId { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? PromoCode { get; set; }
    
    public double DiscountPercent { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}
