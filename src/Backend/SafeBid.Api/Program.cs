using Microsoft.EntityFrameworkCore;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using SafeBid.Infrastructure;
using SafeBid.Api.Services;

using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();

// Configure Rate Limiter for Registration
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("RegisterLimit", context =>
    {
        var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? 
                 context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
});

// Configure MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SafeBid.Application.RegisterCommand).Assembly));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=tcp:localhost,1433;Initial Catalog=SafeBidDb;User ID=sa;Password=StrongPassw0rd!123;Encrypt=False;TrustServerCertificate=True";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<SafeBid.Application.IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

// Redis & RedLock configuration
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379";
var multiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
builder.Services.AddSingleton<IDistributedLockFactory>(sp => 
    RedLockFactory.Create(new List<RedLockMultiplexer> { new RedLockMultiplexer(multiplexer) })
);
builder.Services.AddSingleton<RedisLockService>();
builder.Services.AddScoped<SafeBid.Api.Filters.HmacAuthFilter>();

var app = builder.Build();

// Auto apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.MapControllers();

app.MapGet("/api/health", async (AppDbContext db) => 
{
    var userCount = await db.Users.CountAsync();
    return Results.Ok(new { status = "Health OK", users = userCount });
})
.WithName("GetHealth");

app.Run("http://localhost:5000");

public partial class Program { }
