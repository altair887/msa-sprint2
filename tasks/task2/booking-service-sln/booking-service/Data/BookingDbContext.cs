using Microsoft.EntityFrameworkCore;
using BookingService.Models;

namespace BookingService.Data;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options)
        : base(options)
    {
    }

    public DbSet<BookingEntity> Bookings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookingEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.HotelId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PromoCode).HasMaxLength(50);
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            // Create index on UserId for better query performance
            entity.HasIndex(e => e.UserId);
            
            // Create index on HotelId for better query performance
            entity.HasIndex(e => e.HotelId);
        });
    }
}
