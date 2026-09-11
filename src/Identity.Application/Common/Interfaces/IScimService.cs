using Identity.Domain.Entities;

namespace Identity.Application.Common.Interfaces;

public interface IScimService
{
    Task<string?> SearchUserAsync(string filter, CancellationToken cancellationToken = default);
    Task CreateUserAsync(User user, string? plainPassword = null, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(string scimUserId, CancellationToken cancellationToken = default);
}
