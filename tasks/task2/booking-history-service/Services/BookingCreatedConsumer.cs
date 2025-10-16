using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using BookingHistoryService.Models;

namespace BookingHistoryService.Services;

public class BookingCreatedConsumer : BackgroundService
{
    private readonly ILogger<BookingCreatedConsumer> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _kafkaConnectionString;
    private readonly string _topicName = "BookingCreated";

    public BookingCreatedConsumer(
        ILogger<BookingCreatedConsumer> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _kafkaConnectionString = _configuration["kafkaConnectionString"] 
            ?? throw new ArgumentNullException(nameof(_kafkaConnectionString), "kafkaConnectionString is required");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting BookingCreated Kafka consumer...");

        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaConnectionString,
            GroupId = "booking-history-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        
        try
        {
            consumer.Subscribe(_topicName);
            _logger.LogInformation("Subscribed to topic: {TopicName}", _topicName);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    
                    if (consumeResult?.Message?.Value != null)
                    {
                        await ProcessBookingCreatedEvent(consumeResult.Message.Value);
                        consumer.Commit(consumeResult);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in Kafka consumer");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer is stopping...");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessBookingCreatedEvent(string messageValue)
    {
        try
        {
            _logger.LogInformation("Processing BookingCreated event: {Message}", messageValue);

            var bookingEvent = JsonConvert.DeserializeObject<BookingCreatedEvent>(messageValue);
            if (bookingEvent == null)
            {
                _logger.LogWarning("Failed to deserialize BookingCreated event");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BookingHistoryContext>();

            var bookingHistory = new BookingHistory
            {
                UserId = bookingEvent.UserId,
                HotelId = bookingEvent.HotelId,
                PromoCode = bookingEvent.PromoCode,
                DiscountPercent = bookingEvent.DiscountPercent,
                Price = (decimal)bookingEvent.Price,
                CreatedAt = DateTime.Parse(bookingEvent.CreatedAt),
                EventProcessedAt = DateTime.UtcNow
            };

            context.BookingHistories.Add(bookingHistory);
            await context.SaveChangesAsync();

            _logger.LogInformation("Successfully saved booking history for user {UserId}, hotel {HotelId}", 
                bookingEvent.UserId, bookingEvent.HotelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing BookingCreated event: {Message}", messageValue);
        }
    }
}
