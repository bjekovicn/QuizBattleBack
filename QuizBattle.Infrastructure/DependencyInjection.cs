using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuizBattle.Application.Features.Questions;
using QuizBattle.Application.Features.Users;
using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Abstractions.Notifications;
using QuizBattle.Application.Shared.Abstractions.RealTime;
using QuizBattle.Application.Shared.Abstractions.Auth;
using QuizBattle.Infrastructure.Features.Auth;
using QuizBattle.Infrastructure.Features.Games.Services;
using QuizBattle.Infrastructure.Features.Notifications;
using QuizBattle.Infrastructure.Features.RealTime;
using QuizBattle.Infrastructure.Shared.Data;
using QuizBattle.Infrastructure.Shared.Persistence;
using StackExchange.Redis;
using QuizBattle.Application.Features.Auth;
using QuizBattle.Application.Features.Friendships;
using QuizBattle.Infrastructure.Features.Auth.Services;
using QuizBattle.Infrastructure.Features.Auth.Repositories;
using QuizBattle.Infrastructure.Features.Friendships.Repositories;
using QuizBattle.Infrastructure.Features.Questions.Repositories;
using QuizBattle.Infrastructure.Features.Users.Repositories;
using QuizBattle.Infrastructure.Features.Games.Redis.Repositories;
using QuizBattle.Infrastructure.Features.Games.Redis.Scripting;
using QuizBattle.Application.Features.Games.Repositories;
using QuizBattle.Application.Features.Games.Services;

namespace QuizBattle.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
              this IServiceCollection services,
              IConfiguration configuration)
        {
            // ========== DATABASE ==========

            // PostgreSQL with EF Core
            var connectionString = configuration.GetConnectionString("quizbattledb")
                ?? throw new InvalidOperationException("Connection string 'quizbattledb' not found.");

            services.AddDbContext<AppDbContext>(options => options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

            // Dapper SQL Connection Factory
            services.AddSingleton<ISqlConnectionFactory>(_ =>
                new SqlConnectionFactory(connectionString));

            // Unit of Work
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

            // ========== REDIS ==========

            var redisConnectionString = configuration.GetConnectionString("redis")
                ?? throw new InvalidOperationException("Connection string 'redis' not found.");

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var options = ConfigurationOptions.Parse(redisConnectionString);
                options.AbortOnConnectFail = false;
                options.ConnectRetry = 3;
                options.ConnectTimeout = 5000;
                options.SyncTimeout = 5000;
                return ConnectionMultiplexer.Connect(options);
            });


            services.AddStackExchangeRedisCache(options =>
            {
                options.ConnectionMultiplexerFactory = () =>
                {
                    var sp = services.BuildServiceProvider();
                    return Task.FromResult(sp.GetRequiredService<IConnectionMultiplexer>());
                };
                options.InstanceName = "QuizBattle:Connections:";
            });

            // Lua Script Infrastructure
            services.AddSingleton(sp =>
            {
                var mux = sp.GetRequiredService<IConnectionMultiplexer>();
                return new LuaScriptLoader(mux, "Features/Games/Redis/Scripts");
            });

            services.AddSingleton<LuaScriptExecutor>();

            // ========== REPOSITORIES ==========

            // User
            services.AddScoped<IUserQueryRepository, UserQueryRepository>();
            services.AddScoped<IUserCommandRepository, UserCommandRepository>();

            // Question
            services.AddScoped<IQuestionQueryRepository, QuestionQueryRepository>();
            services.AddScoped<IQuestionCommandRepository, QuestionCommandRepository>();

            // Game (Redis) - Using new Lua script infrastructure
            services.AddScoped<IGameRoomRepository, RedisGameRoomRepository>();
            services.AddScoped<IMatchmakingRepository, RedisMatchmakingRepository>();

            // Auth
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            // Friendship
            services.AddScoped<IFriendshipCommandRepository, FriendshipCommandRepository>();
            services.AddScoped<IFriendshipQueryRepository, FriendshipQueryRepository>();

            // ========== AUTH SERVICES ==========

            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
            services.AddSingleton<IJwtTokenService, JwtTokenService>();
            services.AddSingleton<ITokenHashService, TokenHashService>();
            services.AddScoped<IGoogleAuthService, GoogleAuthService>();
            services.AddHttpClient<IAppleAuthService, AppleAuthService>();

            // ========== NOTIFICATIONS ==========

            services.AddScoped<IPushNotificationService, FirebasePushNotificationService>();

            // ========== SIGNALR ==========

            services.AddScoped<IGameHubService, GameHubService>();

            // ========== CONNECTION MANAGER ==========

            services.AddSingleton<IConnectionManager, ConnectionManager>();

            // ========== BACKGROUND SERVICES ==========

            // Game Round Timer
            services.AddSingleton<GameRoundTimerService>();
            services.AddSingleton<IGameTimerService>(sp => sp.GetRequiredService<GameRoundTimerService>());
            services.AddHostedService(sp => sp.GetRequiredService<GameRoundTimerService>());

            // Refresh Token Cleanup
            services.AddHostedService<RefreshTokenCleanupService>();

            // Game Services - Specialized
            services.AddScoped<IGameRoomService, GameRoomService>();
            services.AddScoped<IGameMatchmakingService, GameMatchmakingService>();
            services.AddScoped<IGameRoundService, GameRoundService>();

            // Game Service - Facade
            services.AddScoped<IGameService, GameService>();

            return services;
        }

        /// <summary>
        /// Initialize Lua scripts on application startup.
        /// Call this in Program.cs after building the app.
        /// </summary>
        public static async Task InitializeLuaScriptsAsync(
            this IServiceProvider services,
            CancellationToken ct = default)
        {
            var loader = services.GetRequiredService<LuaScriptLoader>();
            await loader.LoadAllAsync(ct);
        }
    }
}