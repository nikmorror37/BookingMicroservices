using Microsoft.EntityFrameworkCore;
using RoomService.API.Infrastructure.Data;
using MassTransit;
using RoomService.API.Consumers;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("RoomDb")
    ?? throw new InvalidOperationException("Connection string 'RoomDb' not found.");

builder.Services.AddDbContext<RoomDbContext>(opts =>
    opts.UseSqlServer(connectionString));

// add HealthChecks
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString)
    .AddRabbitMQ(rabbitConnectionString: "amqp://guest:guest@rabbitmq:5672");

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "RoomService.API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter format: {token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }, new string[] {}
        }
    });
});

// after builder initialization, configure MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<RoomReserveRequestedConsumer>();
    x.AddConsumer<BookingCancelledConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var evt = builder.Configuration.GetSection("EventBus");
        cfg.Host(evt["Host"], evt["VirtualHost"], h =>
        {
            h.Username(evt["Username"]);
            h.Password(evt["Password"]);
        });

        cfg.ReceiveEndpoint("room-service-queue", e =>
        {
            e.ConfigureConsumer<RoomReserveRequestedConsumer>(context);
            e.ConfigureConsumer<BookingCancelledConsumer>(context);
        });
    });
});
builder.Services.AddMassTransitHostedService();

builder.Services
    .AddAuthentication(options => {
        options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options => {
        options.RequireHttpsMetadata = false;
        options.SaveToken            = true;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters {
            ValidateIssuer           = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidateAudience         = true,
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                                           System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            NameClaimType            = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// health checks endpoint
app.MapHealthChecks("/health");

// Apply EF Core migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RoomDbContext>();
    db.Database.Migrate();

    // Seed default rooms
    if (!db.Rooms.Any())
    {
        db.Rooms.AddRange(
            // Hilton Warsaw (hotelId 1)
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 1,
                Number = "101",
                Type = RoomService.API.Domain.Models.RoomType.Single,
                Price = 120m,
                Description = "Standard single room",
                IsAvailable = true,
                NumberOfBeds = 1,
                Capacity = 1
            },
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 1,
                Number = "112",
                Type = RoomService.API.Domain.Models.RoomType.Double,
                Price = 180m,
                Description = "Comfortable double room",
                IsAvailable = true,
                NumberOfBeds = 1,
                Capacity = 2
            },
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 1,
                Number = "123",
                Type = RoomService.API.Domain.Models.RoomType.Twin,
                Price = 180m,
                Description = "Pretty twin room",
                IsAvailable = true,
                NumberOfBeds = 2,
                Capacity = 2
            },
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 1,
                Number = "134",
                Type = RoomService.API.Domain.Models.RoomType.Suite,
                Price = 300m,
                Description = "Luxury suite",
                IsAvailable = true,
                NumberOfBeds = 2,
                Capacity = 4
            },
            // Raffles Warsaw (hotelId 2)
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 2,
                Number = "201",
                Type = RoomService.API.Domain.Models.RoomType.Double,
                Price = 250m,
                Description = "Luxure double room",
                IsAvailable = true,
                NumberOfBeds = 1,
                Capacity = 2
            },
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 2,
                Number = "202",
                Type = RoomService.API.Domain.Models.RoomType.Twin,
                Price = 270m,
                Description = "Luxure twin room",
                IsAvailable = true,
                NumberOfBeds = 2,
                Capacity = 2
            },
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 2,
                Number = "203",
                Type = RoomService.API.Domain.Models.RoomType.Suite,
                Price = 530m,
                Description = "Luxure suite room",
                IsAvailable = true,
                NumberOfBeds = 2,
                Capacity = 3
            },
            // Westin Warsaw (hotelId 3)
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 3,
                Number = "301",
                Type = RoomService.API.Domain.Models.RoomType.Single,
                Price = 150m,
                Description = "Standard single room",
                IsAvailable = true,
                NumberOfBeds = 1,
                Capacity = 1
            },
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 3,
                Number = "302",
                Type = RoomService.API.Domain.Models.RoomType.Double,
                Price = 220m,
                Description = "Comfortable double room",
                IsAvailable = true,
                NumberOfBeds = 1,
                Capacity = 2
            },
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 3,
                Number = "303",
                Type = RoomService.API.Domain.Models.RoomType.Twin,
                Price = 220m,
                Description = "Pretty twin room",
                IsAvailable = true,
                NumberOfBeds = 2,
                Capacity = 2
            },
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 3,
                Number = "304",
                Type = RoomService.API.Domain.Models.RoomType.Suite,
                Price = 400m,
                Description = "Luxury suite",
                IsAvailable = true,
                NumberOfBeds = 2,
                Capacity = 3
            },
            // Ibis Styles Centrum Warsaw (hotelId 4)
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 4,
                Number = "101",
                Type = RoomService.API.Domain.Models.RoomType.Single,
                Price = 90m,
                Description = "Standard single room",
                IsAvailable = true,
                NumberOfBeds = 1,
                Capacity = 1
            },
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 4,
                Number = "202",
                Type = RoomService.API.Domain.Models.RoomType.Double,
                Price = 130m,
                Description = "Comfortable double room",
                IsAvailable = true,
                NumberOfBeds = 1,
                Capacity = 2
            },
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 4,
                Number = "303",
                Type = RoomService.API.Domain.Models.RoomType.Twin,
                Price = 130m,
                Description = "Pretty twin room",
                IsAvailable = true,
                NumberOfBeds = 2,
                Capacity = 2
            },
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 4,
                Number = "404",
                Type = RoomService.API.Domain.Models.RoomType.Suite,
                Price = 200m,
                Description = "Cool suite",
                IsAvailable = true,
                NumberOfBeds = 2,
                Capacity = 3
            },
            // Mercure Grand Warsaw (hotelId 5)
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 5,
                Number = "501",
                Type = RoomService.API.Domain.Models.RoomType.Single,
                Price = 110m,
                Description = "Standard single room",
                IsAvailable = true,
                NumberOfBeds = 1,
                Capacity = 1
            },
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 5,
                Number = "502",
                Type = RoomService.API.Domain.Models.RoomType.Double,
                Price = 170m,
                Description = "Comfortable double room",
                IsAvailable = true,
                NumberOfBeds = 1,
                Capacity = 2
            },
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 5,
                Number = "503",
                Type = RoomService.API.Domain.Models.RoomType.Twin,
                Price = 170m,
                Description = "Pretty twin room",
                IsAvailable = true,
                NumberOfBeds = 2,
                Capacity = 2
            },
            new RoomService.API.Domain.Models.Room
            {
                HotelId = 5,
                Number = "504",
                Type = RoomService.API.Domain.Models.RoomType.Suite,
                Price = 320m,
                Description = "Comfortable suite",
                IsAvailable = true,
                NumberOfBeds = 2,
                Capacity = 3
            }
        );
        db.SaveChanges();
    }
}

app.Run();
