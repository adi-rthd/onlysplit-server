using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlySplit.Domain.Entities;

namespace OnlySplit.Infrastructure.Configurations;

public sealed class SettlementPaymentConfiguration : IEntityTypeConfiguration<SettlementPayment>
{
    public void Configure(EntityTypeBuilder<SettlementPayment> builder)
    {
        builder.ToTable("settlement_payments");
        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.Amount).HasPrecision(18, 2);
        builder.Property(sp => sp.Status).HasMaxLength(40);
        builder.Property(sp => sp.Method).HasMaxLength(30);
        builder.Property(sp => sp.ProofUrl).HasMaxLength(1000);
        builder.Property(sp => sp.ProofFileName).HasMaxLength(255);
        builder.Property(sp => sp.UpiReferenceNumber).HasMaxLength(100);
        builder.Property(sp => sp.Notes).HasMaxLength(500);
        builder.Property(sp => sp.RejectionReason).HasMaxLength(500);

        builder.HasOne(sp => sp.Settlement)
            .WithMany(s => s.SettlementPayments)
            .HasForeignKey(sp => sp.SettlementId);

        builder.HasOne(sp => sp.FromUser).WithMany().HasForeignKey(sp => sp.FromUserId);
        builder.HasOne(sp => sp.ToUser).WithMany().HasForeignKey(sp => sp.ToUserId);

        builder.HasIndex(sp => sp.SettlementId);
        builder.HasIndex(sp => sp.Status);
        builder.HasIndex(sp => sp.CreatedAt);
        builder.HasIndex(sp => sp.FromUserId);
        builder.HasIndex(sp => sp.ToUserId);
        builder.HasIndex(sp => new { sp.SettlementId, sp.Status });
    }
}
