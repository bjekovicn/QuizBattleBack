using System.Reflection;
using Microsoft.EntityFrameworkCore;
using QuizBattle.Api.Shared.Abstractions;
using QuizBattle.Api.Shared.Middleware;
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

        public static void UseCustomExceptionHandler(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
        }

        public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
        {
            var endpointTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IEndpoint).IsAssignableFrom(t));

            foreach (var type in endpointTypes)
            {
                if (Activator.CreateInstance(type) is IEndpoint endpoint)
                {
                    endpoint.MapEndpoint(app);
                }
            }

            return app;
        }
    }
}
