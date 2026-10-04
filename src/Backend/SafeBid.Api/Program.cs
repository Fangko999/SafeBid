using Microsoft.EntityFrameworkCore;
using SafeBid.Api.Data;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using SafeBid.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();

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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=tcp:localhost,1433;Initial Catalog=SafeBidDb;User ID=sa;Password=StrongPassw0rd!123;Encrypt=False;TrustServerCertificate=True";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

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
    db.Database.Migrate();
}

app.UseCors("AllowFrontend");

app.MapControllers();

app.MapGet("/api/health", async (AppDbContext db) => 
{
    var userCount = await db.Users.CountAsync();
    return Results.Ok(new { status = "Health OK", users = userCount });
})
.WithName("GetHealth");

app.Run("http://localhost:5000");

public partial class Program { }
