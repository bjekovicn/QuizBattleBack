using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Domain.Shared.Abstractions;
using QuizBattle.Infrastructure.Shared.Data;
using QuizBattle.Infrastructure.Shared.Persistence;

namespace QuizBattle.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
         this IServiceCollection services,
         IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("quizbattledb") ??
                                   throw new InvalidOperationException("Connection string 'quizbattledb' not found.");

            services.AddDbContext<AppDbContext>(options => options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

            services.AddSingleton<ISqlConnectionFactory>(_ =>
                new SqlConnectionFactory(connectionString));

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

            return services;
        }
    }
}
