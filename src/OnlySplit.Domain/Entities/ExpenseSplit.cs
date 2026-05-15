namespace OnlySplit.Domain.Entities;

public class ExpenseSplit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExpenseId { get; set; }
    public Guid UserId { get; set; }
    public decimal AmountOwed { get; set; }
    public string SplitType { get; set; } = Constants.SplitTypes.Equal;
    public string Status { get; set; } = Constants.SplitStatuses.Pending;

    public Expense? Expense { get; set; }
    public User? User { get; set; }
}
