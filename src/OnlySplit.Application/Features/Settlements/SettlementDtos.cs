namespace OnlySplit.Application.Features.Settlements;

public sealed record BalanceResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    decimal NetBalance);

public sealed record SettlementResponse(
    Guid Id,
    Guid? GroupId,
    Guid PayerId,
    string PayerName,
    Guid ReceiverId,
    string ReceiverName,
    decimal Amount,
    string Status,
    DateTimeOffset CreatedAt);
