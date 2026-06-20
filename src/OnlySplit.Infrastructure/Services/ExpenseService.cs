using Microsoft.EntityFrameworkCore;
using OnlySplit.Domain.Constants;
using OnlySplit.Infrastructure.Database;
using OnlySplit.Application.Features.Expenses;
using OnlySplit.Domain.Entities;
using OnlySplit.Domain.Exceptions;
using OnlySplit.Application.Interfaces;
using OnlySplit.Domain.Utils;

namespace OnlySplit.Infrastructure.Services;

public sealed class ExpenseService(
    OnlySplitDbContext context,
    ICurrentUserService currentUser,
    ISettlementService settlementService,
    IActivityService activityService,
    IRealtimeNotifier realtimeNotifier) : IExpenseService
{
    public async Task<ExpenseResponse> CreateAsync(
      CreateExpenseRequest request,
      CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        ExpenseResponse? response = null;
        Guid groupId = Guid.Empty;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var group = await LoadGroupForMutationAsync(request.GroupId, cancellationToken);

            EnsureMember(group, currentUser.UserId);

            var paidBy = request.PaidBy ?? currentUser.UserId;

            EnsureMember(group, paidBy);

            var splitType = NormalizeSplitType(request.SplitType);

            var splits = BuildSplits(
                group,
                request.Amount,
                splitType,
                request.Splits);

            var expense = new Expense
            {
                GroupId = group.Id,
                PaidBy = paidBy,
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim(),
                Amount = MoneyMath.Round(request.Amount),
                Category = request.Category.Trim().ToLowerInvariant(),
                Splits = splits
            };

            context.Expenses.Add(expense);

            await context.SaveChangesAsync(cancellationToken);

            await settlementService.RegenerateForGroupAsync(
                group.Id,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            response = await GetExpenseResponseAsync(
                expense.Id,
                cancellationToken);

            groupId = group.Id;

            await activityService.LogAsync(
                currentUser.UserId,
                ActivityTypes.ExpenseCreated,
                new
                {
                    expense.Id,
                    expense.GroupId,
                    expense.Amount
                },
                cancellationToken);
        });

        await realtimeNotifier.SendGroupAsync(
            groupId,
            "ExpenseAdded",
            response!,
            cancellationToken);

        await BroadcastBalanceRefreshAsync(
            groupId,
            cancellationToken);

        return response!;
    }
    public async Task<IReadOnlyCollection<ExpenseResponse>> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        await EnsureGroupAccessAsync(groupId, cancellationToken);

        var expenses = await context.Expenses
            .AsNoTracking()
            .Include(expense => expense.PaidByUser)
            .Include(expense => expense.Splits)
                .ThenInclude(split => split.User)
            .Where(expense => expense.GroupId == groupId)
            .OrderByDescending(expense => expense.CreatedAt)
            .ToListAsync(cancellationToken);

        return expenses.Select(ToResponse).ToArray();
    }

    public async Task<ExpenseResponse> UpdateAsync(Guid id, UpdateExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        ExpenseResponse? response = null;
        Guid groupId = Guid.Empty;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var expense = await context.Expenses
                .Include(candidate => candidate.Group)
                    .ThenInclude(group => group!.Members)
                .Include(candidate => candidate.Splits)
                .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
                ?? throw new NotFoundException("Expense was not found.");

            EnsureMember(expense.Group!, currentUser.UserId);
            EnsureCanMutateExpense(expense);

            if (request.PaidBy.HasValue)
            {
                EnsureMember(expense.Group!, request.PaidBy.Value);
                expense.PaidBy = request.PaidBy.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                expense.Title = request.Title.Trim();
            }

            if (request.Description is not null)
            {
                expense.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            }

            if (request.Amount.HasValue)
            {
                expense.Amount = MoneyMath.Round(request.Amount.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                expense.Category = request.Category.Trim().ToLowerInvariant();
            }

            var shouldRecalculateSplits = request.Amount.HasValue || request.SplitType is not null || request.Splits is not null;
            if (shouldRecalculateSplits)
            {
                var splitType = NormalizeSplitType(request.SplitType ?? expense.Splits.FirstOrDefault()?.SplitType ?? SplitTypes.Equal);
                var splitInputs = request.Splits;

                if (splitInputs is null && splitType != SplitTypes.Equal)
                {
                    throw new AppException("Updated split details are required when changing non-equal split amounts.");
                }

                // Detach old splits from change tracker and delete via raw SQL
                foreach (var oldSplit in expense.Splits.ToList())
                {
                    context.Entry(oldSplit).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                expense.Splits.Clear();

                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM expense_splits WHERE \"ExpenseId\" = {expense.Id}", cancellationToken);

                // Build and add new splits as fresh entities
                var newSplits = BuildSplits(expense.Group!, expense.Amount, splitType, splitInputs ?? Array.Empty<SplitInputDto>());
                foreach (var split in newSplits)
                {
                    split.ExpenseId = expense.Id;
                }
                context.ExpenseSplits.AddRange(newSplits);
            }

            await context.SaveChangesAsync(cancellationToken);
            await settlementService.RegenerateForGroupAsync(expense.GroupId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            response = await GetExpenseResponseAsync(expense.Id, cancellationToken);
            groupId = expense.GroupId;

            await activityService.LogAsync(currentUser.UserId, ActivityTypes.ExpenseUpdated, new { expense.Id, expense.GroupId, expense.Amount }, cancellationToken);
        });

        await realtimeNotifier.SendGroupAsync(groupId, "ExpenseUpdated", response!, cancellationToken);
        await BroadcastBalanceRefreshAsync(groupId, cancellationToken);

        return response!;
    }

    public async Task DeleteAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        var expense = await context.Expenses
            .AsSplitQuery()
            .Include(expense => expense.Group)
                .ThenInclude(group => group!.Members)
            .Include(expense => expense.Splits)
            .FirstOrDefaultAsync(expense => expense.Id == id, cancellationToken)
            ?? throw new NotFoundException("Expense was not found.");

        EnsureMember(expense.Group!, currentUser.UserId);

        EnsureCanMutateExpense(expense);

        var groupId = expense.GroupId;

        context.Expenses.Remove(expense);

        await context.SaveChangesAsync(cancellationToken);

        await settlementService.RegenerateForGroupAsync(groupId, cancellationToken);

        await activityService.LogAsync(currentUser.UserId,
            ActivityTypes.ExpenseDeleted,
            new
            {
                ExpenseId = id,
                GroupId = groupId
            },
            cancellationToken);

        await realtimeNotifier.SendGroupAsync(groupId,
            "ExpenseDeleted",
            new
            {
                ExpenseId = id,
                GroupId = groupId
            },
            cancellationToken);

        await BroadcastBalanceRefreshAsync(groupId, cancellationToken);
    }

    private async Task<Group> LoadGroupForMutationAsync(Guid groupId, CancellationToken cancellationToken) =>
        await context.Groups
            .Include(group => group.Members)
            .FirstOrDefaultAsync(group => group.Id == groupId, cancellationToken)
        ?? throw new NotFoundException("Group was not found.");

    private async Task EnsureGroupAccessAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var exists = await context.Groups.AnyAsync(group => group.Id == groupId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("Group was not found.");
        }

        var isMember = await context.GroupMembers
            .AnyAsync(member => member.GroupId == groupId && member.UserId == currentUser.UserId, cancellationToken);

        if (!isMember)
        {
            throw new ForbiddenException("You are not a member of this group.");
        }
    }

    private static List<ExpenseSplit> BuildSplits(
        Group group,
        decimal amount,
        string splitType,
        IReadOnlyCollection<SplitInputDto> requestedSplits)
    {
        var memberIds = group.Members.Select(member => member.UserId).ToHashSet();

        if (splitType == SplitTypes.Equal)
        {
            var selectedUserIds = requestedSplits.Count > 0
                ? requestedSplits.Select(split => split.UserId).Distinct().ToArray()
                : memberIds.ToArray();

            EnsureAllSplitUsersAreMembers(selectedUserIds, memberIds);
            return BuildEqualSplits(amount, selectedUserIds);
        }

        var duplicateUserId = requestedSplits
            .GroupBy(split => split.UserId)
            .FirstOrDefault(grouping => grouping.Count() > 1)
            ?.Key;

        if (duplicateUserId.HasValue)
        {
            throw new AppException($"Duplicate split entry found for user {duplicateUserId.Value}.");
        }

        EnsureAllSplitUsersAreMembers(requestedSplits.Select(split => split.UserId), memberIds);

        return splitType switch
        {
            SplitTypes.Percentage => BuildPercentageSplits(amount, requestedSplits),
            SplitTypes.Exact => BuildExactSplits(amount, requestedSplits),
            _ => throw new AppException("Unsupported split type.")
        };
    }

    private static List<ExpenseSplit> BuildEqualSplits(decimal amount, IReadOnlyCollection<Guid> userIds)
    {
        if (userIds.Count == 0)
        {
            throw new AppException("At least one split participant is required.");
        }

        var splits = new List<ExpenseSplit>();
        var baseAmount = MoneyMath.Round(amount / userIds.Count);
        var remaining = MoneyMath.Round(amount);
        var orderedUserIds = userIds.OrderBy(id => id).ToArray();

        for (var index = 0; index < orderedUserIds.Length; index++)
        {
            var owed = index == orderedUserIds.Length - 1 ? remaining : baseAmount;
            remaining = MoneyMath.Round(remaining - owed);
            splits.Add(new ExpenseSplit
            {
                UserId = orderedUserIds[index],
                AmountOwed = MoneyMath.Round(owed),
                SplitType = SplitTypes.Equal
            });
        }

        return splits;
    }

    private static List<ExpenseSplit> BuildPercentageSplits(decimal amount, IReadOnlyCollection<SplitInputDto> requestedSplits)
    {
        if (requestedSplits.Any(split => !split.Percentage.HasValue))
        {
            throw new AppException("Percentage is required for each percentage split.");
        }

        var totalPercentage = requestedSplits.Sum(split => split.Percentage!.Value);
        if (totalPercentage != 100)
        {
            throw new AppException("Percentage splits must total 100.");
        }

        var splits = new List<ExpenseSplit>();
        var remaining = MoneyMath.Round(amount);
        var orderedSplits = requestedSplits.OrderBy(split => split.UserId).ToArray();

        for (var index = 0; index < orderedSplits.Length; index++)
        {
            var requestedSplit = orderedSplits[index];
            var owed = index == orderedSplits.Length - 1
                ? remaining
                : MoneyMath.Round(amount * requestedSplit.Percentage!.Value / 100);

            remaining = MoneyMath.Round(remaining - owed);
            splits.Add(new ExpenseSplit
            {
                UserId = requestedSplit.UserId,
                AmountOwed = MoneyMath.Round(owed),
                SplitType = SplitTypes.Percentage
            });
        }

        return splits;
    }

    private static List<ExpenseSplit> BuildExactSplits(decimal amount, IReadOnlyCollection<SplitInputDto> requestedSplits)
    {
        if (requestedSplits.Any(split => !split.Amount.HasValue))
        {
            throw new AppException("Amount is required for each exact split.");
        }

        var totalAmount = MoneyMath.Round(requestedSplits.Sum(split => split.Amount!.Value));
        if (totalAmount != MoneyMath.Round(amount))
        {
            throw new AppException("Exact split amounts must equal the expense amount.");
        }

        return requestedSplits
            .OrderBy(split => split.UserId)
            .Select(split => new ExpenseSplit
            {
                UserId = split.UserId,
                AmountOwed = MoneyMath.Round(split.Amount!.Value),
                SplitType = SplitTypes.Exact
            })
            .ToList();
    }

    private static void EnsureAllSplitUsersAreMembers(IEnumerable<Guid> splitUserIds, ISet<Guid> memberIds)
    {
        var invalidUserId = splitUserIds.FirstOrDefault(userId => !memberIds.Contains(userId));
        if (invalidUserId != Guid.Empty)
        {
            throw new ForbiddenException($"User {invalidUserId} is not a member of this group.");
        }
    }

    private void EnsureCanMutateExpense(Expense expense)
    {
        if (expense.PaidBy != currentUser.UserId && expense.Group?.CreatedBy != currentUser.UserId)
        {
            throw new ForbiddenException("Only the payer or group owner can change this expense.");
        }
    }

    private static void EnsureMember(Group group, Guid userId)
    {
        if (group.Members.All(member => member.UserId != userId))
        {
            throw new ForbiddenException("User is not a member of this group.");
        }
    }

    private static string NormalizeSplitType(string splitType) => splitType.Trim().ToLowerInvariant();

    private async Task<ExpenseResponse> GetExpenseResponseAsync(Guid id, CancellationToken cancellationToken)
    {
        var expense = await context.Expenses
            .AsNoTracking()
            .Include(candidate => candidate.PaidByUser)
            .Include(candidate => candidate.Splits)
                .ThenInclude(split => split.User)
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException("Expense was not found.");

        return ToResponse(expense);
    }

    private async Task BroadcastBalanceRefreshAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var balances = await settlementService.GetBalancesAsync(groupId, cancellationToken);
        var settlements = await settlementService.GetPendingSettlementsAsync(groupId, cancellationToken);

        await realtimeNotifier.SendGroupAsync(groupId, "BalanceUpdated", new { GroupId = groupId, Balances = balances, Settlements = settlements }, cancellationToken);
    }

    private static ExpenseResponse ToResponse(Expense expense) =>
        new(
            expense.Id,
            expense.GroupId,
            expense.PaidBy,
            $"{expense.PaidByUser?.FirstName} {expense.PaidByUser?.LastName}".Trim(),
            expense.Title,
            expense.Description,
            expense.Amount,
            expense.Category,
            expense.CreatedAt,
            expense.Splits
                .OrderBy(split => split.User?.FirstName)
                .ThenBy(split => split.User?.LastName)
                .Select(split => new ExpenseSplitResponse(
                    split.Id,
                    split.UserId,
                    split.User?.FirstName ?? string.Empty,
                    split.User?.LastName ?? string.Empty,
                    split.AmountOwed,
                    split.SplitType,
                    split.Status))
                .ToArray());
}
