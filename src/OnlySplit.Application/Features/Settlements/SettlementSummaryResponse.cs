namespace OnlySplit.Application.Features.Settlements;

public sealed record SettlementOverviewResponse(
    Guid PayerId,
    string PayerName,
    Guid ReceiverId,
    string ReceiverName,
    decimal Amount
);