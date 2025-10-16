using Microsoft.EntityFrameworkCore;
using BookingService.Data;
using BookingService.Models;

namespace BookingService.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly BookingDbContext _context;
    private readonly ILogger<BookingRepository> _logger;

    public BookingRepository(BookingDbContext context, ILogger<BookingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BookingEntity> CreateAsync(BookingEntity booking)
    {
        try
        {
            booking.CreatedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;
            
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created booking with ID {BookingId} for user {UserId}", 
                booking.Id, booking.UserId);
            
            return booking;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking for user {UserId}", booking.UserId);
            throw;
        }
    }

    public async Task<IEnumerable<BookingEntity>> GetByUserIdAsync(string userId)
    {
        try
        {
            return await _context.Bookings
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bookings for user {UserId}", userId);
            throw;
        }
    }

    public async Task<BookingEntity?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Bookings.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving booking with ID {BookingId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<BookingEntity>> GetAllAsync()
    {
        try
        {
            return await _context.Bookings
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all bookings");
            throw;
        }
    }

    public async Task<BookingEntity> UpdateAsync(BookingEntity booking)
    {
        try
        {
            booking.UpdatedAt = DateTime.UtcNow;
            
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Updated booking with ID {BookingId}", booking.Id);
            
            return booking;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating booking with ID {BookingId}", booking.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted booking with ID {BookingId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting booking with ID {BookingId}", id);
            throw;
        }
    }
}
