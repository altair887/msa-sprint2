using Microsoft.EntityFrameworkCore;
using BookingHistoryService.Models;
using BookingHistoryService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add Entity Framework
var dbConnectionString = builder.Configuration["dbConnectionString"] 
    ?? throw new ArgumentNullException("dbConnectionString", "dbConnectionString is required");

builder.Services.AddDbContext<BookingHistoryContext>(options =>
    options.UseNpgsql(dbConnectionString));

// Add Kafka consumer as hosted service
builder.Services.AddHostedService<BookingCreatedConsumer>();

// Add health check
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseRouting();

app.MapControllers();

// Add health check endpoint
app.MapHealthChecks("/health");

// Simple status endpoint
app.MapGet("/", () => "Booking History Service is running!");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BookingHistoryContext>();
    context.Database.EnsureCreated();
}

app.Run();
