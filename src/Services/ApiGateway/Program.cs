using Yarp.ReverseProxy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// add config from appsettings.json (ReverseProxy)
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

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

var app = builder.Build();

// Middleware
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();             
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
    });
}
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

app.Run();
