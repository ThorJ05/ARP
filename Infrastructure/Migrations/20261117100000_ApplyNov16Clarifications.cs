using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <summary>
    /// Changes from the status meeting 16 November: removes money and hourly paid employees, work schedule
    /// as start/end times, hours with 2 decimals, one accountant and one system owner, cancellation and
    /// transfer requests. Existing data is converted instead of deleted.
    /// </summary>
    public partial class ApplyNov16Clarifications : Migration
    {
        // new day name and the old hours column
        private static readonly (string Day, string OldHours)[] WorkWeek =
        [
            ("Monday", "MonHours"), ("Tuesday", "TueHours"), ("Wednesday", "WedHours"),
            ("Thursday", "ThuHours"), ("Friday", "FriHours")
        ];

        private static readonly string[] OldHoursColumns =
            ["MonHours", "TueHours", "WedHours", "ThuHours", "FriHours", "SatHours", "SunHours"];

        // Same rule as in WorkScheduleEntryConfiguration
        private static string ValidTimesSql => string.Join(" AND ", WorkWeek.Select(w =>
            $"(\"{w.Day}Start\" IS NULL) = (\"{w.Day}End\" IS NULL) AND (\"{w.Day}Start\" IS NULL OR \"{w.Day}End\" > \"{w.Day}Start\")"));

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Remove hourly paid employees (they use Feriekonto)
            migrationBuilder.Sql("""
                DELETE FROM "Users" WHERE "EmployeeId" IN (SELECT "Id" FROM "Employees" WHERE "PayType" = 'Hourly');
                DELETE FROM "Employees" WHERE "PayType" = 'Hourly';
                """);

            migrationBuilder.DropColumn(name: "PayType", table: "Employees");

            // 2. Remove money stuff, payroll handles that
            migrationBuilder.DropTable(name: "MonthlySalaryRecords");

            migrationBuilder.Sql("""
                DELETE FROM "RuleParameters"
                WHERE "Key" IN ('supplement.base_percent', 'supplement.raised_percent',
                                'payout.percent_weeks1to5', 'payout.percent_week6');
                """);

            // 3. Rename roles and add the login rules
            migrationBuilder.Sql("""
                UPDATE "Users" SET "Role" = 'Accountant' WHERE "Role" = 'Accounting';
                UPDATE "Users" SET "Role" = 'SystemOwner' WHERE "Role" = 'Admin';
                """);

            migrationBuilder.DropForeignKey(name: "FK_Users_Employees_EmployeeId", table: "Users");
            migrationBuilder.DropIndex(name: "IX_Users_EmployeeId", table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeId", table: "Users", column: "EmployeeId", unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role", table: "Users", column: "Role", unique: true,
                filter: "\"Role\" <> 'Employee'");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Employees_EmployeeId", table: "Users", column: "EmployeeId",
                principalTable: "Employees", principalColumn: "Id", onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_EmployeeLink", table: "Users",
                sql: "(\"Role\" = 'Employee') = (\"EmployeeId\" IS NOT NULL)");

            // 4. Work schedule as start/end times. Old schedules start at 08:00 and keep the same hours
            foreach (var (day, _) in WorkWeek)
            {
                migrationBuilder.AddColumn<TimeOnly>(
                    name: $"{day}Start", table: "WorkScheduleEntries", type: "time without time zone", nullable: true);
                migrationBuilder.AddColumn<TimeOnly>(
                    name: $"{day}End", table: "WorkScheduleEntries", type: "time without time zone", nullable: true);
            }

            migrationBuilder.Sql("UPDATE \"WorkScheduleEntries\" SET " + string.Join(", ", WorkWeek.Select(w =>
                $"\"{w.Day}Start\" = CASE WHEN \"{w.OldHours}\" > 0 THEN TIME '08:00' END, " +
                $"\"{w.Day}End\" = CASE WHEN \"{w.OldHours}\" > 0 THEN TIME '08:00' + make_interval(secs => (\"{w.OldHours}\" * 3600)::double precision) END")) + ";");

            foreach (var column in OldHoursColumns)
                migrationBuilder.DropColumn(name: column, table: "WorkScheduleEntries");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkScheduleEntries_ValidTimes", table: "WorkScheduleEntries", sql: ValidTimesSql);

            // 5. Hours with 2 decimals + longer Status column for CancellationRequested
            AlterHours(migrationBuilder, "LedgerEntries", fromScale: 3, toScale: 2);
            AlterHours(migrationBuilder, "HolidayRequests", fromScale: 3, toScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Status", table: "HolidayRequests", type: "character varying(30)", maxLength: 30, nullable: false,
                oldClrType: typeof(string), oldType: "character varying(20)", oldMaxLength: 20);

            migrationBuilder.AddCheckConstraint(
                name: "CK_HolidayRequests_Valid", table: "HolidayRequests",
                sql: "\"EndDate\" >= \"StartDate\" AND \"Hours\" > 0");

            // 6. DecidedByUserId becomes a foreign key to Users
            migrationBuilder.Sql("""
                UPDATE "HolidayRequests" SET "DecidedByUserId" = NULL
                WHERE "DecidedByUserId" IS NOT NULL AND "DecidedByUserId" NOT IN (SELECT "Id" FROM "Users");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_HolidayRequests_DecidedByUserId", table: "HolidayRequests", column: "DecidedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_HolidayRequests_Users_DecidedByUserId", table: "HolidayRequests", column: "DecidedByUserId",
                principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.SetNull);

            // 7. Transfer requests
            migrationBuilder.CreateTable(
                name: "TransferRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FromPeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    Hours = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DecisionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferRequests", x => x.Id);
                    table.CheckConstraint("CK_TransferRequests_PositiveHours", "\"Hours\" > 0");
                    table.ForeignKey(
                        name: "FK_TransferRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransferRequests_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransferRequests_DecidedByUserId", table: "TransferRequests", column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRequests_EmployeeId_FromPeriodStart", table: "TransferRequests",
                columns: new[] { "EmployeeId", "FromPeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_TransferRequests_Status", table: "TransferRequests", column: "Status");

            migrationBuilder.AddColumn<Guid>(
                name: "TransferRequestId", table: "LedgerEntries", type: "uuid", nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_TransferRequestId", table: "LedgerEntries", column: "TransferRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_LedgerEntries_TransferRequests_TransferRequestId", table: "LedgerEntries",
                column: "TransferRequestId", principalTable: "TransferRequests", principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 7. Transfer requests
            migrationBuilder.DropForeignKey(name: "FK_LedgerEntries_TransferRequests_TransferRequestId", table: "LedgerEntries");
            migrationBuilder.DropIndex(name: "IX_LedgerEntries_TransferRequestId", table: "LedgerEntries");
            migrationBuilder.DropColumn(name: "TransferRequestId", table: "LedgerEntries");
            migrationBuilder.DropTable(name: "TransferRequests");

            // 6. Decided-by reference
            migrationBuilder.DropForeignKey(name: "FK_HolidayRequests_Users_DecidedByUserId", table: "HolidayRequests");
            migrationBuilder.DropIndex(name: "IX_HolidayRequests_DecidedByUserId", table: "HolidayRequests");

            // 5. Hours with three decimals, old status length
            migrationBuilder.DropCheckConstraint(name: "CK_HolidayRequests_Valid", table: "HolidayRequests");
            migrationBuilder.Sql("UPDATE \"HolidayRequests\" SET \"Status\" = 'Approved' WHERE \"Status\" = 'CancellationRequested';");

            migrationBuilder.AlterColumn<string>(
                name: "Status", table: "HolidayRequests", type: "character varying(20)", maxLength: 20, nullable: false,
                oldClrType: typeof(string), oldType: "character varying(30)", oldMaxLength: 30);

            AlterHours(migrationBuilder, "HolidayRequests", fromScale: 2, toScale: 3);
            AlterHours(migrationBuilder, "LedgerEntries", fromScale: 2, toScale: 3);

            // 4. Work schedule back to hours per day
            migrationBuilder.DropCheckConstraint(name: "CK_WorkScheduleEntries_ValidTimes", table: "WorkScheduleEntries");

            foreach (var column in OldHoursColumns)
            {
                migrationBuilder.AddColumn<decimal>(
                    name: column, table: "WorkScheduleEntries", type: "numeric(8,3)", precision: 8, scale: 3,
                    nullable: false, defaultValue: 0m);
            }

            migrationBuilder.Sql("UPDATE \"WorkScheduleEntries\" SET " + string.Join(", ", WorkWeek.Select(w =>
                $"\"{w.OldHours}\" = COALESCE(EXTRACT(EPOCH FROM (\"{w.Day}End\" - \"{w.Day}Start\")) / 3600, 0)")) + ";");

            foreach (var (day, _) in WorkWeek)
            {
                migrationBuilder.DropColumn(name: $"{day}Start", table: "WorkScheduleEntries");
                migrationBuilder.DropColumn(name: $"{day}End", table: "WorkScheduleEntries");
            }

            // 3. Logins
            migrationBuilder.DropCheckConstraint(name: "CK_Users_EmployeeLink", table: "Users");
            migrationBuilder.DropForeignKey(name: "FK_Users_Employees_EmployeeId", table: "Users");
            migrationBuilder.DropIndex(name: "IX_Users_Role", table: "Users");
            migrationBuilder.DropIndex(name: "IX_Users_EmployeeId", table: "Users");

            migrationBuilder.CreateIndex(name: "IX_Users_EmployeeId", table: "Users", column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Employees_EmployeeId", table: "Users", column: "EmployeeId",
                principalTable: "Employees", principalColumn: "Id", onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
                UPDATE "Users" SET "Role" = 'Accounting' WHERE "Role" = 'Accountant';
                UPDATE "Users" SET "Role" = 'Admin' WHERE "Role" = 'SystemOwner';
                """);

            // 2. Salary table back (old data is not restored)
            migrationBuilder.CreateTable(
                name: "MonthlySalaryRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Month = table.Column<DateOnly>(type: "date", nullable: false),
                    QualifyingSalary = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    HoursWorked = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlySalaryRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlySalaryRecords_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlySalaryRecords_EmployeeId_Month", table: "MonthlySalaryRecords",
                columns: new[] { "EmployeeId", "Month" }, unique: true);

            // 1. PayType back (deleted employees are not restored)
            migrationBuilder.AddColumn<string>(
                name: "PayType", table: "Employees", type: "character varying(20)", maxLength: 20,
                nullable: false, defaultValue: "Salaried");
        }

        private static void AlterHours(MigrationBuilder migrationBuilder, string table, int fromScale, int toScale) =>
            migrationBuilder.AlterColumn<decimal>(
                name: "Hours", table: table, type: $"numeric(8,{toScale})", precision: 8, scale: toScale, nullable: false,
                oldClrType: typeof(decimal), oldType: $"numeric(8,{fromScale})", oldPrecision: 8, oldScale: fromScale);
    }
}
