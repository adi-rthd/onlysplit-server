using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlySplit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFriendships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
             name: "friendships",
             columns: table => new
             {
                 Id = table.Column<Guid>(type: "uuid", nullable: false),
                 RequesterId = table.Column<Guid>(type: "uuid", nullable: false),
                 AddresseeId = table.Column<Guid>(type: "uuid", nullable: false),
                 Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                 CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
             },
             constraints: table =>
             {
                 table.PrimaryKey("PK_friendships", x => x.Id);
                 table.ForeignKey(
                     name: "FK_friendships_users_AddresseeId",
                     column: x => x.AddresseeId,
                     principalTable: "users",
                     principalColumn: "Id",
                     onDelete: ReferentialAction.Restrict);
                 table.ForeignKey(
                     name: "FK_friendships_users_RequesterId",
                     column: x => x.RequesterId,
                     principalTable: "users",
                     principalColumn: "Id",
                     onDelete: ReferentialAction.Restrict);
             });


            migrationBuilder.CreateIndex(
                name: "IX_friendships_AddresseeId",
                table: "friendships",
                column: "AddresseeId");

            migrationBuilder.CreateIndex(
                name: "IX_friendships_RequesterId_AddresseeId",
                table: "friendships",
                columns: new[] { "RequesterId", "AddresseeId" },
                unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropTable(
                name: "friendships");

        }
    }
}
