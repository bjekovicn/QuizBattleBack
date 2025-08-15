using StackExchange.Redis;
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresDatabaseResource> database = builder.AddPostgres("postgres")
   .WithDataVolume()
   .AddDatabase("quizbattledb");

IResourceBuilder<RedisResource> cache = builder.AddRedis("redis")
    .WithRedisInsight();

builder.AddProject<Projects.QuizBattle_Api>("quizbattle-api")   
   .WithReference(cache)
   .WithReference(database)
   .WaitFor(cache)
   .WaitFor(database);

builder.Build().Run();
