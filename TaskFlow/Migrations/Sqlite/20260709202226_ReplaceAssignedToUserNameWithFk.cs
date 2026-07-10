using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskFlow.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class ReplaceAssignedToUserNameWithFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedToUserName",
                table: "TodoTasks");

            migrationBuilder.CreateIndex(
                name: "IX_TodoTasks_AssignedToUserId",
                table: "TodoTasks",
                column: "AssignedToUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TodoTasks_AspNetUsers_AssignedToUserId",
                table: "TodoTasks",
                column: "AssignedToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TodoTasks_AspNetUsers_AssignedToUserId",
                table: "TodoTasks");

            migrationBuilder.DropIndex(
                name: "IX_TodoTasks_AssignedToUserId",
                table: "TodoTasks");

            migrationBuilder.AddColumn<string>(
                name: "AssignedToUserName",
                table: "TodoTasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "TodoTasks",
                keyColumn: "Id",
                keyValue: 1,
                column: "AssignedToUserName",
                value: null);

            migrationBuilder.UpdateData(
                table: "TodoTasks",
                keyColumn: "Id",
                keyValue: 2,
                column: "AssignedToUserName",
                value: null);

            migrationBuilder.UpdateData(
                table: "TodoTasks",
                keyColumn: "Id",
                keyValue: 3,
                column: "AssignedToUserName",
                value: null);
        }
    }
}
