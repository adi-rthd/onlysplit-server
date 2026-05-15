namespace OnlySplit.Domain.Constants;

public static class SettlementStatuses
{
    public const string Pending = "pending";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";

    public static readonly string[] All = [Pending, Completed, Cancelled];
}
