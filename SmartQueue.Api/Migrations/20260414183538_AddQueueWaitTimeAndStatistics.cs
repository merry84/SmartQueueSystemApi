using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartQueue.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQueueWaitTimeAndStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstimatedWaitTimeMinutes",
                table: "QueueTickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ServedOn",
                table: "QueueTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AverageServiceTimeMinutes",
                table: "Queues",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedWaitTimeMinutes",
                table: "QueueTickets");

            migrationBuilder.DropColumn(
                name: "ServedOn",
                table: "QueueTickets");

            migrationBuilder.DropColumn(
                name: "AverageServiceTimeMinutes",
                table: "Queues");
        }
    }
}
