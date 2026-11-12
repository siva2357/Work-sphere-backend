using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WorkSphere.API.Constants;
using WorkSphere.API.Data;
using WorkSphere.API.Services;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Controllers & Services
builder.Services.AddControllers();
builder.Services.AddScoped<IPermissionService, PermissionService>();

// PostgreSQL + Entity Framework Core
var postgresConnectionString =
    Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("PostgreSQL")
    ?? throw new InvalidOperationException(
        "PostgreSQL connection string is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresConnectionString));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");

var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? jwtSettings["Key"]
    ?? throw new InvalidOperationException("JWT key is missing.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT key must contain at least 32 bytes.");
}

builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey))
            };
    });

builder.Services.AddAuthorization();

// Angular CORS
var frontendUrl =
    Environment.GetEnvironmentVariable("FRONTEND_URL")
    ?? "http://localhost:4200";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Seed 17-Table Organizational Hierarchy & Centralized Permission Engine
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

// HTTP Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Actual PostgreSQL health check
app.MapGet(ApiConstants.Health, async (AppDbContext db) =>
{
    try
    {
        var connected = await db.Database.CanConnectAsync();

        return connected
            ? Results.Ok(new
            {
                status = "Healthy",
                database = "PostgreSQL",
                connected = true,
                timestamp = DateTime.UtcNow
            })
            : Results.Problem(
                title: "Database connection failed",
                statusCode: 503);
    }
    catch
    {
        return Results.Problem(
            title: "Database connection failed",
            statusCode: 503);
    }
});

app.Run();
