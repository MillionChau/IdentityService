using AutoMapper;
using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.DTOs;
using Identity.Application.Features.Privacy.Commands.UpdateUserPrivacy;
using Identity.Application.Features.Privacy.Queries.GetMyPrivacySettings;
using Identity.Application.Features.Users.Queries.GetUserPublicProfile;
using Identity.Domain.Contracts;
using Identity.Domain.Entities;
using Moq;
using Xunit;

namespace Identity.UnitTests.Features.Privacy;

public class PrivacyCommandHandlerTests
{
    private readonly Mock<IUserPrivacyRepository> _privacyRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMapper _mapper;

    public PrivacyCommandHandlerTests()
    {
        _privacyRepoMock = new Mock<IUserPrivacyRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _currentUserMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<UserPrivacySetting, UserPrivacySettingDto>();
            cfg.CreateMap<User, PublicUserProfileDto>()
                .ForMember(d => d.Roles, opt => opt.MapFrom(s => s.UserRoles.Select(ur => ur.Role.Name)));
        });
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task GetMyPrivacySettings_Unauthenticated_ShouldThrowUnauthorized()
    {
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(false);
        var handler = new GetMyPrivacySettingsQueryHandler(
            _privacyRepoMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _mapper);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new GetMyPrivacySettingsQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePrivacy_WhenAuthenticated_ShouldSaveSettings()
    {
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.UserId).Returns(userId);

        var existing = new UserPrivacySetting { UserId = userId, ShowEmail = false };
        _privacyRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = new UpdateUserPrivacyCommandHandler(
            _privacyRepoMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _mapper);

        var cmd = new UpdateUserPrivacyCommand
        {
            IsProfilePublic = true,
            ShowEmail = true,
            ShowPhoneNumber = false,
            ShowGitHubStats = true,
            ShowActivityStats = true
        };

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.ShowEmail);
        _privacyRepoMock.Verify(r => r.UpdateAsync(It.Is<UserPrivacySetting>(p => p.ShowEmail == true), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserPublicProfile_WhenOtherUserViewsAndEmailHidden_ShouldHideEmail()
    {
        var targetUserId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.UserId).Returns(viewerId);
        _currentUserMock.Setup(c => c.Roles).Returns(new List<string> { "User" });

        var targetUser = new User
        {
            Id = targetUserId,
            UserName = "targetdev",
            Email = "secret@devradar.io",
            PhoneNumber = "0987654321",
            PrivacySetting = new UserPrivacySetting
            {
                UserId = targetUserId,
                IsProfilePublic = true,
                ShowEmail = false,
                ShowPhoneNumber = false
            }
        };

        _userRepoMock.Setup(r => r.GetUserWithRolesAndPermissionsAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var handler = new GetUserPublicProfileQueryHandler(
            _userRepoMock.Object, _currentUserMock.Object, _mapper);

        var result = await handler.Handle(new GetUserPublicProfileQuery(targetUserId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.Email);
        Assert.Null(result.PhoneNumber);
        Assert.Equal("targetdev", result.UserName);
    }

    [Fact]
    public async Task GetUserPublicProfile_WhenOwnerViewsOwnProfile_ShouldShowAll()
    {
        var myId = Guid.NewGuid();

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.UserId).Returns(myId);
        _currentUserMock.Setup(c => c.Roles).Returns(new List<string> { "User" });

        var me = new User
        {
            Id = myId,
            UserName = "myuser",
            Email = "my@devradar.io",
            PhoneNumber = "0123456789",
            PrivacySetting = new UserPrivacySetting
            {
                UserId = myId,
                IsProfilePublic = true,
                ShowEmail = false
            }
        };

        _userRepoMock.Setup(r => r.GetUserWithRolesAndPermissionsAsync(myId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(me);

        var handler = new GetUserPublicProfileQueryHandler(
            _userRepoMock.Object, _currentUserMock.Object, _mapper);

        var result = await handler.Handle(new GetUserPublicProfileQuery(myId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("my@devradar.io", result.Email);
        Assert.Equal("0123456789", result.PhoneNumber);
    }
}
