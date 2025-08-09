using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

public sealed class User : Entity<UserId>
{
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string GoogleId { get; private set; }
    public string? Photo { get; private set; }
    public int Coins { get; private set; }
    public int Tokens { get; private set; }
    public int GamesWon { get; private set; }
    public int GamesLost { get; private set; }

    private User() : base(new UserId(0)) { GoogleId = string.Empty; }
    public User(UserId id, string googleId, string? firstName, string? lastName, string? photo) : base(id)
    {
        GoogleId = googleId;
        FirstName = firstName;
        LastName = lastName;
        Photo = photo;
        Coins = 0;
        Tokens = 50;
        GamesWon = 0;
        GamesLost = 0;
    }
}