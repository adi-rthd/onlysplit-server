namespace OnlySplit.Domain.Entities;

public class Group
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = "";
    // ADD THIS
    public string Currency { get; set; } = "INR";

    public Guid CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string InviteCode { get; set; } = Guid.NewGuid().ToString("N");

    public User? CreatedByUser { get; set; }

    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();
}