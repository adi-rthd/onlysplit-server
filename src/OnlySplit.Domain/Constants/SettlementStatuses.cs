namespace OnlySplit.Domain.Constants;

// Status terminology mapping (requirements → database values):
// Pending       → "pending"
// PartiallyPaid → "partially_settled"
// Completed     → "settled"
// Cancelled     → "cancelled"
public static class SettlementStatuses
{
    public const string Pending = "pending";
    public const string PartiallySettled = "partially_settled";
    public const string Settled = "settled";
    public const string Cancelled = "cancelled";

    public static readonly string[] All = [Pending, PartiallySettled, Settled, Cancelled];
}
