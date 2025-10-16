using Microsoft.EntityFrameworkCore;

namespace BookingHistoryService.Models;

public class BookingHistoryContext : DbContext
{
    public BookingHistoryContext(DbContextOptions<BookingHistoryContext> options)
        : base(options)
    {
    }

    public DbSet<BookingHistory> BookingHistories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookingHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.HotelId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PromoCode).HasMaxLength(50);
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.EventProcessedAt).IsRequired();
        });
    }
}
