using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlySplit.Domain.Entities;

namespace OnlySplit.Infrastructure.Configurations;

public sealed class SettlementAuditConfiguration : IEntityTypeConfiguration<SettlementAudit>
{
    public void Configure(EntityTypeBuilder<SettlementAudit> builder)
    {
        builder.ToTable("settlement_audit");
        builder.HasKey(sa => sa.Id);

        builder.Property(sa => sa.Action).HasMaxLength(50).IsRequired();
        builder.Property(sa => sa.OldStatus).HasMaxLength(40);
        builder.Property(sa => sa.NewStatus).HasMaxLength(40);
        builder.Property(sa => sa.MetadataJson).HasColumnType("jsonb");
        builder.Property(sa => sa.CreatedAt).IsRequired();

        builder.HasIndex(sa => sa.SettlementPaymentId);
        builder.HasIndex(sa => sa.UserId);

        builder.HasOne(sa => sa.SettlementPayment)
            .WithMany()
            .HasForeignKey(sa => sa.SettlementPaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sa => sa.User)
            .WithMany()
            .HasForeignKey(sa => sa.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
