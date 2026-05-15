using Microsoft.EntityFrameworkCore;
using OnlySplit.Domain.Entities;

namespace OnlySplit.Infrastructure.Database;

public class OnlySplitDbContext(DbContextOptions<OnlySplitDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseSplit> ExpenseSplits => Set<ExpenseSplit>();
    public DbSet<Settlement> Settlements => Set<Settlement>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OnlySplitDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
