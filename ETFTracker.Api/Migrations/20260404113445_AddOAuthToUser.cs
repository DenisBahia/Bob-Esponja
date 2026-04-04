using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_github_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_google_id",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "google_id",
                table: "users",
                newName: "GoogleId");

            migrationBuilder.RenameColumn(
                name: "github_username",
                table: "users",
                newName: "GitHubUsername");

            migrationBuilder.RenameColumn(
                name: "github_id",
                table: "users",
                newName: "GitHubId");

            migrationBuilder.RenameColumn(
                name: "avatar_url",
                table: "users",
                newName: "AvatarUrl");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_email",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "GoogleId",
                table: "users",
                newName: "google_id");

            migrationBuilder.RenameColumn(
                name: "GitHubUsername",
                table: "users",
                newName: "github_username");

            migrationBuilder.RenameColumn(
                name: "GitHubId",
                table: "users",
                newName: "github_id");

            migrationBuilder.RenameColumn(
                name: "AvatarUrl",
                table: "users",
                newName: "avatar_url");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true,
                filter: "email IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_github_id",
                table: "users",
                column: "github_id",
                unique: true,
                filter: "github_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_google_id",
                table: "users",
                column: "google_id",
                unique: true,
                filter: "google_id IS NOT NULL");
        }
    }
}
