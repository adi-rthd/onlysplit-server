using OnlySplit.Application.Features.Settlements;
using OnlySplit.Domain.Entities;

namespace OnlySplit.Application.Interfaces;

public interface ISettlementService
{
    Task<IReadOnlyCollection<BalanceResponse>> GetBalancesAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SettlementResponse>> GetPendingSettlementsAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Settlement>> RegenerateForGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SettlementResponse>> GetAllPendingSettlementsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SettlementOverviewResponse>> GetSettlementSummaryAsync(CancellationToken cancellationToken = default);
}
