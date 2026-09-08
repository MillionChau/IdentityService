using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs;
using Identity.Application.Features.Auth.Commands.Register;
using Identity.Domain.Contracts;
using Identity.Domain.Entities;
using Moq;
using Xunit;

namespace Identity.UnitTests.Features.Auth;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IPasswordHasher> _hasherMock;
    private readonly Mock<IJwtTokenService> _jwtServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IScimService> _scimServiceMock;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _hasherMock = new Mock<IPasswordHasher>();
        _jwtServiceMock = new Mock<IJwtTokenService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _scimServiceMock = new Mock<IScimService>();

        _handler = new RegisterCommandHandler(
            _userRepoMock.Object,
            _hasherMock.Object,
            _jwtServiceMock.Object,
            _unitOfWorkMock.Object,
            _scimServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithNewUser_ShouldCreateUserAndReturnTokens()
    {
        _userRepoMock.Setup(r => r.ExistsByUserNameOrEmailAsync("newdev", "newdev@devradar.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _hasherMock.Setup(h => h.HashPassword("MySecretPass"))
            .Returns("hashed_secret_pass");
        _jwtServiceMock.Setup(j => j.GenerateTokensAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResponseDto { AccessToken = "new_access", RefreshToken = "new_refresh" });

        var command = new RegisterCommand
        {
            UserName = "newdev",
            Email = "newdev@devradar.io",
            Password = "MySecretPass",
            FirstName = "New",
            LastName = "Developer"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("new_access", result.AccessToken);
        _userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u => u.UserName == "newdev" && u.Email == "newdev@devradar.io"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ShouldThrowBadRequestException()
    {
        _userRepoMock.Setup(r => r.ExistsByUserNameOrEmailAsync("existing", "existing@radar.io", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RegisterCommand
        {
            UserName = "existing",
            Email = "existing@radar.io",
            Password = "password123"
        };

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
