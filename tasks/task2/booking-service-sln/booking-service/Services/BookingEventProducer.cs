using Confluent.Kafka;
using System.Text.Json;
using BookingService.Models;

namespace BookingService.Services;

public interface IBookingEventProducer
{
    Task PublishBookingCreatedEventAsync(BookingCreatedEvent bookingEvent);
}

public class BookingEventProducer : IBookingEventProducer
{
    private readonly ILogger<BookingEventProducer> _logger;
    private readonly IProducer<Null, string> _producer;
    private readonly string _topicName = "BookingCreated";

    public BookingEventProducer(ILogger<BookingEventProducer> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        var kafkaConnectionString = configuration["kafkaConnectionString"] 
            ?? throw new ArgumentNullException("kafkaConnectionString", "kafkaConnectionString is required");

        var config = new ProducerConfig
        {
            BootstrapServers = kafkaConnectionString,
            Acks = Acks.All,
            //Retries = 3,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task PublishBookingCreatedEventAsync(BookingCreatedEvent bookingEvent)
    {
        try
        {
            var message = JsonSerializer.Serialize(bookingEvent);
            
            var kafkaMessage = new Message<Null, string>
            {
                Value = message
            };

            var result = await _producer.ProduceAsync(_topicName, kafkaMessage);
            
            _logger.LogInformation("BookingCreated event published successfully. Topic: {Topic}, Partition: {Partition}, Offset: {Offset}", 
                result.Topic, result.Partition, result.Offset);
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogError(ex, "Failed to publish BookingCreated event. Error: {Error}", ex.Error.Reason);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error publishing BookingCreated event");
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}
