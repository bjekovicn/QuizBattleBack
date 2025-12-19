namespace QuizBattle.Infrastructure.Features.Games.Redis.Scripting.Responses
{
    internal sealed class SetPlayerReadyResponse : LuaScriptResponse
    {
        public bool AllPlayersReady { get; init; }
    }
}