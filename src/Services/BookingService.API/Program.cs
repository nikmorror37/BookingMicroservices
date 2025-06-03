using BookingService.API.Infrastructure.Data;
using BookingService.API.Infrastructure.Clients;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MassTransit;
using BookingService.API.Consumers;
using BookingService.API.Infrastructure.Handlers;

var builder = WebApplication.CreateBuilder(args);

// EF
var connectionString = builder.Configuration.GetConnectionString("BookingDb") 
    ?? throw new InvalidOperationException("Connection string 'BookingDb' not found.");

builder.Services.AddDbContext<BookingDbContext>(opts =>
    opts.UseSqlServer(connectionString)
);

// Add HealthChecks

var roomServiceUrl = builder.Configuration["Services:RoomService"] 
    ?? throw new InvalidOperationException("RoomService URL not configured");
var catalogServiceUrl = builder.Configuration["Services:CatalogService"] 
    ?? throw new InvalidOperationException("CatalogService URL not configured");
var paymentServiceUrl = builder.Configuration["Services:PaymentService"] 
    ?? throw new InvalidOperationException("PaymentService URL not configured");

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString, 
        name: "booking-db",
        tags: ["db", "sql", "ready"])
    .AddRabbitMQ(
        $"amqp://guest:guest@{builder.Configuration["EventBus:Host"] ?? "rabbitmq"}:5672",
        name: "rabbitmq",
        tags: ["messaging", "rabbitmq"])
    .AddUrlGroup(
        new Uri($"{roomServiceUrl}/health"),
        name: "room-service",
        tags: ["dependency"])
    .AddUrlGroup(
        new Uri($"{catalogServiceUrl}/health"),
        name: "catalog-service", 
        tags: ["dependency"])
    .AddUrlGroup(
        new Uri($"{paymentServiceUrl}/health"),
        name: "payment-service",
        tags: ["dependency"]);


// HttpClient for RoomService
builder.Services.AddHttpClient<IRoomServiceClient, RoomServiceClient>(client =>
{
    client.BaseAddress = new Uri(roomServiceUrl);
});

// HttpClient for CatalogService
builder.Services.AddHttpClient<IHotelServiceClient, HotelServiceClient>(client =>
{
    client.BaseAddress = new Uri(catalogServiceUrl);
});

// Register HttpContextAccessor and token propagation handler
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BearerTokenHandler>();

// HttpClient for PaymentService (with Bearer token propagation)
builder.Services.AddHttpClient<IPaymentServiceClient, PaymentServiceClient>(client =>
{
    client.BaseAddress = new Uri(paymentServiceUrl);
})
.AddHttpMessageHandler<BearerTokenHandler>();

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); //new

// Auth + JWT
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine("JWT Auth Failed: " + ctx.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                Console.WriteLine("JWT Validated for: " + ctx.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
        options.RequireHttpsMetadata = false;
        options.SaveToken            = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidateAudience         = true,
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            ValidateLifetime         = true,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                            Encoding.UTF8.GetBytes(
                                                builder.Configuration["Jwt:Key"]!)),
            ValidateIssuerSigningKey = true,
            NameClaimType            = ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddAuthorization();

// MassTransit + RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // register consumers
    x.AddConsumer<PaymentCreatedConsumer>();
    x.AddConsumer<RoomReserveRejectedConsumer>();
    x.AddConsumer<PaymentRefundedConsumer>();
    x.AddConsumer<PaymentRefundFailedConsumer>();
    x.AddConsumer<CancelBookingTimeoutConsumer>();

    // enable delayed message scheduler
    x.AddDelayedMessageScheduler();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.UseDelayedMessageScheduler();

        var evt = builder.Configuration.GetSection("EventBus");
        cfg.Host(evt["Host"]!, evt["VirtualHost"]!, h =>
        {
            h.Username(evt["Username"]!);
            h.Password(evt["Password"]!);
        });

        cfg.ReceiveEndpoint("booking-service-queue", e =>
        {
            // connect consumers
            e.ConfigureConsumer<PaymentCreatedConsumer>(context);
            e.ConfigureConsumer<RoomReserveRejectedConsumer>(context);
            e.ConfigureConsumer<PaymentRefundedConsumer>(context);
            e.ConfigureConsumer<PaymentRefundFailedConsumer>(context);
            e.ConfigureConsumer<CancelBookingTimeoutConsumer>(context);
        });
    });
});
builder.Services.AddMassTransitHostedService();

// MVC + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BookingService.API", Version = "v1" });

    // In Swagger add Bearer-token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "JWT Authorization header. Enter format: {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health checks endpoint
app.MapHealthChecks("/health");

// Apply EF Core migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    db.Database.Migrate();
}

app.Run();
