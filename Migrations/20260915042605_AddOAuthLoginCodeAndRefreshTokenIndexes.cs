using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthLoginCodeAndRefreshTokenIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OAuthLoginCodes_CodeHash",
                table: "OAuthLoginCodes");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiryDate",
                table: "RefreshTokens",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthLoginCodes_CodeHash",
                table: "OAuthLoginCodes",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OAuthLoginCodes_ExpiresAt",
                table: "OAuthLoginCodes",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthLoginCodes_UsedAt",
                table: "OAuthLoginCodes",
                column: "UsedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_ExpiryDate",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_OAuthLoginCodes_CodeHash",
                table: "OAuthLoginCodes");

            migrationBuilder.DropIndex(
                name: "IX_OAuthLoginCodes_ExpiresAt",
                table: "OAuthLoginCodes");

            migrationBuilder.DropIndex(
                name: "IX_OAuthLoginCodes_UsedAt",
                table: "OAuthLoginCodes");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthLoginCodes_CodeHash",
                table: "OAuthLoginCodes",
                column: "CodeHash");
        }
    }
}
