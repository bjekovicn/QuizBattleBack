namespace QuizBattle.Application.Shared.Abstractions.Auth
{
    public interface ITokenHashService
    {
        string HashToken(string token);
        bool VerifyToken(string token, string hash);
    }
}
