namespace OnlySplit.Domain.Constants;

public static class SplitStatuses
{
    public const string Pending = "pending";
    public const string Settled = "settled";

    public static readonly string[] All = [Pending, Settled];
}
