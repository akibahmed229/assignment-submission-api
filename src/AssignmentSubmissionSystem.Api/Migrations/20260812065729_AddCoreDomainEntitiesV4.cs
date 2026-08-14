using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssignmentSubmissionSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCoreDomainEntitiesV4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Mark",
                table: "Submissions",
                newName: "Marks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Marks",
                table: "Submissions",
                newName: "Mark");
        }
    }
}
