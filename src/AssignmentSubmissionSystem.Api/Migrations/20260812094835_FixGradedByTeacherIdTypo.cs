using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssignmentSubmissionSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixGradedByTeacherIdTypo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GraddedByTeacherId",
                table: "Submissions",
                newName: "GradedByTeacherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GradedByTeacherId",
                table: "Submissions",
                newName: "GraddedByTeacherId");
        }
    }
}
