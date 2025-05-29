using CatalogService.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("CatalogDb") 
    ?? throw new InvalidOperationException("Connection string 'CatalogDb' not found.");

builder.Services.AddDbContext<CatalogDbContext>(opts =>
    opts.UseSqlServer(connectionString));
// builder.Services.AddDbContext<CatalogDbContext>(opts =>
//     opts.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDb")));

// HealthChecks
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "CatalogService.API", Version = "v1" });

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
            IssuerSigningKey         = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            NameClaimType            = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add static files middleware 
var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "Images");
if (!Directory.Exists(imagesPath))
{
    Directory.CreateDirectory(imagesPath);
}

//app.UseStaticFiles(); // Default wwwroot
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagesPath),
    RequestPath = "/images"
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// endpoint for health checks
app.MapHealthChecks("/health");

// Apply EF Core migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    db.Database.Migrate();

    // Seed some hotels for Warsaw
    if (!db.Hotels.Any())
    {
        db.Hotels.AddRange(
            new CatalogService.API.Domain.Models.Hotel
            {
                Name = "Hilton",
                Address = "Grzybowska 63, 00-844",
                City = "Warsaw",
                Country = "Poland",
                Stars = 4,
                DistanceFromCenter = 1.1,
                ImageUrl = "/images/HiltonWarsaw/hilton_warsaw.jpg",
                Description = "Hilton Warsaw City Hotel offers 4-star accommodation located in Warsaw's busy financial district within walking distance of shops, museums and a lively dining and entertainment scene. The historic Old Town is a 30-minute walk from the hotel and Warsaw Frederic Chopin Airport is a 20-minute drive away."
            },
            new CatalogService.API.Domain.Models.Hotel
            {
                Name = "Raffles Europejski",
                Address = "Krakowskie Przedmiescie 13, 00-071",
                City = "Warsaw",
                Country = "Poland",
                Stars = 5,
                DistanceFromCenter = 1.6,
                ImageUrl = "/images/RafflesWarsaw/raffles_warsaw.jpg",
                Description = "Raffles Europejski Warsaw is a luxury hotel located in the heart of Warsaw, Poland. It is known for its elegant design, exceptional service, and rich history. The hotel features luxurious accommodations, fine dining options, a spa, and various amenities to ensure a comfortable and memorable stay for its guests."
            },
            new CatalogService.API.Domain.Models.Hotel
            {
                Name = "Westin",
                Address = "al. Jana Pawla II 21, 00-854",
                City = "Warsaw",
                Country = "Poland",
                Stars = 5,
                DistanceFromCenter = 0.7,
                ImageUrl = "/images/WestinWarsaw/westin_warsaw.jpg",
                Description = "The Westin Warsaw is known for its modern design, upscale amenities, and convenient location. The hotel offers comfortable accommodations, a fitness center, a spa, and various dining options. It is a popular choice for both business and leisure travelers visiting the city."
            },
            new CatalogService.API.Domain.Models.Hotel
            {
                Name = "ibis Styles Centrum",
                Address = "Zagorna 1A, 00-441",
                City = "Warsaw",
                Country = "Poland",
                Stars = 3,
                DistanceFromCenter = 1.9,
                ImageUrl = "/images/IbisStylesCentrumWarsaw/ibis_styles_centrum_warsaw.jpg",
                Description = "ibis Styles Centrum Warsaw is a budget-friendly hotel. It is known for its modern design, comfortable accommodations, and convenient amenities. The hotel offers a range of services, including free Wi-Fi."
            },
            new CatalogService.API.Domain.Models.Hotel
            {
                Name = "Mercure Grand",
                Address = "Krucza 28, 00-522",
                City = "Warsaw",
                Country = "Poland",
                Stars = 4,
                DistanceFromCenter = 0.5,
                ImageUrl = "/images/MercureGrandWarsaw/mercure_grand_warsaw.jpg",
                Description = "Mercure Warszawa Grand is perfectly located in the centre of Warsaw. The hotel features modern rooms, restaurant, fitness center and conference facilities."
            }
        );
        db.SaveChanges();
    }
}

app.Run();
