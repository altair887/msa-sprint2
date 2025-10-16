# Booking Service - .NET gRPC Application

This directory contains a dockerized .NET application that implements the gRPC contract defined in `booking.proto`.

## Project Structure

- `BookingService.csproj` - .NET project file with gRPC dependencies
- `booking.proto` - Protocol buffer definition for the booking service
- `Program.cs` - Application entry point and gRPC service configuration
- `Services/BookingServiceImpl.cs` - Implementation of the gRPC service methods
- `Dockerfile` - Docker configuration for containerizing the application
- `docker-compose.yml` - Docker Compose configuration for running the service

## Features

The booking service implements two gRPC methods:

1. **CreateBooking** - Creates a new booking with optional promo code discount
2. **ListBookings** - Retrieves all bookings for a specific user

### Promo Codes

The service supports the following promo codes with discounts:
- `welcome` - 10% discount
- `summer` - 15% discount  
- `winter` - 20% discount
- `loyalty` - 25% discount

## Running the Service

### Using Docker Compose

```bash
cd tasks/task2/booking-service
docker-compose up -d --build
```

The service will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

### Using Docker

```bash
cd tasks/task2/booking-service
docker build -t booking-service .
docker run -p 5000:80 -p 5001:443 booking-service
```

## Testing the Service

You can test the gRPC service using tools like:
- [grpcurl](https://github.com/fullstorydev/grpcurl)
- [BloomRPC](https://github.com/uw-labs/bloomrpc)
- Custom gRPC client applications

### Example grpcurl Commands

```bash
# Create a booking
grpcurl -plaintext -d '{"user_id":"user123","hotel_id":"hotel456","promo_code":"welcome"}' \
  localhost:5000 booking.BookingService/CreateBooking

# List bookings for a user
grpcurl -plaintext -d '{"user_id":"user123"}' \
  localhost:5000 booking.BookingService/ListBookings
```

## Development

To run in development mode:

```bash
cd tasks/task2/booking-service
dotnet run
```

The service will start on `http://localhost:5000` and `https://localhost:5001`.
