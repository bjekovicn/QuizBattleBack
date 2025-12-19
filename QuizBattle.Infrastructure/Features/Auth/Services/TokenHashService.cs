using System.Security.Cryptography;
using System.Text;
using QuizBattle.Application.Shared.Abstractions.Auth;

namespace QuizBattle.Infrastructure.Features.Auth.Services
{

    internal sealed class TokenHashService : ITokenHashService
    {
        public string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }

        public bool VerifyToken(string token, string hash)
        {
            var computedHash = HashToken(token);
            return computedHash == hash;
        }
    }
}
