using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIQuizPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddStreakAndXP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "XPGained",
                table: "QuizAttempts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DailyStreak",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastQuizDate",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxStreak",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "XPGained",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "DailyStreak",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastQuizDate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MaxStreak",
                table: "AspNetUsers");
        }
    }
}
