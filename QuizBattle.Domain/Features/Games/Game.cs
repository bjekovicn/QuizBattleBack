using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Features.Questions;
using QuizBattle.Domain.Shared.Abstractions;
public sealed class Game : Entity<GameId>
{
    public Language Language { get; private set; }
    public int CurrentRound { get; private set; }
    public int TotalRounds { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime StartedOn { get; private set; }
    public DateTime RoundStartedOn { get; private set; }
    public ICollection<Player> Players { get; private set; }
    public ICollection<Question> Questions { get; private set; }

    private Game() : base(new GameId(Guid.Empty))
    {
        Language = Language.Serbian;
        Players = new List<Player>();
        Questions = new List<Question>();
    }
    public Game(GameId id, Language language, int totalRounds, DateTime createdOn) : base(id)
    {
        Language = language;
        TotalRounds = totalRounds;
        CreatedOn = createdOn;
        StartedOn = createdOn;
        CurrentRound = 0;
        Players = new List<Player>();
        Questions = new List<Question>();
    }
}