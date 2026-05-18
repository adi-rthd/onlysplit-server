using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlySplit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialBaseLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),

                    UserId = table.Column<Guid>(type: "uuid", nullable: false),

                    Type = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false),

                    Title = table.Column<string>(
                        type: "character varying(255)",
                        maxLength: 255,
                        nullable: false),

                    Message = table.Column<string>(
                        type: "text",
                        nullable: true),

                    Payload = table.Column<string>(
                        type: "jsonb",
                        nullable: true),

                    IsRead = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: false),

                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);

                    table.ForeignKey(
                        name: "FK_notifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    GroupId = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    InvitedBy = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    InvitedUserId = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),

                    Status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false),

                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_group_invitations",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_group_invitations_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_group_invitations_users_InvitedBy",
                        column: x => x.InvitedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "FK_group_invitations_users_InvitedUserId",
                        column: x => x.InvitedUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId",
                table: "notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_group_invitations_GroupId_InvitedUserId",
                table: "group_invitations",
                columns: new[] { "GroupId", "InvitedUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_invitations_InvitedBy",
                table: "group_invitations",
                column: "InvitedBy");

            migrationBuilder.CreateIndex(
                name: "IX_group_invitations_InvitedUserId",
                table: "group_invitations",
                column: "InvitedUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "group_invitations");

            migrationBuilder.DropTable(
                name: "notifications");
        }
    }
}