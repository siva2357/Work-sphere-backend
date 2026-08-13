using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WorkSphere.API.Data;

using WorkSphere.API.Services;
using WorkSphere.API.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers & Services
builder.Services.AddControllers();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IHrService, HrService>();

// Configure MongoDB with EF Core
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMongoDB(mongoConnectionString ?? "mongodb://localhost:27017/WorkSphereDb", "WorkSphereDb"));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// Configure CORS for Angular Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Angular default port
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed Dummy Data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    if (!dbContext.Announcements.Any())
    {
        dbContext.Announcements.AddRange(
            new WorkSphere.API.Models.Announcement { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Title = "Welcome to Work-Sphere!", Content = "We are excited to launch our new HR portal.", DatePosted = DateTime.UtcNow, AuthorId = "system" },
            new WorkSphere.API.Models.Announcement { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Title = "Q3 Townhall Meeting", Content = "Join us this Friday for the Q3 townhall.", DatePosted = DateTime.UtcNow.AddDays(-2), AuthorId = "system" }
        );
        dbContext.SaveChanges();
    }

    if (!dbContext.FinancialEvents.Any())
    {
        dbContext.FinancialEvents.AddRange(
            new WorkSphere.API.Models.FinancialEvent { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Title = "Payroll Cutoff", Description = "Last day to submit timesheets.", Date = DateTime.UtcNow.AddDays(5) },
            new WorkSphere.API.Models.FinancialEvent { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Title = "Tax Filing Deadline", Description = "Ensure your tax forms are uploaded.", Date = DateTime.UtcNow.AddDays(15) }
        );
        dbContext.SaveChanges();
    }
}

app.Run();
