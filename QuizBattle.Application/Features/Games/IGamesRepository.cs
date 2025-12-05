using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;
using QuizBattle.Application.Features.Games.RedisModels;

namespace QuizBattle.Application.Features.Games
{
    public interface IGameRoomRepository
    {
        // Basic operations
        Task<GameRoomDto?> GetByIdAsync(GameRoomId roomId, CancellationToken ct = default);
        Task<bool> ExistsAsync(GameRoomId roomId, CancellationToken ct = default);
        Task DeleteAsync(GameRoomId roomId, CancellationToken ct = default);

        // Atomic operations via Lua scripts
        Task<Result<GameRoomDto>> CreateRoomAsync(
            GameType gameType,
            string languageCode,
            int totalRounds,
            CancellationToken ct = default);

        Task<Result<GamePlayerDto>> JoinRoomAsync(
            GameRoomId roomId,
            int userId,
            string displayName,
            string? photoUrl,
            CancellationToken ct = default);

        Task<Result> LeaveRoomAsync(
            GameRoomId roomId,
            int userId,
            CancellationToken ct = default);

        Task<Result> SetPlayerReadyAsync(
            GameRoomId roomId,
            int userId,
            bool isReady,
            CancellationToken ct = default);

        Task<Result> SetPlayerConnectedAsync(
            GameRoomId roomId,
            int userId,
            bool isConnected,
            CancellationToken ct = default);

        Task<Result<GameRoomDto>> StartGameAsync(
            GameRoomId roomId,
            List<GameQuestionDto> questions,
            CancellationToken ct = default);

        Task<Result<GameQuestionDto>> StartNextRoundAsync(
            GameRoomId roomId,
            CancellationToken ct = default);

        Task<Result<SubmitAnswerResult>> SubmitAnswerAsync(
            GameRoomId roomId,
            int userId,
            string answer,
            CancellationToken ct = default);

        Task<Result<RoundResultDto>> EndRoundAsync(
            GameRoomId roomId,
            CancellationToken ct = default);

        Task<Result<GameResultDto>> EndGameAsync(
            GameRoomId roomId,
            CancellationToken ct = default);

        // Player lookup
        Task<GameRoomId?> GetRoomIdByPlayerAsync(int userId, CancellationToken ct = default);
    }

    public sealed record SubmitAnswerResult(
        bool Accepted,
        bool AllPlayersAnswered,
        int PlayersAnsweredCount,
        int TotalPlayersCount);

    public sealed record RoundResultDto(
        int RoundNumber,
        int QuestionId,
        string CorrectOption,
        string CorrectAnswerText,
        List<PlayerRoundResultDto> PlayerResults,
        List<PlayerScoreDto> CurrentStandings);

    public sealed record PlayerRoundResultDto(
        int UserId,
        string DisplayName,
        string? AnswerGiven,
        long? ResponseTimeMs,
        int PointsAwarded,
        bool IsCorrect);

    public sealed record PlayerScoreDto(
        int UserId,
        string DisplayName,
        int TotalScore);

    public sealed record GameResultDto(
        string GameRoomId,
        int GameType,
        int TotalRounds,
        int? WinnerUserId,
        List<FinalStandingDto> FinalStandings,
        long? StartedAt,
        long EndedAt);

    public sealed record FinalStandingDto(
        int Position,
        int UserId,
        string DisplayName,
        string? PhotoUrl,
        int TotalScore,
        string ColorHex);
}
