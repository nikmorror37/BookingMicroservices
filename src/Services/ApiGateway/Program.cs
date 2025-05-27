using Yarp.ReverseProxy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// add config from appsettings.json (ReverseProxy)
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// if running inside container we may have Docker config
builder.Configuration.AddJsonFile("appsettings.Docker.json", optional: true, reloadOnChange: true);

// JWT-auth
builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
      options.RequireHttpsMetadata = false;
      options.SaveToken = true;
      options.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer           = true,
          ValidIssuer              = builder.Configuration["Jwt:Issuer"],
          ValidateAudience         = true,
          ValidAudience            = builder.Configuration["Jwt:Audience"],
          ValidateLifetime         = true,
          ValidateIssuerSigningKey = true,
          IssuerSigningKey         = new SymmetricSecurityKey(
                                        Encoding.UTF8.GetBytes(
                                            builder.Configuration["Jwt:Key"]!
                                        )
                                    ),
          NameClaimType            = JwtRegisteredClaimNames.Sub
      };
  });

// Registr YARP
builder.Services.AddReverseProxy()
       .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo {
      Title   = "API Gateway",
      Version = "v1"
    });
});

// after builder.Configuration.AddJsonFile("appsettings.json" ... ) add CORS & HealthChecks
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p => p
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

//builder.Services.AddHealthChecks();

// Enhanced HealthChecks to check all services
builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri("http://catalogservice/health"), 
        name: "catalog-service", 
        tags: new[] { "service", "catalog" })
    .AddUrlGroup(new Uri("http://roomservice/health"), 
        name: "room-service", 
        tags: new[] { "service", "room" })
    .AddUrlGroup(new Uri("http://bookingservice/health"), 
        name: "booking-service", 
        tags: new[] { "service", "booking" })
    .AddUrlGroup(new Uri("http://paymentservice/health"), 
        name: "payment-service", 
        tags: new[] { "service", "payment" })
    .AddUrlGroup(new Uri("http://identityservice/health"), 
        name: "identity-service", 
        tags: new[] { "service", "identity" });

// Rate Limiting:
//100 requests per minute per user (or IP if not authorized)
//Automatic limit recovery every minute
//429 Too Many Requests when limit is exceeded
//Protection against DDoS and API overload.
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
    
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please try again later.", cancellationToken: token);
    };
});

var app = builder.Build();

// Middleware
// REMOVE app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// in pipeline, just after app.UseAuthorization();
app.UseCors("AllowAll");

// Rate Limiting middleware
app.UseRateLimiter();

// Accept X-Forwarded-* when running behind reverse proxies (nginx, docker networks, etc.)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();             
//     app.UseSwaggerUI(c =>
//     {
//         c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
//     });
// }

app.UseSwagger();
app.UseSwaggerUI();

// put proxy on whole pipeline
app.MapReverseProxy(proxyPipeline =>
{
    // If needed, can add interceptors (such as logins):
    proxyPipeline.Use(async (context, next) =>
    {
        // here context.User already filled from JWT
        await next();
    });
});

// health checks
//app.MapHealthChecks("/health");

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration,
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                duration = x.Value.Duration,
                description = x.Value.Description,
                exception = x.Value.Exception?.Message
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.Run();
