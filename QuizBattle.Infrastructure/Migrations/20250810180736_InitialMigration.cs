using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizBattle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    question_id = table.Column<int>(type: "integer", nullable: false),
                    language_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    question_text = table.Column<string>(type: "text", nullable: false),
                    answer_a = table.Column<string>(type: "text", nullable: false),
                    answer_b = table.Column<string>(type: "text", nullable: false),
                    answer_c = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.question_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    google_id = table.Column<string>(type: "text", nullable: false),
                    photo_url = table.Column<string>(type: "text", nullable: true),
                    coins = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    tokens = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    games_won = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    games_lost = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.user_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
