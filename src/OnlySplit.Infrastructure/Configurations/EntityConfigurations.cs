using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlySplit.Application.Features.Notifications;
using OnlySplit.Domain.Entities;

namespace OnlySplit.Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.LastName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(255).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(user => user.AvatarUrl).HasMaxLength(1000);
        builder.Property(user => user.Role).HasMaxLength(50).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();

        builder.HasIndex(user => user.Email).IsUnique();
    }
}

public sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups");
        builder.HasKey(group => group.Id);

        builder.Property(group => group.Name).HasMaxLength(160).IsRequired();
        builder.Property(group => group.InviteCode).HasMaxLength(128).IsRequired();
        builder.Property(group => group.CreatedAt).IsRequired();

        builder.HasIndex(group => group.InviteCode).IsUnique();
        builder.HasIndex(group => group.CreatedBy);

        builder.HasOne(group => group.CreatedByUser)
            .WithMany(user => user.CreatedGroups)
            .HasForeignKey(group => group.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> builder)
    {
        builder.ToTable("group_members");
        builder.HasKey(member => member.Id);

        builder.Property(member => member.JoinedAt).IsRequired();
        builder.HasIndex(member => new { member.GroupId, member.UserId }).IsUnique();
        builder.HasIndex(member => member.UserId);

        builder.HasOne(member => member.Group)
            .WithMany(group => group.Members)
            .HasForeignKey(member => member.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(member => member.User)
            .WithMany(user => user.GroupMemberships)
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses");
        builder.HasKey(expense => expense.Id);

        builder.Property(expense => expense.Title).HasMaxLength(180).IsRequired();
        builder.Property(expense => expense.Description).HasMaxLength(1000);
        builder.Property(expense => expense.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(expense => expense.Category).HasMaxLength(80).IsRequired();
        builder.Property(expense => expense.CreatedAt).IsRequired();

        builder.HasIndex(expense => expense.GroupId);
        builder.HasIndex(expense => expense.PaidBy);
        builder.HasIndex(expense => expense.CreatedAt);

        builder.HasOne(expense => expense.Group)
            .WithMany(group => group.Expenses)
            .HasForeignKey(expense => expense.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(expense => expense.PaidByUser)
            .WithMany(user => user.PaidExpenses)
            .HasForeignKey(expense => expense.PaidBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ExpenseSplitConfiguration : IEntityTypeConfiguration<ExpenseSplit>
{
    public void Configure(EntityTypeBuilder<ExpenseSplit> builder)
    {
        builder.ToTable("expense_splits");
        builder.HasKey(split => split.Id);

        builder.Property(split => split.AmountOwed).HasPrecision(18, 2).IsRequired();
        builder.Property(split => split.SplitType).HasMaxLength(40).IsRequired();
        builder.Property(split => split.Status).HasMaxLength(40).IsRequired();

        builder.HasIndex(split => new { split.ExpenseId, split.UserId }).IsUnique();
        builder.HasIndex(split => split.UserId);

        builder.HasOne(split => split.Expense)
            .WithMany(expense => expense.Splits)
            .HasForeignKey(split => split.ExpenseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(split => split.User)
            .WithMany(user => user.ExpenseSplits)
            .HasForeignKey(split => split.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
{
    public void Configure(EntityTypeBuilder<Settlement> builder)
    {
        builder.ToTable("settlements");
        builder.HasKey(settlement => settlement.Id);

        builder.Property(settlement => settlement.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(settlement => settlement.Status).HasMaxLength(40).IsRequired();
        builder.Property(settlement => settlement.CreatedAt).IsRequired();

        builder.HasIndex(settlement => settlement.GroupId);
        builder.HasIndex(settlement => new { settlement.PayerId, settlement.ReceiverId, settlement.Status });

        builder.HasOne(settlement => settlement.Group)
            .WithMany(group => group.Settlements)
            .HasForeignKey(settlement => settlement.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(settlement => settlement.Payer)
            .WithMany(user => user.SettlementsPaid)
            .HasForeignKey(settlement => settlement.PayerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(settlement => settlement.Receiver)
            .WithMany(user => user.SettlementsReceived)
            .HasForeignKey(settlement => settlement.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.RazorpayOrderId).HasMaxLength(255).IsRequired();
        builder.Property(payment => payment.RazorpayPaymentId).HasMaxLength(255);
        builder.Property(payment => payment.RazorpaySignature).HasMaxLength(1000);
        builder.Property(payment => payment.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(payment => payment.Status).HasMaxLength(40).IsRequired();
        builder.Property(payment => payment.CreatedAt).IsRequired();

        builder.HasIndex(payment => payment.RazorpayOrderId).IsUnique();
        builder.HasIndex(payment => payment.RazorpayPaymentId);
        builder.HasIndex(payment => payment.SettlementId);

        builder.HasOne(payment => payment.Settlement)
            .WithMany(settlement => settlement.Payments)
            .HasForeignKey(payment => payment.SettlementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("activity_logs");
        builder.HasKey(activity => activity.Id);

        builder.Property(activity => activity.Type).HasMaxLength(120).IsRequired();
        builder.Property(activity => activity.Metadata).HasColumnType("jsonb").IsRequired();
        builder.Property(activity => activity.CreatedAt).IsRequired();

        builder.HasIndex(activity => activity.UserId);
        builder.HasIndex(activity => activity.CreatedAt);
        builder.HasIndex(activity => activity.Type);

        builder.HasOne(activity => activity.User)
            .WithMany(user => user.ActivityLogs)
            .HasForeignKey(activity => activity.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(token => token.CreatedByIp).HasMaxLength(64);
        builder.Property(token => token.RevokedByIp).HasMaxLength(64);
        builder.Property(token => token.ReplacedByTokenHash).HasMaxLength(128);
        builder.Property(token => token.ExpiresAt).IsRequired();
        builder.Property(token => token.CreatedAt).IsRequired();

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.UserId);
        builder.HasIndex(token => token.ExpiresAt);

        builder.HasOne(token => token.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class GroupInvitationConfiguration
    : IEntityTypeConfiguration<GroupInvitation>
{
    public void Configure(EntityTypeBuilder<GroupInvitation> builder)
    {
        builder.ToTable("group_invitations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.GroupId, x.InvitedUserId })
            .IsUnique();

        builder.HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.InvitedByUser)
            .WithMany()
            .HasForeignKey(x => x.InvitedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InvitedUser)
            .WithMany()
            .HasForeignKey(x => x.InvitedUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


public sealed class NotificationConfiguration
    : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Message);

        builder.Property(x => x.Payload)
            .HasColumnType("jsonb");

        builder.Property(x => x.IsRead)
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}