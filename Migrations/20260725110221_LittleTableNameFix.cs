using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymTracker.Migrations
{
    /// <inheritdoc />
    public partial class LittleTableNameFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_externalLogins_Users_UserId",
                table: "externalLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_externalLogins",
                table: "externalLogins");

            migrationBuilder.RenameTable(
                name: "externalLogins",
                newName: "ExternalLogins");

            migrationBuilder.RenameIndex(
                name: "IX_externalLogins_UserId",
                table: "ExternalLogins",
                newName: "IX_ExternalLogins_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_externalLogins_Provider_ProviderUserId",
                table: "ExternalLogins",
                newName: "IX_ExternalLogins_Provider_ProviderUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExternalLogins",
                table: "ExternalLogins",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExternalLogins_Users_UserId",
                table: "ExternalLogins",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExternalLogins_Users_UserId",
                table: "ExternalLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExternalLogins",
                table: "ExternalLogins");

            migrationBuilder.RenameTable(
                name: "ExternalLogins",
                newName: "externalLogins");

            migrationBuilder.RenameIndex(
                name: "IX_ExternalLogins_UserId",
                table: "externalLogins",
                newName: "IX_externalLogins_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ExternalLogins_Provider_ProviderUserId",
                table: "externalLogins",
                newName: "IX_externalLogins_Provider_ProviderUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_externalLogins",
                table: "externalLogins",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_externalLogins_Users_UserId",
                table: "externalLogins",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
