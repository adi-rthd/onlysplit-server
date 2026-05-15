namespace OnlySplit.Domain.Constants;

public static class PaymentStatuses
{
    public const string Pending = "pending";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Refunded = "refunded";
    public const string Cancelled = "cancelled";

    public static readonly string[] All = [Pending, Completed, Failed, Refunded, Cancelled];
}
