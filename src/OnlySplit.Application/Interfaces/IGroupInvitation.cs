using OnlySplit.Application.Features.GroupInvitation;

namespace OnlySplit.Application.Interfaces;

public interface IGroupInvitation
{
    Task SendInvitationAsync(CreateGroupInvitationRequest request, CancellationToken cancellationToken = default);
}