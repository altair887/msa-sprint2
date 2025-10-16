using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookingHistoryService.Models;

namespace BookingHistoryService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingHistoryController : ControllerBase
{
    private readonly BookingHistoryContext _context;
    private readonly ILogger<BookingHistoryController> _logger;

    public BookingHistoryController(BookingHistoryContext context, ILogger<BookingHistoryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingHistory>>> GetBookingHistories()
    {
        return await _context.BookingHistories
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<BookingHistory>>> GetBookingHistoriesByUser(string userId)
    {
        var bookings = await _context.BookingHistories
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return Ok(bookings);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetStats()
    {
        var totalBookings = await _context.BookingHistories.CountAsync();
        var totalRevenue = await _context.BookingHistories.SumAsync(b => b.Price);
        var avgPrice = await _context.BookingHistories.AverageAsync(b => (double)b.Price);

        return Ok(new
        {
            TotalBookings = totalBookings,
            TotalRevenue = totalRevenue,
            AveragePrice = Math.Round(avgPrice, 2)
        });
    }
}
