using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartQueue.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameTicketLifecycleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedWaitTimeMinutes",
                table: "QueueTickets");

            migrationBuilder.RenameColumn(
                name: "ServedOn",
                table: "QueueTickets",
                newName: "ServiceStartedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedOn",
                table: "QueueTickets",
                newName: "JoinedAt");

            migrationBuilder.RenameColumn(
                name: "CalledOn",
                table: "QueueTickets",
                newName: "ServedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "CalledAt",
                table: "QueueTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "QueueTickets",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalledAt",
                table: "QueueTickets");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "QueueTickets");

            migrationBuilder.RenameColumn(
                name: "ServiceStartedAt",
                table: "QueueTickets",
                newName: "ServedOn");

            migrationBuilder.RenameColumn(
                name: "ServedAt",
                table: "QueueTickets",
                newName: "CalledOn");

            migrationBuilder.RenameColumn(
                name: "JoinedAt",
                table: "QueueTickets",
                newName: "CreatedOn");

            migrationBuilder.AddColumn<int>(
                name: "EstimatedWaitTimeMinutes",
                table: "QueueTickets",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
