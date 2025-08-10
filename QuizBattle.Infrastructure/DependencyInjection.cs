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
            var connectionString = configuration.GetConnectionString("Postgres") ??
                                   throw new InvalidOperationException("Connection string 'Postgres' not found.");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddSingleton<ISqlConnectionFactory>(_ =>
                new SqlConnectionFactory(connectionString));

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

            return services;
        }
    }
}
