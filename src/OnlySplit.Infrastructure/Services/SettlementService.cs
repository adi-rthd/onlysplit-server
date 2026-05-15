using Microsoft.EntityFrameworkCore;
using OnlySplit.Domain.Constants;
using OnlySplit.Infrastructure.Database;
using OnlySplit.Application.Features.Settlements;
using OnlySplit.Domain.Entities;
using OnlySplit.Domain.Exceptions;
using OnlySplit.Application.Interfaces;
using OnlySplit.Domain.Utils;

namespace OnlySplit.Infrastructure.Services;

public sealed class SettlementService(
    OnlySplitDbContext context,
    ICurrentUserService currentUser,
    IActivityService activityService) : ISettlementService
{
    public async Task<IReadOnlyCollection<BalanceResponse>> GetBalancesAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(groupId, cancellationToken);
        var (members, balances) = await CalculateBalancesAsync(groupId, cancellationToken);

        return members.Select(member => new BalanceResponse(
                member.UserId,
                member.FirstName,
                member.LastName,
                member.Email,
                MoneyMath.Round(balances.GetValueOrDefault(member.UserId))))
            .OrderByDescending(balance => balance.NetBalance)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<SettlementResponse>> GetPendingSettlementsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(groupId, cancellationToken);

        var settlements = await context.Settlements
            .AsNoTracking()
            .Include(settlement => settlement.Payer)
            .Include(settlement => settlement.Receiver)
            .Where(settlement => settlement.GroupId == groupId && settlement.Status == SettlementStatuses.Pending)
            .OrderBy(settlement => settlement.CreatedAt)
            .ToListAsync(cancellationToken);

        return settlements.Select(ToResponse).ToArray();
    }

    public async Task<IReadOnlyCollection<Settlement>> RegenerateForGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(groupId, cancellationToken);

        var pendingSettlements = await context.Settlements
            .Include(settlement => settlement.Payments)
            .Where(settlement => settlement.GroupId == groupId && settlement.Status == SettlementStatuses.Pending)
            .ToListAsync(cancellationToken);

        foreach (var settlement in pendingSettlements)
        {
            settlement.Status = SettlementStatuses.Cancelled;
            foreach (var payment in settlement.Payments.Where(payment => payment.Status == PaymentStatuses.Pending))
            {
                payment.Status = PaymentStatuses.Cancelled;
            }
        }

        var (_, balances) = await CalculateBalancesAsync(groupId, cancellationToken);
        var settlements = BuildOptimizedSettlements(groupId, balances);

        if (settlements.Count > 0)
        {
            context.Settlements.AddRange(settlements);
        }

        await context.SaveChangesAsync(cancellationToken);

        await activityService.LogAsync(currentUser.UserId, ActivityTypes.SettlementsGenerated, new
        {
            GroupId = groupId,
            Count = settlements.Count,
            Total = settlements.Sum(settlement => settlement.Amount)
        }, cancellationToken);

        return settlements;
    }

    private async Task EnsureMemberAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var groupExists = await context.Groups.AnyAsync(group => group.Id == groupId, cancellationToken);
        if (!groupExists)
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

    private async Task<(IReadOnlyCollection<MemberBalanceProjection> Members, Dictionary<Guid, decimal> Balances)> CalculateBalancesAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var members = await context.GroupMembers
            .AsNoTracking()
            .Where(member => member.GroupId == groupId)
            .Select(member => new MemberBalanceProjection(
                member.UserId,
                member.User!.FirstName,
                member.User.LastName,
                member.User.Email))
            .ToListAsync(cancellationToken);

        var balances = members.ToDictionary(member => member.UserId, _ => 0m);

        var expenses = await context.Expenses
            .AsNoTracking()
            .Include(expense => expense.Splits)
            .Where(expense => expense.GroupId == groupId)
            .ToListAsync(cancellationToken);

        foreach (var expense in expenses)
        {
            balances[expense.PaidBy] = balances.GetValueOrDefault(expense.PaidBy) + expense.Amount;

            foreach (var split in expense.Splits)
            {
                balances[split.UserId] = balances.GetValueOrDefault(split.UserId) - split.AmountOwed;
            }
        }

        var completedSettlements = await context.Settlements
            .AsNoTracking()
            .Where(settlement => settlement.GroupId == groupId && settlement.Status == SettlementStatuses.Completed)
            .ToListAsync(cancellationToken);

        foreach (var settlement in completedSettlements)
        {
            balances[settlement.PayerId] = balances.GetValueOrDefault(settlement.PayerId) + settlement.Amount;
            balances[settlement.ReceiverId] = balances.GetValueOrDefault(settlement.ReceiverId) - settlement.Amount;
        }

        return (members, balances);
    }

    private static List<Settlement> BuildOptimizedSettlements(Guid groupId, IReadOnlyDictionary<Guid, decimal> balances)
    {
        var debtors = balances
            .Where(balance => balance.Value < -0.01m)
            .Select(balance => new BalanceBucket(balance.Key, MoneyMath.Round(Math.Abs(balance.Value))))
            .OrderByDescending(balance => balance.Amount)
            .ToList();

        var creditors = balances
            .Where(balance => balance.Value > 0.01m)
            .Select(balance => new BalanceBucket(balance.Key, MoneyMath.Round(balance.Value)))
            .OrderByDescending(balance => balance.Amount)
            .ToList();

        var settlements = new List<Settlement>();
        var debtorIndex = 0;
        var creditorIndex = 0;

        while (debtorIndex < debtors.Count && creditorIndex < creditors.Count)
        {
            var debtor = debtors[debtorIndex];
            var creditor = creditors[creditorIndex];
            var amount = MoneyMath.Round(Math.Min(debtor.Amount, creditor.Amount));

            if (amount > 0)
            {
                settlements.Add(new Settlement
                {
                    GroupId = groupId,
                    PayerId = debtor.UserId,
                    ReceiverId = creditor.UserId,
                    Amount = amount
                });
            }

            debtors[debtorIndex] = debtor with { Amount = MoneyMath.Round(debtor.Amount - amount) };
            creditors[creditorIndex] = creditor with { Amount = MoneyMath.Round(creditor.Amount - amount) };

            if (debtors[debtorIndex].Amount <= 0.01m)
            {
                debtorIndex++;
            }

            if (creditors[creditorIndex].Amount <= 0.01m)
            {
                creditorIndex++;
            }
        }

        return settlements;
    }

    private static SettlementResponse ToResponse(Settlement settlement) =>
        new(
            settlement.Id,
            settlement.GroupId,
            settlement.PayerId,
            $"{settlement.Payer?.FirstName} {settlement.Payer?.LastName}".Trim(),
            settlement.ReceiverId,
            $"{settlement.Receiver?.FirstName} {settlement.Receiver?.LastName}".Trim(),
            settlement.Amount,
            settlement.Status,
            settlement.CreatedAt);

    private sealed record MemberBalanceProjection(Guid UserId, string FirstName, string LastName, string Email);

    private sealed record BalanceBucket(Guid UserId, decimal Amount);
}
