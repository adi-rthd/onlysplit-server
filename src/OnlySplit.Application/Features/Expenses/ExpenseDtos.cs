namespace OnlySplit.Application.Features.Expenses;

public sealed record SplitInputDto(
    Guid UserId,
    decimal? Amount,
    decimal? Percentage);

public sealed record CreateExpenseRequest(
    Guid GroupId,
    Guid? PaidBy,
    string Title,
    string? Description,
    decimal Amount,
    string Category,
    string SplitType,
    IReadOnlyCollection<SplitInputDto> Splits);

public sealed record UpdateExpenseRequest(
    Guid? PaidBy,
    string? Title,
    string? Description,
    decimal? Amount,
    string? Category,
    string? SplitType,
    IReadOnlyCollection<SplitInputDto>? Splits);

public sealed record ExpenseSplitResponse(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    decimal AmountOwed,
    string SplitType,
    string Status);

public sealed record ExpenseResponse(
    Guid Id,
    Guid GroupId,
    Guid PaidBy,
    string PaidByName,
    string Title,
    string? Description,
    decimal Amount,
    string Category,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<ExpenseSplitResponse> Splits);
