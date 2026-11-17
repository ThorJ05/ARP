using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <summary>Counts failed logins in a row and stores when a locked account can log in again.</summary>
    public partial class AddLoginLockout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount", table: "Users", type: "integer", nullable: false, defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEndUtc", table: "Users", type: "timestamp with time zone", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "FailedLoginCount", table: "Users");
            migrationBuilder.DropColumn(name: "LockoutEndUtc", table: "Users");
        }
    }
}
