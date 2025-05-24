using CatalogService.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDb")));
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

app.UseStaticFiles(); // Default wwwroot
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagesPath),
    RequestPath = "/images"
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Apply EF Core migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    db.Database.Migrate();

    // Seed default hotels
    if(!db.Hotels.Any())
    {
        db.Hotels.AddRange(
            new CatalogService.API.Domain.Models.Hotel {
                Name = "Hilton Midtown",
                Address = "1335 6th Ave",
                City = "New York",
                Country = "USA",
                Stars = 5,
                DistanceFromCenter = 0.9,
                ImageUrl = "/images/hilton-midtown.jpg",
                Description = "Experience luxury in the heart of Manhattan at the Hilton Midtown. Featuring spacious rooms with stunning city views, our hotel offers premium amenities including a rooftop pool, gourmet dining options, and a state-of-the-art fitness center. Minutes away from Central Park, Broadway theaters, and Fifth Avenue shopping."
            },
            new CatalogService.API.Domain.Models.Hotel {
                Name = "Ibis Centre",
                Address = "7 Rue de Temple",
                City = "Paris",
                Country = "France",
                Stars = 3,
                DistanceFromCenter = 1.2,
                ImageUrl = "/images/ibis-paris.jpg",
                Description = "Enjoy comfortable and affordable accommodations at Ibis Centre Paris. Our cozy rooms provide all essential amenities for a pleasant stay in the City of Light. Conveniently located just a short walk from major attractions like Notre Dame Cathedral and the Louvre Museum. Continental breakfast available daily."
            }
        );
        db.SaveChanges();
    }
}

app.Run();
