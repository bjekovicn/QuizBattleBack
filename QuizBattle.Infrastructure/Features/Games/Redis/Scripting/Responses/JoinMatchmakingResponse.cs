namespace QuizBattle.Infrastructure.Features.Games.Redis.Scripting.Responses
{
    internal sealed class JoinMatchmakingResponse : LuaScriptResponse
    {
        public bool Matched { get; init; }
        public List<MatchmakingPlayerDto>? Players { get; init; }
        public int QueuePosition { get; init; }
    }

    internal sealed class MatchmakingPlayerDto
    {
        public int UserId { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public string? PhotoUrl { get; init; }
        public long JoinedAt { get; init; }
    }
}