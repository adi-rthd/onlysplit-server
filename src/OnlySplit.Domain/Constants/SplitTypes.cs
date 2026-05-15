namespace OnlySplit.Domain.Constants;

public static class SplitTypes
{
    public const string Equal = "equal";
    public const string Percentage = "percentage";
    public const string Exact = "exact";

    public static readonly string[] All = [Equal, Percentage, Exact];
}
