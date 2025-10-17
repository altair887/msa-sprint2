using BookingService;
using BookingService.Models;
using BookingService.Repositories;
using BookingService.Proxies;
using Grpc.Core;

namespace BookingService.Services;

public class BookingServiceImpl : BookingService.BookingServiceBase
{
    private readonly ILogger<BookingServiceImpl> _logger;
    private readonly IBookingRepository _bookingRepository;
    private readonly IAppUserProxy _appUserProxy;
    private readonly IHotelProxy _hotelProxy;
    private readonly IReviewProxy _reviewProxy;
    private readonly IPromoCodeProxy _promoCodeProxy;
    private readonly IBookingEventProducer _bookingEventProducer;

    public BookingServiceImpl(
        ILogger<BookingServiceImpl> logger, 
        IBookingRepository bookingRepository,
        IAppUserProxy appUserProxy,
        IHotelProxy hotelProxy,
        IReviewProxy reviewProxy,
        IPromoCodeProxy promoCodeProxy,
        IBookingEventProducer bookingEventProducer)
    {
        _logger = logger;
        _bookingRepository = bookingRepository;
        _appUserProxy = appUserProxy;
        _hotelProxy = hotelProxy;
        _reviewProxy = reviewProxy;
        _promoCodeProxy = promoCodeProxy;
        _bookingEventProducer = bookingEventProducer;
    }

