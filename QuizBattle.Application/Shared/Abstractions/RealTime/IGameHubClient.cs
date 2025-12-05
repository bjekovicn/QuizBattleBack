using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Features.Games;

namespace QuizBattle.Application.Shared.Abstractions.RealTime
{
    public interface IGameHubClient
    {
        // Room events
        Task RoomCreated(GameRoomDto room);
        Task PlayerJoined(GamePlayerDto player);
        Task PlayerLeft(int userId);
        Task PlayerReadyChanged(int userId, bool isReady);
        Task PlayerDisconnected(int userId);
        Task PlayerReconnected(int userId);

        // Game flow events
        Task GameStarting(GameRoomDto room);
        Task RoundStarted(RoundStartedEvent roundEvent);
        Task PlayerAnswered(int userId);
        Task RoundEnded(RoundResultDto result);
        Task GameEnded(GameResultDto result);

        // Matchmaking events
        Task MatchFound(MatchFoundEvent matchEvent);
        Task MatchmakingUpdate(MatchmakingUpdateEvent update);

        // Error events
        Task Error(string code, string message);
    }

    public sealed record RoundStartedEvent(
        int CurrentRound,
        int TotalRounds,
        GameQuestionClientDto Question,
        long RoundEndsAt);

    public sealed record GameQuestionClientDto(
        int QuestionId,
        int RoundNumber,
        string Text,
        string OptionA,
        string OptionB,
        string OptionC);  // No CorrectOption sent to client!

    public sealed record MatchFoundEvent(
        string RoomId,
        List<MatchedPlayerDto> Players);

    public sealed record MatchmakingUpdateEvent(
        int QueuePosition,
        int PlayersInQueue);
}
