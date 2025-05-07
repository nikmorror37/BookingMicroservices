using BookingService.API.Infrastructure.Data;
using BookingService.API.Infrastructure.Clients;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

//JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); //new
var builder = WebApplication.CreateBuilder(args);

// EF
builder.Services.AddDbContext<BookingDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("BookingDb"))
);

// HttpClient for RoomService
builder.Services.AddHttpClient<IRoomServiceClient, RoomServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:RoomService"]);
});

// HttpClient for CatalogService
builder.Services.AddHttpClient<IHotelServiceClient, HotelServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:CatalogService"]);
});

// HttpClient for PaymentService
builder.Services.AddHttpClient<IPaymentServiceClient, PaymentServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:PaymentService"]);
});

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
            Console.WriteLine("JWT Validated for: " + ctx.Principal.Identity.Name);
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
            // get ClaimType.Name from "sub"
            //NameClaimType            = JwtRegisteredClaimNames.Sub
            NameClaimType            = ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddAuthorization();

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

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
