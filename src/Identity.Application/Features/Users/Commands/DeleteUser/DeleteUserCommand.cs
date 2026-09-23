using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Domain.Contracts;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Features.Users.Commands.DeleteUser;

public record DeleteUserCommand(Guid Id) : IRequest<bool>;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScimService _scimService;

    public DeleteUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IScimService scimService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _scimService = scimService;
    }

    public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("User", request.Id);

        user.Status = UserStatus.Deleted;
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(user.ExternalId))
        {
            try
            {
                await _scimService.DeleteUserAsync(user.ExternalId, cancellationToken);
            }
            catch
            {
                // Non-blocking
            }
        }

        return true;
    }
}
