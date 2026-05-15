using OnlySplit.Application.Features.Expenses;

namespace OnlySplit.Application.Interfaces;

public interface IExpenseService
{
    Task<ExpenseResponse> CreateAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ExpenseResponse>> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<ExpenseResponse> UpdateAsync(Guid id, UpdateExpenseRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
