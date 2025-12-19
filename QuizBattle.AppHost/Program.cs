var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("quizbattle-postgres-data", isReadOnly: false)
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent);

var database = postgres.AddDatabase("quizbattledb");

var cache = builder.AddRedis("redis")
    .WithDataVolume("quizbattle-redis-data")
    .WithPersistence(TimeSpan.FromMinutes(5), 100)
    .WithRedisInsight()
    .WithLifetime(ContainerLifetime.Session);

builder.AddProject<Projects.QuizBattle_Api>("quizbattle-api")
    .WithReference(cache)
    .WithReference(database)
    .WaitFor(cache)
    .WaitFor(database);

builder.Build().Run();