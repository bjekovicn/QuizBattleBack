using Microsoft.EntityFrameworkCore;
using QuizBattle.Infrastructure.Shared.Persistence;

namespace QuizBattle.Api.Shared.Extensions
{
    internal static class ApplicationBuilderExtensions
    {
        public static void ApplyMigrations(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();

            using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.Database.Migrate();
        }
    }
}
