using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs;
using Identity.Application.Features.Auth.Commands.Login;
using Identity.Domain.Contracts;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Moq;
using Xunit;

namespace Identity.UnitTests.Features.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IPasswordHasher> _hasherMock;
    private readonly Mock<IJwtTokenService> _jwtServiceMock;
    private readonly Mock<IPermissionRepository> _permRepoMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _hasherMock = new Mock<IPasswordHasher>();
        _jwtServiceMock = new Mock<IJwtTokenService>();
        _permRepoMock = new Mock<IPermissionRepository>();

        _handler = new LoginCommandHandler(
            _userRepoMock.Object,
            _hasherMock.Object,
            _jwtServiceMock.Object,
            _permRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnAuthResponse()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "devuser",
            Email = "devuser@radar.io",
            PasswordHash = "valid_hash",
            Status = UserStatus.Active
        };

        _userRepoMock.Setup(r => r.GetByUserNameAsync("devuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepoMock.Setup(r => r.GetUserWithRolesAndPermissionsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasherMock.Setup(h => h.VerifyPassword("Secret123", "valid_hash"))
            .Returns(true);
        _permRepoMock.Setup(p => p.GetPermissionsByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Permission>());
        _jwtServiceMock.Setup(j => j.GenerateTokensAsync(user, It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResponseDto { AccessToken = "access_token_123", RefreshToken = "refresh_token_123" });

        var command = new LoginCommand { UserNameOrEmail = "devuser", Password = "Secret123" };
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("access_token_123", result.AccessToken);
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ShouldThrowBadRequestException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "devuser",
            PasswordHash = "valid_hash",
            Status = UserStatus.Active
        };

        _userRepoMock.Setup(r => r.GetByUserNameAsync("devuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasherMock.Setup(h => h.VerifyPassword("WrongPassword", "valid_hash"))
            .Returns(false);

        var command = new LoginCommand { UserNameOrEmail = "devuser", Password = "WrongPassword" };

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
