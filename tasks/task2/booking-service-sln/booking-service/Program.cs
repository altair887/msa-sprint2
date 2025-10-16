using BookingService.Services;
using BookingService.Data;
using BookingService.Repositories;
using BookingService.Proxies;
using BookingService.Interceptors;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

// Configure Serilog for detailed gRPC logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Grpc", LogEventLevel.Debug)
    .MinimumLevel.Override("Grpc.Net.Client", LogEventLevel.Debug)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Booking Service...");
    
    var builder = WebApplication.CreateBuilder(args);
    
    // Add Serilog
    builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddGrpc(options =>
{
    // Enable detailed gRPC logging
    options.EnableDetailedErrors = true;
    // Add the interceptor globally
    options.Interceptors.Add<GrpcLoggingInterceptor>();
});

// Add Entity Framework
var dbConnectionString = builder.Configuration["dbConnectionString"] 
    ?? throw new ArgumentNullException("dbConnectionString", "dbConnectionString is required");

builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseNpgsql(dbConnectionString));

// Add repository
builder.Services.AddScoped<IBookingRepository, BookingRepository>();

// Add HttpClient for monolith API calls
builder.Services.AddHttpClient();

// Add proxy services
builder.Services.AddScoped<IAppUserProxy, AppUserProxy>();
builder.Services.AddScoped<IHotelProxy, HotelProxy>();
builder.Services.AddScoped<IReviewProxy, ReviewProxy>();
builder.Services.AddScoped<IPromoCodeProxy, PromoCodeProxy>();

// Add Kafka producer service
builder.Services.AddSingleton<IBookingEventProducer, BookingEventProducer>();

// Health checks removed - service is gRPC-only

// Configure Kestrel for gRPC only (HTTP/2)
// Configuration is now handled in appsettings.json
builder.WebHost.ConfigureKestrel(options =>
{
    // Enable connection logging
    options.ConfigureEndpointDefaults(listenOptions =>
    {
        listenOptions.UseConnectionLogging();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<BookingServiceImpl>();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    Log.Information("Ensuring database is created...");
    context.Database.EnsureCreated();
    Log.Information("Database ready");
}

Log.Information("Booking Service started successfully");
Log.Information("gRPC endpoint: grpc://localhost:9090 (HTTP/2 only)");
Log.Information("Service is gRPC-only - no HTTP/1.1 endpoints available");

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
