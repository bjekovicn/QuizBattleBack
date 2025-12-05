namespace QuizBattle.Domain.Features.Games
{
    public enum GameStatus
    {
        WaitingForPlayers = 1,
        Starting = 2,
        RoundInProgress = 3,
        RoundEnded = 4,
        GameEnded = 5,
        Cancelled = 6
    }
}
