using System.Text;
using Identity.Application.Common.Interfaces;
using Identity.Domain.Contracts;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.Interceptors;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Services;
using Identity.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Settings configuration
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<OAuthSettings>(configuration.GetSection(OAuthSettings.SectionName));
        services.Configure<MinioStorageSettings>(configuration.GetSection(MinioStorageSettings.SectionName));

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

        // Database context
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            options.UseNpgsql(connectionString)
                   .AddInterceptors(interceptor);
        });

        // Redis Connection & Cache Service
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RedisCacheService>>();
            var redisConn = configuration.GetConnectionString("Redis") 
                            ?? configuration["ConnectionStrings:Redis"] 
                            ?? "localhost:6379";
            try
            {
                var options = ConfigurationOptions.Parse(redisConn);
                options.AbortOnConnectFail = false;
                options.ConnectRetry = 3;
                return ConnectionMultiplexer.Connect(options);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not connect to Redis at {RedisConn}. In-memory fallback will be active.", redisConn);
                return null!;
            }
        });
        services.AddSingleton<IRedisCacheService, RedisCacheService>();

        // Repositories & Unit of work
        services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IUserPrivacyRepository, UserPrivacyRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Application Services
        services.AddHttpContextAccessor();
        services.AddHttpClient();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IOAuthService, OAuthService>();
        services.AddScoped<IStorageService, MinioStorageService>();

        // JWT Bearer Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var redisCache = context.HttpContext.RequestServices.GetRequiredService<IRedisCacheService>();
                    var authHeader = context.Request.Headers.Authorization.ToString();
                    var token = authHeader.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

                    if (!string.IsNullOrEmpty(token) && await redisCache.IsTokenBlacklistedAsync(token))
                    {
                        context.Fail("Token đã bị vô hiệu hóa / thu hồi.");
                    }
                }
            };
        });

        return services;
    }
}
