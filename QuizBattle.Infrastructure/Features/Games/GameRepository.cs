using QuizBattle.Application.Features.Games;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;
using StackExchange.Redis;
using System.Text.Json;

namespace QuizBattle.Infrastructure.Features.Games;

public sealed class RedisGamesRepository : IGamesRepository
{
    private readonly IDatabase _redisDb;

    public RedisGamesRepository(IConnectionMultiplexer redisConnection)
    {
        _redisDb = redisConnection.GetDatabase();
    }

    public async Task AddAsync(Game game)
    {
        var gameJson = JsonSerializer.Serialize(game);
        await _redisDb.StringSetAsync(game.Id.Value.ToString(), gameJson);
    }

    public async Task<Game?> GetByIdAsync(GameId gameId)
    {
        var gameJson = await _redisDb.StringGetAsync(gameId.Value.ToString());
        return gameJson.HasValue ? JsonSerializer.Deserialize<Game>(gameJson.ToString()) : null;
    }

    public async Task UpdateAsync(Game game)
    {
        var gameJson = JsonSerializer.Serialize(game);
        await _redisDb.StringSetAsync(game.Id.Value.ToString(), gameJson);
    }

    public async Task DeleteAsync(Game game)
    {
        await _redisDb.KeyDeleteAsync(game.Id.Value.ToString());
    }

    public async Task<IReadOnlyList<Game>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<bool> ExistsAsync(GameId gameId)
    {
        return await _redisDb.KeyExistsAsync(gameId.Value.ToString());
    }

    public async Task<Result<bool>> AddPlayerAsync(GameId gameId, User user, CancellationToken cancellationToken = default)
    {
        var game = await GetByIdAsync(gameId);
        if (game == null)
        {
            return Result.Failure<bool>(new Error("Game.NotFound", "Game not found."));
        }

        if (game.Players.Any(p => p.UserId.Value == user.Id.Value))
        {
            return Result.Failure<bool>(new Error("Player.Exists", "Player already in the game."));
        }

        game.Players.Add(new Player(user.Id, "red", 0, null, null));

        await UpdateAsync(game);
        return Result.Success(true);
    }

    public async Task<Result<bool>> AddAnswerAsync(
        GameId gameId,
        UserId userId,
        string answer,
        CancellationToken cancellationToken = default)
    {
        var game = await GetByIdAsync(gameId);
        if (game == null)
        {
            return Result.Failure<bool>(new Error("Game.NotFound", "Game not found."));
        }

        var player = game.Players.FirstOrDefault(p => p.UserId.Value == userId.Value);
        if (player is null)
        {
            return Result.Failure<bool>(new Error("Player.NotFound", "Player not found in this game."));
        }

        var updatedPlayer = player.With(answer: answer, answerTime: DateTime.UtcNow - game.RoundStartedOn);

        game.Players.Remove(player);
        game.Players.Add(updatedPlayer);

        await UpdateAsync(game);
        return Result.Success(true);
    }
}