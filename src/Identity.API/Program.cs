using Identity.API.Middleware;
using Identity.Application;
using Identity.Infrastructure;
using Microsoft.OpenApi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Clean Architecture Layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// CORS for DevRadar Web / Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Request logging: đặt ĐẦU pipeline để log mọi request kèm thời gian phản hồi
app.UseMiddleware<RequestLoggingMiddleware>();

// Custom Global Exception Handler Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure HTTP Pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DevRadar Identity API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/healthz", () => Results.Ok(new { status = "Healthy", service = "IdentityService", timestamp = DateTime.UtcNow }));

// Auto-migrate database on startup (cần thiết khi chạy trong Docker container)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Identity.Infrastructure.Persistence.IdentityDbContext>();
    db.Database.Migrate();
}

app.Run();

public partial class Program { }
