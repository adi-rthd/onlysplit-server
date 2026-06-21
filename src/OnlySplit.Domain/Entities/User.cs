namespace OnlySplit.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = Constants.UserRoles.User;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Profile extension fields
    public string? UpiId { get; set; }
    public string? PreferredUpiApp { get; set; }
    public string? NotificationPreferencesJson { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<Group> CreatedGroups { get; set; } = new List<Group>();
    public ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
    public ICollection<Expense> PaidExpenses { get; set; } = new List<Expense>();
    public ICollection<ExpenseSplit> ExpenseSplits { get; set; } = new List<ExpenseSplit>();
    public ICollection<Settlement> SettlementsPaid { get; set; } = new List<Settlement>();
    public ICollection<Settlement> SettlementsReceived { get; set; } = new List<Settlement>();
    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Friendship> SentFriendRequests { get; set; } = new List<Friendship>();

    public ICollection<Friendship> ReceivedFriendRequests { get; set; } = new List<Friendship>();
}
