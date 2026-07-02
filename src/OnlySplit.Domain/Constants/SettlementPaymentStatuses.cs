namespace OnlySplit.Domain.Constants;

// Status terminology mapping (requirements → database values):
// Pending   → "PendingConfirmation"
// Confirmed → "Confirmed"
// Rejected  → "Rejected"
public static class SettlementPaymentStatuses
{
    public const string PendingConfirmation = "PendingConfirmation";
    public const string Confirmed = "Confirmed";
    public const string Rejected = "Rejected";

    public static readonly string[] All = [PendingConfirmation, Confirmed, Rejected];
}
