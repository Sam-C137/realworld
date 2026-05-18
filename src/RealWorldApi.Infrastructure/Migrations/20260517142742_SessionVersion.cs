using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealWorldApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SessionVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "session_version",
                table: "user_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_refresh_token_hash",
                table: "user_sessions",
                column: "refresh_token_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_sessions_refresh_token_hash",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "session_version",
                table: "user_sessions");
        }
    }
}
