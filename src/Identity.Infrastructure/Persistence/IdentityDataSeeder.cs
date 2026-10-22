using Identity.Application.Common.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Persistence;

public static class IdentityDataSeeder
{
    public static async Task SeedAsync(IdentityDbContext context, IPasswordHasher passwordHasher, ILogger logger)
    {
        try
        {
            // 1. Roles
            var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var modRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var memberRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

            if (!await context.Roles.AnyAsync())
            {
                logger.LogInformation("Seeding default roles (Admin, Moderator, Member)...");
                var roles = new List<Role>
                {
                    new()
                    {
                        Id = adminRoleId,
                        Name = "Admin",
                        Description = "Toàn quyền quản trị hệ thống DevRadar",
                        IsDefault = false,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = modRoleId,
                        Name = "Moderator",
                        Description = "Điều phối viên kiểm duyệt nội dung cộng đồng",
                        IsDefault = false,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = memberRoleId,
                        Name = "Member",
                        Description = "Thành viên cộng đồng lập trình viên",
                        IsDefault = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }

            // 2. Users
            var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var devUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var postmanUserId = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var modUserId = Guid.Parse("00000000-0000-0000-0000-000000000004");

            if (!await context.Users.AnyAsync(u => u.Email == "admin@devradar.io" || u.UserName == "admin"))
            {
                logger.LogInformation("Seeding default users (admin, chautantan, postman_user, moderator)...");

                var defaultPasswordHash = passwordHasher.HashPassword("Password123!@#");

                var adminUser = new User
                {
                    Id = adminUserId,
                    UserName = "admin",
                    Email = "admin@devradar.io",
                    FirstName = "System",
                    LastName = "Administrator",
                    FullName = "System Administrator",
                    AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=100&h=100&fit=crop&crop=faces",
                    Status = UserStatus.Active,
                    PrimaryProvider = AuthProvider.Local,
                    PasswordHash = defaultPasswordHash,
                    CreatedAt = DateTime.UtcNow
                };

                var devUser = new User
                {
                    Id = devUserId,
                    UserName = "chautantan",
                    Email = "chautantan@devradar.io",
                    FirstName = "Tấn",
                    LastName = "Châu Hoàng",
                    FullName = "Châu Hoàng Tấn",
                    AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=100&h=100&fit=crop&crop=faces",
                    Address = "Ho Chi Minh City, Vietnam",
                    PhoneNumber = "0901234567",
                    Status = UserStatus.Active,
                    PrimaryProvider = AuthProvider.Local,
                    PasswordHash = defaultPasswordHash,
                    CreatedAt = DateTime.UtcNow
                };

                var postmanUser = new User
                {
                    Id = postmanUserId,
                    UserName = "postman_user",
                    Email = "postman@devradar.io",
                    FirstName = "Postman",
                    LastName = "Tester",
                    FullName = "Postman Tester",
                    Status = UserStatus.Active,
                    PrimaryProvider = AuthProvider.Local,
                    PasswordHash = defaultPasswordHash,
                    CreatedAt = DateTime.UtcNow
                };

                var modUser = new User
                {
                    Id = modUserId,
                    UserName = "moderator",
                    Email = "moderator@devradar.io",
                    FirstName = "Mod",
                    LastName = "DevRadar",
                    FullName = "DevRadar Moderator",
                    AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=100&h=100&fit=crop&crop=faces",
                    Status = UserStatus.Active,
                    PrimaryProvider = AuthProvider.Local,
                    PasswordHash = defaultPasswordHash,
                    CreatedAt = DateTime.UtcNow
                };

                await context.Users.AddRangeAsync(adminUser, devUser, postmanUser, modUser);
                await context.SaveChangesAsync();

                // Assign User Roles
                var userRoles = new List<UserRole>
                {
                    new() { UserId = adminUserId, RoleId = adminRoleId, CreatedAt = DateTime.UtcNow },
                    new() { UserId = devUserId, RoleId = memberRoleId, CreatedAt = DateTime.UtcNow },
                    new() { UserId = postmanUserId, RoleId = adminRoleId, CreatedAt = DateTime.UtcNow },
                    new() { UserId = modUserId, RoleId = modRoleId, CreatedAt = DateTime.UtcNow }
                };

                await context.UserRoles.AddRangeAsync(userRoles);

                // Add Privacy Settings
                var privacySettings = new List<UserPrivacySetting>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        UserId = adminUserId,
                        IsProfilePublic = true,
                        ShowEmail = true,
                        ShowPhoneNumber = false,
                        ShowGitHubStats = true,
                        ShowActivityStats = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        UserId = devUserId,
                        IsProfilePublic = true,
                        ShowEmail = true,
                        ShowPhoneNumber = false,
                        ShowGitHubStats = true,
                        ShowActivityStats = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        UserId = postmanUserId,
                        IsProfilePublic = true,
                        ShowEmail = true,
                        ShowPhoneNumber = false,
                        ShowGitHubStats = true,
                        ShowActivityStats = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await context.UserPrivacySettings.AddRangeAsync(privacySettings);
                await context.SaveChangesAsync();

                logger.LogInformation("Successfully seeded default identity users and privacy settings.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while seeding default Identity data.");
        }
    }
}
