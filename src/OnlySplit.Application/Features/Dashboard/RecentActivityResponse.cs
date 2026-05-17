// RecentActivityResponse.cs

public class RecentActivityResponse
{
    public Guid ExpenseId { get; set; }

    public string Title { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public string GroupName { get; set; } = string.Empty;

    public Guid PaidByName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}