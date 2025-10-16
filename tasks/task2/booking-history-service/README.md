# Booking History Service - .NET Kafka Consumer

This directory contains a dockerized .NET application that consumes "BookingCreated" events from Kafka and stores them in PostgreSQL 15.

## Project Structure

- `BookingHistoryService.csproj` - .NET project file with Kafka and PostgreSQL dependencies
- `Program.cs` - Application entry point and service configuration
- `Models/` - Database models and Entity Framework context
- `Services/BookingCreatedConsumer.cs` - Kafka consumer service
- `Controllers/BookingHistoryController.cs` - REST API endpoints
- `Dockerfile` - Docker configuration for containerizing the application
- `docker-compose.yml` - Docker Compose configuration with Kafka and PostgreSQL

## Features

The booking history service:

1. **Kafka Consumer** - Listens to "BookingCreated" events from Kafka
2. **PostgreSQL Storage** - Stores booking history in PostgreSQL 15 database
3. **REST API** - Provides endpoints to query booking history
4. **Health Checks** - Includes health check endpoints

### Configuration Parameters

- `kafkaConnectionString` - Kafka server connection string (default: localhost:9092)
- `dbConnectionString` - PostgreSQL database connection string

## Running the Service

### Using Docker Compose (Recommended)

```bash
cd tasks/task2/booking-history-service
docker-compose up -d --build
```

This will start:
- **Zookeeper** (port 2181)
- **Kafka** (port 9092)
- **PostgreSQL 15** (port 5432)
- **Booking History Service** (ports 5002/5003)

### Using Docker

```bash
cd tasks/task2/booking-history-service
docker build -t booking-history-service .
docker run -p 5002:80 -p 5003:443 \
  -e kafkaConnectionString=localhost:9092 \
  -e dbConnectionString="Host=localhost;Database=booking_history;Username=postgres;Password=password" \
  booking-history-service
```

## API Endpoints

The service provides the following REST endpoints:

- `GET /` - Service status
- `GET /health` - Health check
- `GET /api/bookinghistory` - Get all booking histories
- `GET /api/bookinghistory/user/{userId}` - Get booking histories for a specific user
- `GET /api/bookinghistory/stats` - Get booking statistics

## Testing the Service

### Send a BookingCreated Event to Kafka

You can use Kafka tools to send test events:

```bash
# Using kafka-console-producer
docker exec -it kafka kafka-console-producer --bootstrap-server localhost:9092 --topic BookingCreated

# Then send a JSON message like:
{"id":"booking123","userId":"user456","hotelId":"hotel789","promoCode":"welcome","discountPercent":10.0,"price":90.0,"createdAt":"2024-01-15T10:30:00Z"}
```

### Query the API

```bash
# Get all booking histories
curl http://localhost:5002/api/bookinghistory

# Get booking histories for a specific user
curl http://localhost:5002/api/bookinghistory/user/user456

# Get statistics
curl http://localhost:5002/api/bookinghistory/stats
```

## Development

To run in development mode:

```bash
cd tasks/task2/booking-history-service
dotnet run
```

Make sure you have Kafka and PostgreSQL running locally with the connection strings configured in `appsettings.json`.

## Database Schema

The service creates a `BookingHistories` table with the following structure:

- `Id` (int, Primary Key)
- `UserId` (string, Required)
- `HotelId` (string, Required)
- `PromoCode` (string, Optional)
- `DiscountPercent` (double)
- `Price` (decimal)
- `CreatedAt` (datetime)
- `EventProcessedAt` (datetime)
