using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace edu_tracking.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClassGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingRequests_TeacherSlots_SlotId",
                table: "BookingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSlots_TeacherWeeklyAvailability_SourceAvailabilityId",
                table: "TeacherSlots");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Session_Seats",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_SlotId_StudentId",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "SlotId",
                table: "BookingRequests");

            migrationBuilder.RenameColumn(
                name: "SourceAvailabilityId",
                table: "TeacherSlots",
                newName: "ClassGroupScheduleId");

            migrationBuilder.RenameIndex(
                name: "IX_TeacherSlots_SourceAvailabilityId",
                table: "TeacherSlots",
                newName: "IX_TeacherSlots_ClassGroupScheduleId");

            migrationBuilder.AddColumn<int>(
                name: "ClassGroupId",
                table: "Sessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Sessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClassGroupId",
                table: "BookingRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ClassGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    GradeLevel = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MaxStudents = table.Column<int>(type: "int", nullable: false),
                    MembersCount = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RepeatsWeekly = table.Column<bool>(type: "bit", nullable: false),
                    IsOpenForEnrollment = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassGroups", x => x.Id);
                    table.CheckConstraint("CK_ClassGroup_Dates", "[EndDate] >= [StartDate]");
                    table.CheckConstraint("CK_ClassGroup_MaxStudents", "[MaxStudents] >= 1 AND [MaxStudents] <= 50");
                    table.CheckConstraint("CK_ClassGroup_Members", "[MembersCount] >= 0 AND [MembersCount] <= [MaxStudents]");
                    table.ForeignKey(
                        name: "FK_ClassGroups_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassGroups_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassGroupMembers",
                columns: table => new
                {
                    ClassGroupId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassGroupMembers", x => new { x.ClassGroupId, x.StudentId });
                    table.ForeignKey(
                        name: "FK_ClassGroupMembers_ClassGroups_ClassGroupId",
                        column: x => x.ClassGroupId,
                        principalTable: "ClassGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassGroupMembers_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassGroupSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassGroupId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassGroupSchedules", x => x.Id);
                    table.CheckConstraint("CK_ClassGroupSchedule_TimeRange", "[EndTime] > [StartTime]");
                    table.ForeignKey(
                        name: "FK_ClassGroupSchedules_ClassGroups_ClassGroupId",
                        column: x => x.ClassGroupId,
                        principalTable: "ClassGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ClassGroupId_StartUtc",
                table: "Sessions",
                columns: new[] { "ClassGroupId", "StartUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Session_Seats",
                table: "Sessions",
                sql: "[SeatsTaken] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_ClassGroupId_StudentId",
                table: "BookingRequests",
                columns: new[] { "ClassGroupId", "StudentId" },
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ClassGroupMembers_StudentId_Status",
                table: "ClassGroupMembers",
                columns: new[] { "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassGroups_SubjectId_GradeLevel_IsOpenForEnrollment",
                table: "ClassGroups",
                columns: new[] { "SubjectId", "GradeLevel", "IsOpenForEnrollment" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassGroups_TeacherId_StartDate",
                table: "ClassGroups",
                columns: new[] { "TeacherId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassGroupSchedules_ClassGroupId_DayOfWeek",
                table: "ClassGroupSchedules",
                columns: new[] { "ClassGroupId", "DayOfWeek" });

            migrationBuilder.AddForeignKey(
                name: "FK_BookingRequests_ClassGroups_ClassGroupId",
                table: "BookingRequests",
                column: "ClassGroupId",
                principalTable: "ClassGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_ClassGroups_ClassGroupId",
                table: "Sessions",
                column: "ClassGroupId",
                principalTable: "ClassGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSlots_ClassGroupSchedules_ClassGroupScheduleId",
                table: "TeacherSlots",
                column: "ClassGroupScheduleId",
                principalTable: "ClassGroupSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingRequests_ClassGroups_ClassGroupId",
                table: "BookingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_ClassGroups_ClassGroupId",
                table: "Sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSlots_ClassGroupSchedules_ClassGroupScheduleId",
                table: "TeacherSlots");

            migrationBuilder.DropTable(
                name: "ClassGroupMembers");

            migrationBuilder.DropTable(
                name: "ClassGroupSchedules");

            migrationBuilder.DropTable(
                name: "ClassGroups");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_ClassGroupId_StartUtc",
                table: "Sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Session_Seats",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_ClassGroupId_StudentId",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "ClassGroupId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ClassGroupId",
                table: "BookingRequests");

            migrationBuilder.RenameColumn(
                name: "ClassGroupScheduleId",
                table: "TeacherSlots",
                newName: "SourceAvailabilityId");

            migrationBuilder.RenameIndex(
                name: "IX_TeacherSlots_ClassGroupScheduleId",
                table: "TeacherSlots",
                newName: "IX_TeacherSlots_SourceAvailabilityId");

            migrationBuilder.AddColumn<long>(
                name: "SlotId",
                table: "BookingRequests",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Session_Seats",
                table: "Sessions",
                sql: "[SeatsTaken] >= 0 AND [SeatsTaken] <= [Capacity]");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_SlotId_StudentId",
                table: "BookingRequests",
                columns: new[] { "SlotId", "StudentId" },
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingRequests_TeacherSlots_SlotId",
                table: "BookingRequests",
                column: "SlotId",
                principalTable: "TeacherSlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSlots_TeacherWeeklyAvailability_SourceAvailabilityId",
                table: "TeacherSlots",
                column: "SourceAvailabilityId",
                principalTable: "TeacherWeeklyAvailability",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
