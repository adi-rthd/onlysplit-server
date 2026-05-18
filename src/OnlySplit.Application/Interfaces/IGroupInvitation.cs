using OnlySplit.Application.Features.GroupInvitation;

namespace OnlySplit.Application.Interfaces;

public interface IGroupInvitation
{
    Task SendInvitationAsync(
        CreateGroupInvitationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GroupInvitationResponse>>
        GetMyInvitationsAsync(
            CancellationToken cancellationToken = default);

    Task AcceptInvitationAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default);

    Task RejectInvitationAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default);
}