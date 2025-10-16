using BookingService.Models;

namespace BookingService.Repositories;

public interface IBookingRepository
{
    Task<BookingEntity> CreateAsync(BookingEntity booking);
    Task<IEnumerable<BookingEntity>> GetByUserIdAsync(string userId);
    Task<BookingEntity?> GetByIdAsync(int id);
    Task<IEnumerable<BookingEntity>> GetAllAsync();
    Task<BookingEntity> UpdateAsync(BookingEntity booking);
    Task DeleteAsync(int id);
}
