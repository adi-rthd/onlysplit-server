using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlySplit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileSettlementsNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- Extend existing users table with new columns ---
            migrationBuilder.AddColumn<string>(
                name: "UpiId",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredUpiApp",
                table: "users",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotificationPreferencesJson",
                table: "users",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            // --- Extend existing notifications table with new columns ---
            migrationBuilder.AddColumn<Guid>(
                name: "ReferenceId",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActorUserId",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReadAt",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "notifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            // --- Add FK for notifications.ActorUserId ---
            migrationBuilder.AddForeignKey(
                name: "FK_notifications_users_ActorUserId",
                table: "notifications",
                column: "ActorUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_ActorUserId",
                table: "notifications",
                column: "ActorUserId");

            // --- Composite index on notifications for query performance ---
            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_IsRead_CreatedAt",
                table: "notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" },
                descending: new[] { false, false, true });

            // --- Create new settlement_payments table ---
            migrationBuilder.CreateTable(
                name: "settlement_payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProofUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProofFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ProofFileSize = table.Column<long>(type: "bigint", nullable: true),
                    ProofUploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpiReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConfirmedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settlement_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_settlement_payments_settlements_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_settlement_payments_users_FromUserId",
                        column: x => x.FromUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_settlement_payments_users_ToUserId",
                        column: x => x.ToUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // --- Indexes for settlement_payments ---
            migrationBuilder.CreateIndex(
                name: "IX_settlement_payments_SettlementId",
                table: "settlement_payments",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_settlement_payments_Status",
                table: "settlement_payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_settlement_payments_CreatedAt",
                table: "settlement_payments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_settlement_payments_FromUserId",
                table: "settlement_payments",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_settlement_payments_ToUserId",
                table: "settlement_payments",
                column: "ToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_settlement_payments_SettlementId_Status",
                table: "settlement_payments",
                columns: new[] { "SettlementId", "Status" });

            // --- Create new settlement_audit table ---
            migrationBuilder.CreateTable(
                name: "settlement_audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SettlementPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OldStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    NewStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settlement_audit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_settlement_audit_settlement_payments_SettlementPaymentId",
                        column: x => x.SettlementPaymentId,
                        principalTable: "settlement_payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_settlement_audit_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_settlement_audit_SettlementPaymentId",
                table: "settlement_audit",
                column: "SettlementPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_settlement_audit_UserId",
                table: "settlement_audit",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop new tables
            migrationBuilder.DropTable(
                name: "settlement_audit");

            migrationBuilder.DropTable(
                name: "settlement_payments");

            // Remove notification indexes and FK
            migrationBuilder.DropIndex(
                name: "IX_notifications_UserId_IsRead_CreatedAt",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_ActorUserId",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_users_ActorUserId",
                table: "notifications");

            // Remove notification columns
            migrationBuilder.DropColumn(name: "ArchivedAt", table: "notifications");
            migrationBuilder.DropColumn(name: "IsArchived", table: "notifications");
            migrationBuilder.DropColumn(name: "ReadAt", table: "notifications");
            migrationBuilder.DropColumn(name: "ActorUserId", table: "notifications");
            migrationBuilder.DropColumn(name: "ReferenceId", table: "notifications");

            // Remove user columns
            migrationBuilder.DropColumn(name: "UpdatedAt", table: "users");
            migrationBuilder.DropColumn(name: "NotificationPreferencesJson", table: "users");
            migrationBuilder.DropColumn(name: "PreferredUpiApp", table: "users");
            migrationBuilder.DropColumn(name: "UpiId", table: "users");
        }
    }
}
