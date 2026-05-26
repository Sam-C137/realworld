using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace RealWorldApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Articles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_email_username",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_user_sessions_user_id_is_revoked",
                table: "user_sessions");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "articles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    title = table.Column<string>(type: "character varying(510)", maxLength: 510, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    body_json = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false)
                        .Annotation("Npgsql:TsVectorConfig", "english")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "title", "description", "body", "slug" }),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_articles", x => x.id);
                    table.ForeignKey(
                        name: "fk_articles_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "article_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_article_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_article_tags_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_article_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_expires_at",
                table: "user_sessions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_revoked_at",
                table: "user_sessions",
                column: "revoked_at",
                filter: "\"is_revoked\" = true and \"revoked_at\" is not null");

            migrationBuilder.CreateIndex(
                name: "ix_article_tags_article_id_tag_id",
                table: "article_tags",
                columns: new[] { "article_id", "tag_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_article_tags_tag_id",
                table: "article_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_articles_author_id",
                table: "articles",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_articles_search_vector",
                table: "articles",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_articles_slug",
                table: "articles",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_tags");

            migrationBuilder.DropTable(
                name: "articles");

            migrationBuilder.DropIndex(
                name: "ix_user_sessions_expires_at",
                table: "user_sessions");

            migrationBuilder.DropIndex(
                name: "ix_user_sessions_revoked_at",
                table: "user_sessions");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "ix_users_email_username",
                table: "users",
                columns: new[] { "email", "username" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_user_id_is_revoked",
                table: "user_sessions",
                columns: new[] { "user_id", "is_revoked" });
        }
    }
}
