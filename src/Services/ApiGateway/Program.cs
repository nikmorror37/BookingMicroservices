using Yarp.ReverseProxy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;

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

builder.Services.AddHealthChecks();

var app = builder.Build();

// Middleware
// REMOVE app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// in pipeline, just after app.UseAuthorization();
app.UseCors("AllowAll");

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

// before app.Run(), map health checks
app.MapHealthChecks("/health");

app.Run();
