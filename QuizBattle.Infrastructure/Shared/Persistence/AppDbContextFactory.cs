using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;

namespace QuizBattle.Infrastructure.Shared.Persistence
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // Dummy connection string for design-time tools.
            // The actual connection string is injected by the AppHost at runtime.
            var dummyConnectionString = "Host=localhost;Database=quizbattledl;Username=postgres;Password=password";

            optionsBuilder.UseNpgsql(dummyConnectionString)
                          .UseSnakeCaseNamingConvention();

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
