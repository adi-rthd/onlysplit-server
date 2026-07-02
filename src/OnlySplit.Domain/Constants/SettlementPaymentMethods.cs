namespace OnlySplit.Domain.Constants;

public static class SettlementPaymentMethods
{
    public const string Cash = "Cash";
    public const string UPI = "UPI";
    public const string BankTransfer = "BankTransfer";
    public const string Razorpay = "Razorpay";

    /// <summary>
    /// Manual payment methods that can be submitted by users (excludes gateway methods).
    /// </summary>
    public static readonly string[] ManualMethods = [Cash, UPI, BankTransfer];

    public static readonly string[] All = [Cash, UPI, BankTransfer, Razorpay];
}
