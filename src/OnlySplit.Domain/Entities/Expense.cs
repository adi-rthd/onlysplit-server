namespace OnlySplit.Domain.Entities;

public class Expense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public Guid PaidBy { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Group? Group { get; set; }
    public User? PaidByUser { get; set; }
    public ICollection<ExpenseSplit> Splits { get; set; } = new List<ExpenseSplit>();
}
