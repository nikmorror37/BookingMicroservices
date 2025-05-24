using Microsoft.EntityFrameworkCore;
using RoomService.API.Infrastructure.Data;
using MassTransit;
using RoomService.API.Consumers;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("RoomDb");

builder.Services.AddDbContext<RoomDbContext>(opts =>
    opts.UseSqlServer(connectionString));

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply EF Core migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RoomDbContext>();
    db.Database.Migrate();

    // Seed default rooms
    if(!db.Rooms.Any())
    {
        db.Rooms.AddRange(
            // Hilton Midtown (hotelId 1)
            new RoomService.API.Domain.Models.Room {
                HotelId = 1,
                Number = "101",
                Type = RoomService.API.Domain.Models.RoomType.Single,
                Price = 150m,
                Description = "Standard single room",
                IsAvailable = true,
                NumberOfBeds = 1,
                Capacity = 1
            },
            new RoomService.API.Domain.Models.Room {
                HotelId = 1,
                Number = "102",
                Type = RoomService.API.Domain.Models.RoomType.Double,
                Price = 180m,
                Description = "Comfort double room",
                IsAvailable = true,
                NumberOfBeds = 1,
                Capacity = 2
            },
            new RoomService.API.Domain.Models.Room {
                HotelId = 1,
                Number = "103",
                Type = RoomService.API.Domain.Models.RoomType.Suite,
                Price = 300m,
                Description = "Luxury suite",
                IsAvailable = true,
                NumberOfBeds = 2,
                Capacity = 4
            },
            // Ibis Centre (hotelId 2)
            new RoomService.API.Domain.Models.Room {
                HotelId = 2,
                Number = "201",
                Type = RoomService.API.Domain.Models.RoomType.Single,
                Price = 90m,
                Description = "Budget single room",
                IsAvailable = true,
                NumberOfBeds = 1,
                Capacity = 1
            },
            new RoomService.API.Domain.Models.Room {
                HotelId = 2,
                Number = "202",
                Type = RoomService.API.Domain.Models.RoomType.Double,
                Price = 120m,
                Description = "Standard double room",
                IsAvailable = true,
                NumberOfBeds = 1,
                Capacity = 2
            },
            new RoomService.API.Domain.Models.Room {
                HotelId = 2,
                Number = "203",
                Type = RoomService.API.Domain.Models.RoomType.Twin,
                Price = 130m,
                Description = "Twin room",
                IsAvailable = true,
                NumberOfBeds = 2,
                Capacity = 2
            }
        );
        db.SaveChanges();
    }
}

app.Run();
