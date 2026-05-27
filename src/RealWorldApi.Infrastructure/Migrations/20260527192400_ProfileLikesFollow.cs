using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealWorldApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProfileLikesFollow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_username",
                table: "users");

            migrationBuilder.DropColumn(
                name: "bio",
                table: "users");

            migrationBuilder.DropColumn(
                name: "image",
                table: "users");

            migrationBuilder.DropColumn(
                name: "username",
                table: "users");

            migrationBuilder.CreateTable(
                name: "likes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_likes", x => x.id);
                    table.ForeignKey(
                        name: "fk_likes_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_likes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    bio = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    image = table.Column<string>(type: "character varying(510)", maxLength: 510, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "follows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    follower_id = table.Column<Guid>(type: "uuid", nullable: false),
                    followee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_follows", x => x.id);
                    table.ForeignKey(
                        name: "fk_follows_profiles_followee_id",
                        column: x => x.followee_id,
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_follows_profiles_follower_id",
                        column: x => x.follower_id,
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_follows_followee_id",
                table: "follows",
                column: "followee_id");

            migrationBuilder.CreateIndex(
                name: "ix_follows_follower_id",
                table: "follows",
                column: "follower_id");

            migrationBuilder.CreateIndex(
                name: "ix_follows_follower_id_followee_id",
                table: "follows",
                columns: new[] { "follower_id", "followee_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_likes_article_id",
                table: "likes",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "ix_likes_user_id_article_id",
                table: "likes",
                columns: new[] { "user_id", "article_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_profiles_bio",
                table: "profiles",
                column: "bio",
                filter: "\"bio\" is not null and \"bio\" <> ''")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_profiles_user_id",
                table: "profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_profiles_username",
                table: "profiles",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "follows");

            migrationBuilder.DropTable(
                name: "likes");

            migrationBuilder.DropTable(
                name: "profiles");

            migrationBuilder.AddColumn<string>(
                name: "bio",
                table: "users",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image",
                table: "users",
                type: "character varying(510)",
                maxLength: 510,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "username",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);
        }
    }
}