    public override async Task<BookingResponse> CreateBooking(BookingRequest request, ServerCallContext context)
    {
        var peer = context.Peer;
        var method = context.Method;
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("🔵 gRPC CreateBooking Request - Method: {Method}, Peer: {Peer}, Timestamp: {Timestamp}", 
            method, peer, startTime.ToString("yyyy-MM-dd HH:mm:ss.fff")); 
        
        _logger.LogInformation("📥 CreateBooking Details: userId={UserId}, hotelId={HotelId}, promoCode={PromoCode}", 
            request.UserId, request.HotelId, request.PromoCode);

        try
        {
            // Validate user and hotel
            await ValidateUserAsync(request.UserId);
            await ValidateHotelAsync(request.HotelId);

            // Calculate pricing
            var basePrice = await ResolveBasePriceAsync(request.UserId);
            var discount = await ResolvePromoDiscountAsync(request.PromoCode, request.UserId);
            var finalPrice = basePrice - discount;

            _logger.LogInformation("Final price calculated: base={BasePrice}, discount={Discount}, final={FinalPrice}", 
                basePrice, discount, finalPrice);

            // Create booking entity
            var bookingEntity = new BookingEntity
            {
                UserId = request.UserId,
                HotelId = request.HotelId,
                PromoCode = request.PromoCode,
                DiscountPercent = discount,
                Price = (decimal)finalPrice
            };

            // Save to database
            var savedBooking = await _bookingRepository.CreateAsync(bookingEntity);

            // Convert to response
            var booking = new BookingResponse
            {
                Id = savedBooking.Id.ToString(),
                UserId = savedBooking.UserId,
                HotelId = savedBooking.HotelId,
                PromoCode = savedBooking.PromoCode,
                DiscountPercent = savedBooking.DiscountPercent,
                Price = (double)savedBooking.Price,
                CreatedAt = savedBooking.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            // Publish BookingCreated event to Kafka
            await PublishBookingCreatedEventAsync(savedBooking);

            var endTime = DateTime.UtcNow;
            var duration = (endTime - startTime).TotalMilliseconds;
            
            _logger.LogInformation("✅ gRPC CreateBooking Success - Method: {Method}, Peer: {Peer}, Duration: {Duration}ms, BookingId: {BookingId}", 
                method, peer, duration, savedBooking.Id);
            
            _logger.LogInformation("📤 CreateBooking Response: BookingId={BookingId}, Price={Price}, CreatedAt={CreatedAt}", 
                savedBooking.Id, savedBooking.Price, savedBooking.CreatedAt);

            return booking;
        }
        catch (ArgumentException ex)
        {
            var endTime = DateTime.UtcNow;
            var duration = (endTime - startTime).TotalMilliseconds;
            
            _logger.LogWarning(ex, "❌ gRPC CreateBooking Validation Error - Method: {Method}, Peer: {Peer}, Duration: {Duration}ms, Error: {Error}", 
                method, peer, duration, ex.Message);
            
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            var duration = (endTime - startTime).TotalMilliseconds;
            
            _logger.LogError(ex, "❌ gRPC CreateBooking Error - Method: {Method}, Peer: {Peer}, Duration: {Duration}ms, Error: {Error}", 
                method, peer, duration, ex.Message);
            
            throw new RpcException(new Status(StatusCode.Internal, "Failed to create booking"));
        }
    }

    private async Task ValidateUserAsync(string userId)
    {
        var isActive = await _appUserProxy.IsUserActiveAsync(userId);
        if (!isActive)
        {
            _logger.LogWarning("User {UserId} is inactive", userId);
            throw new ArgumentException("User is inactive");
        }

        var isBlacklisted = await _appUserProxy.IsUserBlacklistedAsync(userId);
        if (isBlacklisted)
        {
            _logger.LogWarning("User {UserId} is blacklisted", userId);
            throw new ArgumentException("User is blacklisted");
        }
    }

    private async Task ValidateHotelAsync(string hotelId)
    {
        var isOperational = await _hotelProxy.IsHotelOperationalAsync(hotelId);
        if (!isOperational)
        {
            _logger.LogWarning("Hotel {HotelId} is not operational", hotelId);
            throw new ArgumentException("Hotel is not operational");
        }

        var isTrusted = await _reviewProxy.IsHotelTrustedAsync(hotelId);
        if (!isTrusted)
        {
            _logger.LogWarning("Hotel {HotelId} is not trusted", hotelId);
            throw new ArgumentException("Hotel is not trusted based on reviews");
        }

        var isFullyBooked = await _hotelProxy.IsHotelFullyBookedAsync(hotelId);
        if (isFullyBooked)
        {
            _logger.LogWarning("Hotel {HotelId} is fully booked", hotelId);
            throw new ArgumentException("Hotel is fully booked");
        }
    }

    private async Task<double> ResolveBasePriceAsync(string userId)
    {
        var status = await _appUserProxy.GetUserStatusAsync(userId);
        if (status != null)
        {
            var isVip = status.Equals("VIP", StringComparison.OrdinalIgnoreCase);
            var basePrice = isVip ? 80.0 : 100.0;
            _logger.LogDebug("User {UserId} has status '{Status}', base price is {BasePrice}", 
                userId, status, basePrice);
            return basePrice;
        }

        _logger.LogDebug("User {UserId} has unknown status, default base price 100.0", userId);
        return 100.0;
    }

    private async Task<double> ResolvePromoDiscountAsync(string? promoCode, string userId)
    {
        if (string.IsNullOrEmpty(promoCode))
            return 0.0;

        var promo = await _promoCodeProxy.ValidatePromoAsync(promoCode, userId);
        if (promo == null)
        {
            _logger.LogInformation("Promo code '{PromoCode}' is invalid or not applicable for user {UserId}", 
                promoCode, userId);
            return 0.0;
        }

        _logger.LogDebug("Promo code '{PromoCode}' applied with discount {Discount}", 
            promoCode, promo.Discount);
        return promo.Discount;
    }

    public override async Task<BookingListResponse> ListBookings(BookingListRequest request, ServerCallContext context)
    {
        var peer = context.Peer;
        var method = context.Method;
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("🔵 gRPC ListBookings Request - Method: {Method}, Peer: {Peer}, Timestamp: {Timestamp}", 
            method, peer, startTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        
        _logger.LogInformation("📥 ListBookings Details: userId={UserId}", request.UserId);

        try
        {
            var userBookings = await _bookingRepository.GetByUserIdAsync(request.UserId);

            var response = new BookingListResponse();
            foreach (var bookingEntity in userBookings)
            {
                var booking = new BookingResponse
                {
                    Id = bookingEntity.Id.ToString(),
                    UserId = bookingEntity.UserId,
                    HotelId = bookingEntity.HotelId,
                    PromoCode = bookingEntity.PromoCode,
                    DiscountPercent = bookingEntity.DiscountPercent,
                    Price = (double)bookingEntity.Price,
                    CreatedAt = bookingEntity.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                response.Bookings.Add(booking);
            }

            var endTime = DateTime.UtcNow;
            var duration = (endTime - startTime).TotalMilliseconds;
            var bookingCount = userBookings.Count();
            
            _logger.LogInformation("✅ gRPC ListBookings Success - Method: {Method}, Peer: {Peer}, Duration: {Duration}ms, Count: {Count}", 
                method, peer, duration, bookingCount);
            
            _logger.LogInformation("📤 ListBookings Response: Found {Count} bookings for user {UserId}", 
                bookingCount, request.UserId);

            return response;
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            var duration = (endTime - startTime).TotalMilliseconds;
            
            _logger.LogError(ex, "❌ gRPC ListBookings Error - Method: {Method}, Peer: {Peer}, Duration: {Duration}ms, Error: {Error}", 
                method, peer, duration, ex.Message);
            
            throw new RpcException(new Status(StatusCode.Internal, "Failed to list bookings"));
        }
    }

    private async Task PublishBookingCreatedEventAsync(BookingEntity bookingEntity)
    {
        try
        {
            var bookingEvent = new BookingCreatedEvent
            {
                Id = bookingEntity.Id.ToString(),
                UserId = bookingEntity.UserId,
                HotelId = bookingEntity.HotelId,
                PromoCode = bookingEntity.PromoCode,
                DiscountPercent = bookingEntity.DiscountPercent,
                Price = (double)bookingEntity.Price,
                CreatedAt = bookingEntity.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            await _bookingEventProducer.PublishBookingCreatedEventAsync(bookingEvent);
            
            _logger.LogInformation("BookingCreated event published for booking {BookingId}", bookingEntity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish BookingCreated event for booking {BookingId}", bookingEntity.Id);
            // Don't throw exception here to avoid failing the booking creation
            // The booking is already saved to database
        }
    }
}
