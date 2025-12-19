namespace QuizBattle.Infrastructure.Features.Games.Redis.Scripting.Responses
{
    internal sealed class SubmitAnswerResponse : LuaScriptResponse
    {
        public SubmitAnswerResultDto? Result { get; init; }
    }
    internal sealed class SubmitAnswerResultDto
    {
        public bool Accepted { get; init; }
        public bool AllPlayersAnswered { get; init; }
        public int PlayersAnsweredCount { get; init; }
        public int TotalPlayersCount { get; init; }
    }

}