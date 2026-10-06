using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using SafeBid.Infrastructure;
using SafeBid.Api.Services;
using Minio;
using SafeBid.Application;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddScoped<SafeBid.Application.IJwtProvider, SafeBid.Infrastructure.JwtProvider>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secretKey = builder.Configuration["Jwt:Secret"] ?? "SuperSecretKeyThatIsAtLeast32BytesLongForSafeBid";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "SafeBid",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "SafeBid",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("jwt", out var token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

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

    options.AddPolicy("QuestionLimit", context =>
    {
        var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? 
                 context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
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

var minioEndpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT") ?? builder.Configuration["MINIO_ENDPOINT"] ?? "localhost:9000";
var minioAccessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY") ?? builder.Configuration["MINIO_ACCESS_KEY"] ?? "admin";
var minioSecretKey = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY") ?? builder.Configuration["MINIO_SECRET_KEY"] ?? "password123";

builder.Services.AddMinio(configureClient => configureClient
    .WithEndpoint(minioEndpoint)
    .WithCredentials(minioAccessKey, minioSecretKey)
    .WithSSL(false)
    .Build());

builder.Services.AddScoped<SafeBid.Application.IStorageService, SafeBid.Infrastructure.MinioService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Auto apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

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
