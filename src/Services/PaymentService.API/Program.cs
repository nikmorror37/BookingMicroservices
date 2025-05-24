using PaymentService.API.Infrastructure.Data;
using PaymentService.API.Infrastructure.Clients;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MassTransit;
using PaymentService.API.Consumers;
using BookingMicro.Contracts.Events;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// EF
builder.Services.AddDbContext<PaymentDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("PaymentDb"))
);

//Register client to bank gateway
builder.Services.AddScoped<IPaymentGatewayClient, PaymentGatewayClient>();

//JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

//JWT Auth
builder.Services
  .AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
  })
  .AddJwtBearer(options => {
    options.RequireHttpsMetadata = false;
    options.SaveToken            = true;
    options.TokenValidationParameters = new TokenValidationParameters 
    {
      ValidateIssuer           = true,
      ValidIssuer              = builder.Configuration["Jwt:Issuer"],
      ValidateAudience         = true,
      ValidAudience            = builder.Configuration["Jwt:Audience"],
      ValidateLifetime         = true,
      ValidateIssuerSigningKey = true,
      IssuerSigningKey         = new SymmetricSecurityKey(
                                  Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
      NameClaimType            = JwtRegisteredClaimNames.Sub
    };
  });

// HttpClient to RoomService
builder.Services.AddHttpClient<IRoomServiceClient, RoomServiceClient>(client =>
{
    var roomServiceUrl = builder.Configuration["Services:RoomService"] ?? throw new InvalidOperationException("RoomService URL not configured");
    client.BaseAddress = new Uri(roomServiceUrl);
});

// MassTransit + RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // register a consumer that will process BookingCancelled
    x.AddConsumer<BookingCancelledConsumer>();
    // NEW consumers
    x.AddConsumer<BookingCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var evt = builder.Configuration.GetSection("EventBus");
        cfg.Host(evt["Host"]!, evt["VirtualHost"]!, h =>
        {
            h.Username(evt["Username"]!);
            h.Password(evt["Password"]!);
        });

        // Configure publishing
        cfg.Publish<PaymentCreated>(e => e.ExchangeType = "fanout");

        cfg.ReceiveEndpoint("payment-service-queue", e =>
        {
            // connect consumers
            e.ConfigureConsumer<BookingCancelledConsumer>(context);
            e.ConfigureConsumer<BookingCreatedConsumer>(context);
        });
    });
});
builder.Services.AddMassTransitHostedService();


// Controllers + Swagger
builder.Services.AddControllers()
    .AddJsonOptions(opts => opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
  c.SwaggerDoc("v1", new OpenApiInfo { Title = "PaymentService.API", Version = "v1" });

  // Add schema Bearer into Swagger UI
  c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter format: {token}"
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
if (app.Environment.IsDevelopment()) {
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply EF Core migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.Migrate();
}

app.Run();
