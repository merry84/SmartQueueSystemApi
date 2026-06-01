using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartQueue.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserToQueueTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "QueueTickets",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QueueTickets_UserId",
                table: "QueueTickets",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_QueueTickets_AspNetUsers_UserId",
                table: "QueueTickets",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QueueTickets_AspNetUsers_UserId",
                table: "QueueTickets");

            migrationBuilder.DropIndex(
                name: "IX_QueueTickets_UserId",
                table: "QueueTickets");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "QueueTickets");
        }
    }
}
